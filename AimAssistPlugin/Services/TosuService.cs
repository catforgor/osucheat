using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTabletDriver.Plugin;

namespace AimAssistPlugin.Services;

public sealed record TosuSnapshot(
	string? State,
	string? Client,
	int? LiveTime,
	double? Ar,
	double? Cs,
	string Mods,
	string? SongsFolder,
	string? BeatmapFile);

public static class TosuService
{
	private const string LogGroup = "Reconstructor";
	private const string StateUrl = "http://localhost:24050/json/v2";
	private const string PreciseUrl = "http://localhost:24050/json/v2/precise";
	private const string ReleaseUrl = "https://api.github.com/repos/tosuapp/tosu/releases/latest";

	// tosu = 15ms
	private static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(8);
	private static readonly TimeSpan MonitorInterval = TimeSpan.FromSeconds(2);
	private static readonly TimeSpan StartRetryBackoff = TimeSpan.FromSeconds(60);

	private const int FailureLimit = 5;

	private const string DefaultTosuConfig =
		"DEBUG_LOG=false\r\nENABLE_AUTOUPDATE=false\r\nOPEN_DASHBOARD_ON_STARTUP=false\r\n\r\n" +
		"SHOW_MP_COMMANDS=false\r\nCALCULATE_PP=false\r\n\r\n" +
		"ENABLE_KEY_OVERLAY=false\r\nENABLE_INGAME_OVERLAY=false\r\n\r\n" +
		"POLL_RATE=15\r\nPRECISE_DATA_POLL_RATE=15\r\n\r\n" +
		"INGAME_OVERLAY_KEYBIND=Control + Shift + Space\r\nINGAME_OVERLAY_MAX_FPS=60\r\n\r\n" +
		"SERVER_IP=127.0.0.1\r\nSERVER_PORT=24050\r\nALLOWED_IPS=127.0.0.1,localhost,absolute\r\n\r\n" +
		"STATIC_FOLDER_PATH=./static";

	private const int NoTime = int.MinValue;

	private static readonly HttpClient PollClient = new() { Timeout = TimeSpan.FromSeconds(2) };
	private static readonly HttpClient DownloadClient = new() { Timeout = TimeSpan.FromMinutes(5) };

	private static readonly SemaphoreSlim StartLock = new(1, 1);
	private static readonly Lock LifecycleLock = new();
	private static readonly string[] GameProcessNames = ["osu!", "osulazer"];

	private static volatile bool _isLazer;
	private static volatile bool _osuRunning;
	private static volatile bool _tosuRunning;
	private static volatile bool _isInitializing;
	private static volatile bool _isRunning;

	private static TosuSnapshot? _snapshot;
	private static int _preciseTime = NoTime;

	private static CancellationTokenSource? _cts;
	private static Process? _ownedTosu;
	private static DateTime _retryStartAfter = DateTime.MinValue;
	private static string? _lastMonitorError;

	public static bool IsRunning => _isRunning;

	public static bool IsLazer => _isLazer;

	public static TosuSnapshot? Snapshot => Volatile.Read(ref _snapshot);

	public static int? PreciseTime
	{
		get
		{
			int time = Volatile.Read(ref _preciseTime);
			return time == NoTime ? null : time;
		}
	}

	public static void EnsureStarted()
	{
		if (_isRunning)
		{
			return;
		}
		lock (LifecycleLock)
		{
			if (_isRunning)
			{
				return;
			}
			_cts?.Cancel();
			_cts?.Dispose();
			_cts = new CancellationTokenSource();
			CancellationToken token = _cts.Token;
			_ = Task.Run(() => MonitorProcessesAsync(token));
			_ = Task.Run(() => UpdateTosuDataAsync(token));
			_isRunning = true;
		}
	}

	private static async Task MonitorProcessesAsync(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			try
			{
				if (!_isInitializing)
				{
					_osuRunning = IsAnyRunning(GameProcessNames);
					_tosuRunning = IsAnyRunning("tosu");
					if (_osuRunning && !_tosuRunning)
					{
						if (DateTime.UtcNow >= _retryStartAfter)
						{
							await StartTosuAsync();
						}
					}
					else if (!_osuRunning && _tosuRunning)
					{
						StopOwnedTosu();
					}
				}
				_lastMonitorError = null;
				await Task.Delay(MonitorInterval, ct);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				LogOnce(ex);
				try
				{
					await Task.Delay(MonitorInterval, ct);
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}
		}
	}

	private static void LogOnce(Exception ex)
	{
		if (_lastMonitorError == ex.Message)
		{
			return;
		}
		_lastMonitorError = ex.Message;
		Log.Write(LogGroup, "tosu monitor error: " + ex.Message, LogLevel.Error);
	}

	private static bool IsAnyRunning(params string[] names)
	{
		foreach (string name in names)
		{
			Process[] processes = Process.GetProcessesByName(name);
			try
			{
				if (processes.Length > 0)
				{
					return true;
				}
			}
			finally
			{
				foreach (Process process in processes)
				{
					process.Dispose();
				}
			}
		}
		return false;
	}

	private static void StopOwnedTosu()
	{
		Process? owned = Interlocked.Exchange(ref _ownedTosu, null);
		if (owned == null)
		{
			return;
		}
		try
		{
			if (!owned.HasExited)
			{
				owned.Kill();
			}
		}
		catch (Exception ex) when (ex is InvalidOperationException or Win32Exception) { }
		finally
		{
			owned.Dispose();
		}
	}

	private static async Task UpdateTosuDataAsync(CancellationToken ct)
	{
		int failures = 0;
		while (!ct.IsCancellationRequested)
		{
			try
			{
				if (_osuRunning && _tosuRunning)
				{
					try
					{
						Task<string> stateTask = PollClient.GetStringAsync(StateUrl, ct);
						Task<string> preciseTask = PollClient.GetStringAsync(PreciseUrl, ct);
						await Task.WhenAll(stateTask, preciseTask);

						TosuSnapshot snapshot = ParseSnapshot(JObject.Parse(await stateTask));
						int? preciseTime = (int?)JObject.Parse(await preciseTask).SelectToken("currentTime");

						if (snapshot.Client != null)
						{
							_isLazer = string.Equals(snapshot.Client, "lazer", StringComparison.OrdinalIgnoreCase);
						}
						Volatile.Write(ref _snapshot, snapshot);
						Volatile.Write(ref _preciseTime, preciseTime ?? NoTime);
						failures = 0;
					}
					catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or FormatException or InvalidCastException)
					{
						if (ct.IsCancellationRequested)
						{
							break;
						}
						if (++failures >= FailureLimit)
						{
							ClearData();
						}
					}
				}
				else
				{
					failures = 0;
					ClearData();
				}
				await Task.Delay(UpdateInterval, ct);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				break;
			}
		}
	}

	private static void ClearData()
	{
		Volatile.Write(ref _snapshot, null);
		Volatile.Write(ref _preciseTime, NoTime);
	}

	private static TosuSnapshot ParseSnapshot(JObject root)
	{
		return new TosuSnapshot(
			State: (string?)root.SelectToken("state.name"),
			Client: (string?)root.SelectToken("client"),
			LiveTime: (int?)root.SelectToken("beatmap.time.live"),
			Ar: (double?)root.SelectToken("beatmap.stats.ar.converted"),
			Cs: (double?)root.SelectToken("beatmap.stats.cs.original"),
			Mods: (string?)root.SelectToken("play.mods.name") ?? string.Empty,
			SongsFolder: (string?)root.SelectToken("folders.songs"),
			BeatmapFile: (string?)root.SelectToken("directPath.beatmapFile"));
	}

	private static string ResolvePluginFolder()
	{
		string userData = Path.Combine(AppContext.BaseDirectory, "userdata");
		return Directory.Exists(userData)
			? Path.Combine(userData, "Plugins", "Reconstructor")
			: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenTabletDriver", "Plugins", "Reconstructor");
	}

	private static async Task StartTosuAsync()
	{
		if (!await StartLock.WaitAsync(0))
		{
			return;
		}
		_isInitializing = true;
		try
		{
			string pluginFolder = ResolvePluginFolder();
			if (!Directory.Exists(pluginFolder))
			{
				Log.Write(LogGroup, "Plugin folder not found: " + pluginFolder, LogLevel.Warning);
				_retryStartAfter = DateTime.UtcNow + StartRetryBackoff;
				return;
			}

			string tosuPath = Path.Combine(pluginFolder, "tosu.exe");
			string tosuConfigPath = Path.Combine(pluginFolder, "tosu.env");
			if (!File.Exists(tosuPath))
			{
				await DownloadLatestTosuAsync(tosuPath);
			}
			if (!File.Exists(tosuPath))
			{
				_retryStartAfter = DateTime.UtcNow + StartRetryBackoff;
				return;
			}
			if (!File.Exists(tosuConfigPath))
			{
				await File.WriteAllTextAsync(tosuConfigPath, DefaultTosuConfig);
			}

			try
			{
				Process? started = Process.Start(new ProcessStartInfo
				{
					FileName = tosuPath,
					WorkingDirectory = pluginFolder
				});
				Process? previous = Interlocked.Exchange(ref _ownedTosu, started);
				previous?.Dispose();
				await Task.Delay(500);
			}
			catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
			{
				Log.Write(LogGroup, "Failed to start tosu: " + ex.Message, LogLevel.Error);
				_retryStartAfter = DateTime.UtcNow + StartRetryBackoff;
			}
		}
		finally
		{
			_isInitializing = false;
			StartLock.Release();
		}
	}

	private static async Task DownloadLatestTosuAsync(string tosuPath)
	{
		string tempZip = Path.Combine(Path.GetTempPath(), "tosu_" + Path.GetRandomFileName() + ".zip");
		try
		{
			using HttpRequestMessage request = new(HttpMethod.Get, ReleaseUrl);
			request.Headers.UserAgent.Add(new ProductInfoHeaderValue("TosuAutoUpdater", "1.0"));
			using HttpResponseMessage response = await DownloadClient.SendAsync(request);
			response.EnsureSuccessStatusCode();

			JObject release = JObject.Parse(await response.Content.ReadAsStringAsync());
			JToken? asset = (release["assets"] as JArray)?.FirstOrDefault(a =>
			{
				string? name = (string?)a["name"];
				return name != null
					&& name.Contains("windows", StringComparison.OrdinalIgnoreCase)
					&& name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
			});
			if (asset == null)
			{
				Log.Write(LogGroup, "Nothing usable found in tosu release", LogLevel.Warning);
				return;
			}

			string? downloadUrl = (string?)asset["browser_download_url"];
			string? digest = (string?)asset["digest"];
			if (string.IsNullOrEmpty(downloadUrl) || digest == null || !digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
			{
				Log.Write(LogGroup, "tosu release has no digest", LogLevel.Warning);
				return;
			}

			byte[] bytes = await DownloadClient.GetByteArrayAsync(downloadUrl);
			string actual = Convert.ToHexString(SHA256.HashData(bytes));
			if (!string.Equals(actual, digest["sha256:".Length..], StringComparison.OrdinalIgnoreCase))
			{
				Log.Write(LogGroup, "tosu digest invalid", LogLevel.Error);
				return;
			}

			await File.WriteAllBytesAsync(tempZip, bytes);
			using ZipArchive archive = ZipFile.OpenRead(tempZip);
			ZipArchiveEntry? entry = archive.Entries.FirstOrDefault(e => e.Name.Equals("tosu.exe", StringComparison.OrdinalIgnoreCase));
			if (entry == null)
			{
				Log.Write(LogGroup, "Nothing usable found in tosu release", LogLevel.Warning);
				return;
			}
			entry.ExtractToFile(tosuPath, overwrite: true);
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or IOException or InvalidDataException or UnauthorizedAccessException)
		{
			Log.Write(LogGroup, "tosu download failed: " + ex.Message, LogLevel.Error);
		}
		finally
		{
			try
			{
				File.Delete(tempZip);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
		}
	}
}
