using System.Collections.Generic;
using System.IO;
using System.Numerics;
using AimAssistPlugin.Tosu;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Decoders;

namespace AimAssistPlugin.Beatmaps;

internal static class BeatmapLoader
{
	private const float PlayfieldHeight = 384f;

	public static List<HitObject> Load()
	{
		TosuSnapshot? snapshot = TosuState.Snapshot;
		if (snapshot?.SongsFolder == null || snapshot.BeatmapFile == null)
		{
			return new List<HitObject>();
		}
		string path = Path.Combine(snapshot.SongsFolder, snapshot.BeatmapFile);
		if (!File.Exists(path))
		{
			return new List<HitObject>();
		}
		List<HitObject> objects = BeatmapDecoder.Decode(path).HitObjects;
		if (ModParser.Parse(snapshot.Mods).Contains("HR"))
		{
			foreach (HitObject hitObject in objects)
			{
				hitObject.Position = new Vector2(hitObject.Position.X, PlayfieldHeight - hitObject.Position.Y);
			}
		}
		return objects;
	}
}
