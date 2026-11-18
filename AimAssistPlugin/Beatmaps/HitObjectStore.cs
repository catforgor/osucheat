using System.Collections.Generic;
using OsuParsers.Beatmaps.Objects;

namespace AimAssistPlugin.Beatmaps;

internal static class HitObjectStore
{
	private static List<HitObject> hitObjects = new();

	public static int Count => hitObjects.Count;

	public static void Load()
	{
		hitObjects = BeatmapLoader.Load();
	}

	public static void Clear()
	{
		hitObjects.Clear();
	}

	public static HitObject Get(int index)
	{
		return hitObjects[index];
	}
}
