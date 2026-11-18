using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
			TimeSpan delay = TosuSettings.UpdateInterval;
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
			catch (Exception) when (!ct.IsCancellationRequested)
			{
				if (++failures >= TosuSettings.FailureLimit)
				{
					TosuState.Clear();
					delay = TosuSettings.RetryDelay;
				}
			}
			try
			{
				await Task.Delay(delay, ct);
			}
			catch (OperationCanceledException)
			{
				break;
			}
		}
	}
}
