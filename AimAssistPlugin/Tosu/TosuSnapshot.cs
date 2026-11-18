namespace AimAssistPlugin.Tosu;

internal sealed record TosuSnapshot(
	string? State,
	string? Client,
	int? LiveTime,
	double? Ar,
	double? Cs,
	string Mods,
	string? SongsFolder,
	string? BeatmapFile);
