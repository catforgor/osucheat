using AimAssistPlugin.Services;

namespace AimAssistPlugin.Sdk.Player;

internal static class Player
{
	public static bool IsPlaying => TosuService.Snapshot?.State == "play";
}
