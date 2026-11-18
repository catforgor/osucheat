using AimAssistPlugin.Tosu;

namespace AimAssistPlugin.Game;

internal static class PlayState
{
	public static bool IsPlaying => TosuState.Snapshot?.State == "play";
}
