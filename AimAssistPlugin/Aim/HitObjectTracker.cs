using AimAssistPlugin.Beatmaps;
using AimAssistPlugin.Osu;
using OsuParsers.Beatmaps.Objects;

namespace AimAssistPlugin.Aim;

internal static class HitObjectTracker
{
	public static HitObject? Current { get; private set; }

	public static HitObject? Previous { get; private set; }

	public static int Index { get; private set; }

	public static void Begin()
	{
		Index = 0;
		Current = HitObjectStore.Count > 0 ? HitObjectStore.Get(0) : null;
		Previous = null;
	}

	public static void Clear()
	{
		Index = 0;
		Current = null;
		Previous = null;
	}

	public static bool TryAdvance()
	{
		int nextIndex = Index + 1;
		if (nextIndex >= HitObjectStore.Count)
		{
			return false;
		}
		Index = nextIndex;
		Previous = Current;
		Current = HitObjectStore.Get(Index);
		Current.Position = GameField.FieldToDisplay(Current.Position) + GameField.CalculateScreenOffset();
		return true;
	}
}
