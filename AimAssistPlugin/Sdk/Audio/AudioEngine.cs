using AimAssistPlugin.Services;

namespace AimAssistPlugin.Sdk.Audio;

internal static class AudioEngine
{
	public static int GetTime()
	{
		return TosuService.PreciseTime ?? TosuService.Snapshot?.LiveTime ?? 0;
	}
}
