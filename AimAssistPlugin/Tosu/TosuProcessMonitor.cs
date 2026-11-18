using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AimAssistPlugin.Tosu;

internal static class TosuProcessMonitor
{
	public static async Task RunAsync(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			try
			{
				if (!TosuLauncher.IsStarting)
				{
					TosuState.OsuRunning = IsAnyRunning(TosuSettings.GameProcessNames);
					TosuState.TosuRunning = IsAnyRunning(TosuSettings.TosuProcessName);
					if (TosuState.OsuRunning && !TosuState.TosuRunning)
					{
						if (TosuLauncher.CanRetryNow)
						{
							await TosuLauncher.StartAsync();
						}
					}
					else if (!TosuState.OsuRunning && TosuState.TosuRunning)
					{
						TosuLauncher.StopOwned();
					}
				}
				TosuLog.ClearLastError();
				await Task.Delay(TosuSettings.MonitorInterval, ct);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				TosuLog.ErrorOnce("tosu monitor error", ex);
				try
				{
					await Task.Delay(TosuSettings.MonitorInterval, ct);
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}
		}
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
}
