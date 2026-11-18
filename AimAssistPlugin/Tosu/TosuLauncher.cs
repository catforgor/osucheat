using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AimAssistPlugin.Tosu;

internal static class TosuLauncher
{
	private static readonly SemaphoreSlim StartLock = new(1, 1);

	private static Process? _ownedTosu;

	private static DateTime _retryStartAfter = DateTime.MinValue;

	public static volatile bool IsStarting;

	public static bool CanRetryNow => DateTime.UtcNow >= _retryStartAfter;

	public static async Task StartAsync()
	{
		if (!await StartLock.WaitAsync(0))
		{
			return;
		}
		IsStarting = true;
		try
		{
			string pluginFolder = TosuInstaller.ResolvePluginFolder();
			if (!Directory.Exists(pluginFolder))
			{
				TosuLog.Warning("Plugin folder not found: " + pluginFolder);
				BackOff();
				return;
			}

			string tosuPath = TosuInstaller.ExePath(pluginFolder);
			string tosuConfigPath = TosuInstaller.ConfigPath(pluginFolder);
			if (!File.Exists(tosuPath))
			{
				await TosuInstaller.DownloadLatestAsync(tosuPath);
			}
			if (!File.Exists(tosuPath))
			{
				BackOff();
				return;
			}
			if (!File.Exists(tosuConfigPath))
			{
				await TosuInstaller.WriteDefaultConfigAsync(tosuConfigPath);
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
				TosuLog.Error("Failed to start tosu: " + ex.Message);
				BackOff();
			}
		}
		finally
		{
			IsStarting = false;
			StartLock.Release();
		}
	}

	public static void StopOwned()
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
		catch (Exception ex) when (ex is InvalidOperationException or Win32Exception) { /* already exited, or access denied */ }
		finally
		{
			owned.Dispose();
		}
	}

	private static void BackOff()
	{
		_retryStartAfter = DateTime.UtcNow + TosuSettings.StartRetryBackoff;
	}
}
