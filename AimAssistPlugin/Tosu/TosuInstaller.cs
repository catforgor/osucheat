using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AimAssistPlugin.Tosu;

internal static class TosuInstaller
{
	private static readonly HttpClient DownloadClient = new() { Timeout = TosuSettings.DownloadTimeout };

	public static string ResolvePluginFolder()
	{
		string userData = Path.Combine(AppContext.BaseDirectory, "userdata");
		return Directory.Exists(userData)
			? Path.Combine(userData, "Plugins", "Reconstructor")
			: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenTabletDriver", "Plugins", "Reconstructor");
	}

	public static string ExePath(string pluginFolder)
	{
		return Path.Combine(pluginFolder, "tosu.exe");
	}

	public static string ConfigPath(string pluginFolder)
	{
		return Path.Combine(pluginFolder, "tosu.env");
	}

	public static Task WriteDefaultConfigAsync(string configPath)
	{
		return File.WriteAllTextAsync(configPath, TosuSettings.DefaultConfig);
	}

	public static async Task DownloadLatestAsync(string tosuPath)
	{
		string tempZip = Path.Combine(Path.GetTempPath(), "tosu_" + Path.GetRandomFileName() + ".zip");
		try
		{
			using HttpRequestMessage request = new(HttpMethod.Get, TosuSettings.ReleaseUrl);
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
				TosuLog.Warning("Nothing usable found in tosu release");
				return;
			}

			string? downloadUrl = (string?)asset["browser_download_url"];
			string? digest = (string?)asset["digest"];
			if (string.IsNullOrEmpty(downloadUrl) || digest == null || !digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
			{
				TosuLog.Warning("tosu release has no digest");
				return;
			}
			if (!downloadUrl.StartsWith(TosuSettings.TrustedDownloadPrefix, StringComparison.OrdinalIgnoreCase))
			{
				TosuLog.Error("tosu download URL is not from the tosu GitHub repository; refusing: " + downloadUrl);
				return;
			}

			byte[] bytes = await DownloadClient.GetByteArrayAsync(downloadUrl);
			string actual = Convert.ToHexString(SHA256.HashData(bytes));
			if (!string.Equals(actual, digest["sha256:".Length..], StringComparison.OrdinalIgnoreCase))
			{
				TosuLog.Error("tosu digest invalid");
				return;
			}

			await File.WriteAllBytesAsync(tempZip, bytes);
			using ZipArchive archive = ZipFile.OpenRead(tempZip);
			ZipArchiveEntry? entry = archive.Entries.FirstOrDefault(e => e.Name.Equals("tosu.exe", StringComparison.OrdinalIgnoreCase));
			if (entry == null)
			{
				TosuLog.Warning("Nothing usable found in tosu release");
				return;
			}
			entry.ExtractToFile(tosuPath, overwrite: true);
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or IOException or InvalidDataException or UnauthorizedAccessException)
		{
			TosuLog.Error("tosu download failed: " + ex.Message);
		}
		finally
		{
			try
			{
				File.Delete(tempZip);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { /* best-effort temp cleanup */ }
		}
	}
}
