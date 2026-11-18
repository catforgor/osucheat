using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AimAssistPlugin.Tosu;

internal static class TosuDataPoller
{
	private static readonly HttpClient PollClient = new() { Timeout = TosuSettings.PollTimeout };

	public static async Task RunAsync(CancellationToken ct)
	{
		int failures = 0;
		while (!ct.IsCancellationRequested)
		{
			try
			{
				if (TosuState.OsuRunning && TosuState.TosuRunning)
				{
					try
					{
						Task<string> stateTask = PollClient.GetStringAsync(TosuSettings.StateUrl, ct);
						Task<string> preciseTask = PollClient.GetStringAsync(TosuSettings.PreciseUrl, ct);
						await Task.WhenAll(stateTask, preciseTask);

						TosuSnapshot snapshot = TosuSnapshotParser.Parse(JObject.Parse(await stateTask));
						int? preciseTime = TosuSnapshotParser.ParsePreciseTime(JObject.Parse(await preciseTask));

						TosuState.Publish(snapshot, preciseTime);
						failures = 0;
					}
					catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or FormatException or InvalidCastException or ArgumentException)
					{
						if (ct.IsCancellationRequested)
						{
							break;
						}
						if (++failures >= TosuSettings.FailureLimit)
						{
							TosuState.Clear();
						}
					}
				}
				else
				{
					failures = 0;
					TosuState.Clear();
				}
				await Task.Delay(TosuSettings.UpdateInterval, ct);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				TosuLog.ErrorOnce("tosu poll error", ex);
				TosuState.Clear();
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
}
