using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using AimAssistPlugin.Services;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Decoders;

namespace AimAssistPlugin.Sdk.Player;

internal partial class HitObjectManager
{
	private const float PlayfieldHeight = 384f;

	public static List<HitObject> hitObjects = new List<HitObject>();

	public static void CacheHitObjects()
	{
		hitObjects = GetAndTransformHitObjects();
	}

	public static HitObject GetHitObject(int index)
	{
		return hitObjects[index];
	}

	public static int GetHitObjectsCount()
	{
		return hitObjects.Count;
	}

	// AR0 = 1800ms AR5 = 1200ms AR10 = 450ms
	public static int GetPreEmpt()
	{
		double? ar = TosuService.Snapshot?.Ar;
		return ar.HasValue ? (int)MapDifficultyRange(ar.Value, 1800.0, 1200.0, 450.0) : 0;
	}

	public static float GetHitObjectRadius()
	{
		TosuSnapshot? snapshot = TosuService.Snapshot;
		if (snapshot?.Cs == null)
		{
			return 0f;
		}
		double cs = snapshot.Cs.Value;
		HashSet<string> mods = ParseMods(snapshot.Mods);
		if (mods.Contains("EZ"))
		{
			cs *= 0.5;
		}
		else if (mods.Contains("HR"))
		{
			cs = Math.Min(cs * 1.3, 10.0);
		}
		return 54.4f - 4.48f * (float)cs;
	}

	public static double MapDifficultyRange(double difficulty, double min, double mid, double max)
	{
		if (difficulty > 5.0)
		{
			return mid + (max - mid) * (difficulty - 5.0) / 5.0;
		}
		if (difficulty < 5.0)
		{
			return mid - (mid - min) * (5.0 - difficulty) / 5.0;
		}
		return mid;
	}

	private static List<HitObject> GetAndTransformHitObjects()
	{
		TosuSnapshot? snapshot = TosuService.Snapshot;
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
		if (ParseMods(snapshot.Mods).Contains("HR"))
		{
			foreach (HitObject hitObject in objects)
			{
				hitObject.Position = new Vector2(hitObject.Position.X, PlayfieldHeight - hitObject.Position.Y);
			}
		}
		return objects;
	}

	internal static HashSet<string> ParseMods(string? mods)
	{
		if (string.IsNullOrWhiteSpace(mods))
		{
			return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		return ModToken().Matches(mods.ToUpperInvariant())
			.Select(m => m.Value)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
	}

	[GeneratedRegex("[A-Z]{2}")]
	private static partial Regex ModToken();
}
