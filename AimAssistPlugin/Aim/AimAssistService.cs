using System;
using System.Numerics;
using AimAssistPlugin.Game;
using AimAssistPlugin.Beatmaps;
using AimAssistPlugin.Input;
using OsuParsers.Beatmaps.Objects;

namespace AimAssistPlugin.Aim;

internal static class AimAssistService
{
	private const int NoAudioTime = int.MinValue;

	public static volatile float Power = 0f;

	private static int preEmpt;

	private static float hitObjectRadius;

	private static int lastAudioTime = NoAudioTime;

	private static long nextInitAttemptMs;

	public static bool EnsureInitialized()
	{
		if (HitObjectStore.Count > 0)
		{
			return true;
		}

		long now = Environment.TickCount64;
		if (now < nextInitAttemptMs)
		{
			return false;
		}

		Initialize();

		if (HitObjectStore.Count == 0 || hitObjectRadius <= 0f || preEmpt <= 0)
		{
			HitObjectStore.Clear();
			Reset();
			nextInitAttemptMs = now + AimTuning.InitRetryIntervalMs;
			return false;
		}
		return true;
	}

	public static void Initialize()
	{
		HitObjectStore.Load();
		preEmpt = BeatmapDifficulty.GetPreEmpt();
		hitObjectRadius = BeatmapDifficulty.GetHitObjectRadius();
		lastAudioTime = NoAudioTime;
		HitObjectTracker.Begin();
		InputManager.SetAccumulatedOffset(Vector2.Zero);
		InputManager.SetLastCursorPosition(Vector2.Zero);
	}

	public static void Reset()
	{
		lastAudioTime = NoAudioTime;
		HitObjectTracker.Clear();
	}

	public static Vector2 GetOffset(Vector2 cursorPosition)
	{
		int time = SongClock.GetTime();
		if (lastAudioTime != NoAudioTime && time < lastAudioTime - AimTuning.RewindThresholdMs)
		{
			Initialize();
		}
		lastAudioTime = Math.Max(lastAudioTime, time);

		HitObject? current = HitObjectTracker.Current;
		if (current == null)
		{
			InputManager.SetLastCursorPosition(cursorPosition);
			return InputManager.GetAccumulatedOffset();
		}

		if (time > current.EndTime)
		{
			if (!HitObjectTracker.TryAdvance())
			{
				Vector2 resynced = OffsetResync.Apply(InputManager.GetLastCursorPosition() - cursorPosition, InputManager.GetAccumulatedOffset(), AimTuning.ResyncAtMapEnd);
				InputManager.SetAccumulatedOffset(resynced);
				InputManager.SetLastCursorPosition(cursorPosition);
				return resynced;
			}
			current = HitObjectTracker.Current;
		}

		Vector2 accumulatedOffset = InputManager.GetAccumulatedOffset();
		if (HitObjectTracker.Index == 0)
		{
			InputManager.SetLastCursorPosition(cursorPosition);
			return accumulatedOffset;
		}

		HitObject? previous = HitObjectTracker.Previous;
		int gap = previous != null ? current.StartTime - previous.EndTime : int.MaxValue;
		if (gap > preEmpt * AimTuning.MaxGapInPreEmpts)
		{
			InputManager.SetLastCursorPosition(cursorPosition);
			return accumulatedOffset;
		}

		Vector2 result = current switch
		{
			HitCircle => AimAlgorithm.Apply(current, cursorPosition, time, Power, preEmpt, hitObjectRadius),
			Slider slider when time < slider.StartTime => AimAlgorithm.Apply(current, cursorPosition, time, Power, preEmpt, hitObjectRadius),
			_ => accumulatedOffset,
		};
		InputManager.SetLastCursorPosition(cursorPosition);
		return result;
	}
}
