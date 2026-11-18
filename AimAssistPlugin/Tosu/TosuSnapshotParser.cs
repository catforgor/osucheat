using Newtonsoft.Json.Linq;

namespace AimAssistPlugin.Tosu;

internal static class TosuSnapshotParser
{
	public static TosuSnapshot Parse(JObject root)
	{
		return new TosuSnapshot(
			State: (string?)root.SelectToken("state.name"),
			Client: (string?)root.SelectToken("client"),
			LiveTime: (int?)root.SelectToken("beatmap.time.live"),
			Ar: (double?)root.SelectToken("beatmap.stats.ar.converted"),
			Cs: (double?)root.SelectToken("beatmap.stats.cs.original"),
			Mods: (string?)root.SelectToken("play.mods.name") ?? string.Empty,
			SongsFolder: (string?)root.SelectToken("folders.songs"),
			BeatmapFile: (string?)root.SelectToken("directPath.beatmapFile"));
	}

	public static int? ParsePreciseTime(JObject root)
	{
		return (int?)root.SelectToken("currentTime");
	}
}
