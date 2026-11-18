using System;
using System.Collections.Generic;
using AimAssistPlugin.Tosu;

namespace AimAssistPlugin.Beatmaps;

internal static class BeatmapDifficulty
{
	private const double PreEmptAtAr0 = 1800.0;
	private const double PreEmptAtAr5 = 1200.0;
	private const double PreEmptAtAr10 = 450.0;

	private const float RadiusAtCs0 = 54.4f;
	private const float RadiusPerCs = 4.48f;

	private const double EasyCsMultiplier = 0.5;
	private const double HardRockCsMultiplier = 1.3;
	private const double MaxCs = 10.0;

	public static int GetPreEmpt()
	{
		double? ar = TosuState.Snapshot?.Ar;
		return ar.HasValue ? (int)MapDifficultyRange(ar.Value, PreEmptAtAr0, PreEmptAtAr5, PreEmptAtAr10) : 0;
	}

	public static float GetHitObjectRadius()
	{
		TosuSnapshot? snapshot = TosuState.Snapshot;
		if (snapshot?.Cs == null)
		{
			return 0f;
		}
		double cs = snapshot.Cs.Value;
		HashSet<string> mods = ModParser.Parse(snapshot.Mods);
		if (mods.Contains("EZ"))
		{
			cs *= EasyCsMultiplier;
		}
		else if (mods.Contains("HR"))
		{
			cs = Math.Min(cs * HardRockCsMultiplier, MaxCs);
		}
		return RadiusAtCs0 - RadiusPerCs * (float)cs;
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
}
