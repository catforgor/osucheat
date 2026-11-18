using AimAssistPlugin.Tosu;

namespace AimAssistPlugin.Game;

internal static class SongClock
{
	public static int GetTime()
	{
		return TosuState.PreciseTime ?? TosuState.Snapshot?.LiveTime ?? 0;
	}
}
