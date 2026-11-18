using System.Threading;
using System.Threading.Tasks;

namespace AimAssistPlugin.Tosu;

internal static class TosuStartup
{
	private static readonly Lock LifecycleLock = new();

	private static volatile bool _isStarted;

	private static CancellationTokenSource? _cts;

	public static void EnsureStarted()
	{
		if (_isStarted)
		{
			return;
		}
		lock (LifecycleLock)
		{
			if (_isStarted)
			{
				return;
			}
			_cts?.Cancel();
			_cts?.Dispose();
			_cts = new CancellationTokenSource();
			CancellationToken token = _cts.Token;
			_ = Task.Run(() => TosuDataPoller.RunAsync(token));
			_isStarted = true;
		}
	}
}
