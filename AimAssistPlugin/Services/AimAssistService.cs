using System;
using System.Numerics;
using AimAssistPlugin.Sdk.Audio;
using AimAssistPlugin.Sdk.Input;
using AimAssistPlugin.Sdk.Osu;
using AimAssistPlugin.Sdk.Player;
using OsuParsers.Beatmaps.Objects;

namespace AimAssistPlugin.Services;

public static class AimAssistService
{
	private readonly struct AimFrameCache(float scaledRadius, float fovRadius, Vector2 targetPosition)
	{
		public readonly float ScaledRadiusSquared = scaledRadius * scaledRadius;

		public readonly float FovSquared = fovRadius * fovRadius;

		public readonly Vector2 TargetPosition = targetPosition;
	}

	private const long InitRetryIntervalMs = 500;

	public static volatile float Power = 0f;

	private static int preEmpt;

	private static float hitObjectRadius;

	private static int lastAudioTime = -1;

	private static int currentIndex;

	private static long nextInitAttemptMs;

	private static HitObject? currentHitObject;

	private static HitObject? previousHitObject;

	public static bool EnsureInitialized()
	{
		if (HitObjectManager.GetHitObjectsCount() > 0)
		{
			return true;
		}
		long now = Environment.TickCount64;
		if (now < nextInitAttemptMs)
		{
			return false;
		}
		Initialize();
		if (HitObjectManager.GetHitObjectsCount() == 0)
		{
			nextInitAttemptMs = now + InitRetryIntervalMs;
			return false;
		}
		return true;
	}

	public static void Initialize()
	{
		HitObjectManager.CacheHitObjects();
		preEmpt = HitObjectManager.GetPreEmpt();
		hitObjectRadius = HitObjectManager.GetHitObjectRadius();
		lastAudioTime = -1;
		currentIndex = 0;
		currentHitObject = HitObjectManager.GetHitObjectsCount() > 0 ? HitObjectManager.GetHitObject(0) : null;
		previousHitObject = null;
		InputManager.SetAccumulatedOffset(Vector2.Zero);
		InputManager.SetLastCursorPosition(Vector2.Zero);
	}

	public static void Reset()
	{
		lastAudioTime = -1;
		currentIndex = 0;
		currentHitObject = null;
		previousHitObject = null;
	}

	private static Vector2 ApplyAimAssist(HitObject target, Vector2 cursorPosition, int time)
	{
		Vector2 offset = InputManager.GetAccumulatedOffset();
		if (Power <= 0f)
		{
			return offset;
		}

		float ratio = GameField.GetRatio();
		float scaledRadius = hitObjectRadius * ratio;
		float maxOffset = Power * 16f * ratio;
		float strength = Power <= 1f ? Power * 0.7f : (Power - 1f) * 0.15f + 0.7f;
		float fovRadius = Math.Max(1.2f, 4f * strength) * scaledRadius;

		AimFrameCache frame = new(scaledRadius, fovRadius, target.Position);
		Vector2 assistedCursor = cursorPosition + offset;
		Vector2 toTarget = frame.TargetPosition - assistedCursor;
		float distanceSquared = Vector2.DistanceSquared(assistedCursor, frame.TargetPosition);
		float insideFactor = distanceSquared <= frame.ScaledRadiusSquared ? 0.65f : 1f;
		Vector2 displacement = InputManager.GetLastCursorPosition() - cursorPosition;

		if (target is Spinner)
		{
			InputManager.SetAccumulatedOffset(offset);
			return offset;
		}

		bool insideFov = Vector2.DistanceSquared(cursorPosition, frame.TargetPosition) <= frame.FovSquared;
		bool approachStarted = time > target.StartTime - preEmpt;
		if (insideFov & approachStarted)
		{
			if (target is Slider slider)
			{
				float sliderCoreRadius = scaledRadius * 0.35f;
				if (distanceSquared <= sliderCoreRadius * sliderCoreRadius)
				{
					float distance = toTarget.Length();
					if (distance > float.Epsilon)
					{
						strength *= Math.Min(distance / sliderCoreRadius, 1f);
					}
				}
				if (time > slider.StartTime)
				{
					strength *= Power / 2.5f;
				}
			}

			float toTargetSquared = toTarget.LengthSquared();
			float displacementSquared = displacement.LengthSquared();
			if (toTargetSquared > float.Epsilon && displacementSquared > float.Epsilon)
			{
				float distance = MathF.Sqrt(toTargetSquared);
				float moved = MathF.Sqrt(displacementSquared);
				float step = Math.Min(distance, moved);
				offset += toTarget / distance * (insideFactor * strength * step);
				offset = new Vector2(Math.Clamp(offset.X, -maxOffset, maxOffset), Math.Clamp(offset.Y, -maxOffset, maxOffset));
			}
		}
		else
		{
			offset = InputManager.Resync(displacement, offset, 0.6f * strength);
		}

		InputManager.SetAccumulatedOffset(offset);
		return offset;
	}

	public static Vector2 GetOffset(Vector2 cursorPosition)
	{
		int time = AudioEngine.GetTime();
		if (lastAudioTime != -1 && time < lastAudioTime)
		{
			Initialize();
		}
		lastAudioTime = time;

		HitObject? current = currentHitObject;
		if (current == null)
		{
			InputManager.SetLastCursorPosition(cursorPosition);
			return InputManager.GetAccumulatedOffset();
		}

		if (time > current.EndTime)
		{
			int nextIndex = currentIndex + 1;
			if (nextIndex >= HitObjectManager.GetHitObjectsCount())
			{
				Vector2 resynced = InputManager.Resync(InputManager.GetLastCursorPosition() - cursorPosition, InputManager.GetAccumulatedOffset(), 0.5f);
				InputManager.SetAccumulatedOffset(resynced);
				InputManager.SetLastCursorPosition(cursorPosition);
				return resynced;
			}
			currentIndex = nextIndex;
			previousHitObject = current;
			current = HitObjectManager.GetHitObject(currentIndex);
			currentHitObject = current;
			current.Position = GameField.FieldToDisplay(current.Position) + GameField.CalculateScreenOffset();
		}

		Vector2 accumulatedOffset = InputManager.GetAccumulatedOffset();
		if (currentIndex == 0)
		{
			InputManager.SetLastCursorPosition(cursorPosition);
			return accumulatedOffset;
		}

		int gap = previousHitObject != null ? current.StartTime - previousHitObject.EndTime : int.MaxValue;
		if (gap > preEmpt * 2)
		{
			InputManager.SetLastCursorPosition(cursorPosition);
			return accumulatedOffset;
		}

		Vector2 result = current switch
		{
			HitCircle => ApplyAimAssist(current, cursorPosition, time),
			Slider slider when time < slider.StartTime => ApplyAimAssist(current, cursorPosition, time),
			_ => accumulatedOffset,
		};
		InputManager.SetLastCursorPosition(cursorPosition);
		return result;
	}
}
