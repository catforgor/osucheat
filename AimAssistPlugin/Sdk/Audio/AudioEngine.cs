using AimAssistPlugin.Services;

namespace AimAssistPlugin.Sdk.Audio;

internal class AudioEngine
{
	public static int GetTime()
	{
		try
		{
			dynamic? precise = TosuService.LatestPreciseResponse;
			if (precise?.currentTime != null)
			{
				return (int)precise.currentTime;
			}
			dynamic? response = TosuService.LatestResponse;
			if (response?.beatmap?.time?.live != null)
			{
				return (int)response.beatmap.time.live;
			}
		}
		catch
		{
		}
		return 0;
	}
}
