using System;
using System.Numerics;
using AimAssistPlugin.Sdk.Audio;
using AimAssistPlugin.Sdk.Input;
using AimAssistPlugin.Sdk.Osu;
using AimAssistPlugin.Sdk.Player;
using OsuParsers.Beatmaps.Objects;

namespace AimAssistPlugin.Services;

public class AimAssistService
{
	private readonly struct AimFrameCache(float scaledRadius, float fovRadius, Vector2 targetPosition)
	{
		public readonly float ScaledRadiusSquared = scaledRadius * scaledRadius;

		public readonly float FovSquared = fovRadius * fovRadius;

		public readonly Vector2 TargetPosition = targetPosition;
	}

	public static float Power = 0f;

	private static readonly Vector2 monitorOffsets = GameField.CalculateScreenOffset();

	private static int preEmpt;

	private static float hitObjectRadius;

	private static int lastAudioTime = -1;

	private static int currentIndex;

	private static HitObject? currentHitObject;

	private static HitObject? previousHitObject;

	public static void Initialize()
	{
		HitObjectManager.CacheHitObjects();
		preEmpt = HitObjectManager.GetPreEmpt();
		hitObjectRadius = HitObjectManager.GetHitObjectRadius();
		lastAudioTime = -1;
		currentIndex = 0;
		currentHitObject = HitObjectManager.GetHitObject(0);
		previousHitObject = HitObjectManager.GetHitObject(1);
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

	private static Vector2 ApplyAimAssist(Vector2 cursorPosition)
	{
		int time = AudioEngine.GetTime();
		if (Power <= 0f)
		{
			return InputManager.GetAccumulatedOffset();
		}
		float ratio = GameField.GetRatio();
		float num = hitObjectRadius * ratio;
		float num2 = Power * 16f * ratio;
		float num3 = ((Power <= 1f) ? (Power * 0.7f) : ((Power - 1f) * 0.15f + 0.7f));
		float fovRadius = Math.Max(1.2f, 4f * num3) * num;
		HitObject hitObject = currentHitObject;
		AimFrameCache aimFrameCache = new AimFrameCache(num, fovRadius, hitObject.Position);
		Vector2 vector = InputManager.GetAccumulatedOffset();
		Vector2 vector2 = cursorPosition + vector;
		Vector2 vector3 = aimFrameCache.TargetPosition - vector2;
		float num4 = Vector2.DistanceSquared(vector2, aimFrameCache.TargetPosition);
		float num5 = ((num4 <= aimFrameCache.ScaledRadiusSquared) ? 0.65f : 1f);
		Vector2 displacement = InputManager.GetLastCursorPosition() - cursorPosition;
		if (hitObject is Spinner)
		{
			InputManager.SetAccumulatedOffset(vector);
			return vector;
		}
		bool flag = Vector2.DistanceSquared(cursorPosition, aimFrameCache.TargetPosition) <= aimFrameCache.FovSquared;
		bool flag2 = time > hitObject.StartTime - preEmpt;
		if (flag & flag2)
		{
			if (hitObject is Slider slider)
			{
				float num6 = num * 0.35f;
				float num7 = num6 * num6;
				if (num4 <= num7)
				{
					float num8 = vector3.Length();
					if (num8 > float.Epsilon)
					{
						num3 *= Math.Min(num8 / num6, 1f);
					}
				}
				if (time > slider.StartTime)
				{
					num3 *= Power / 2.5f;
				}
			}
			float num9 = vector3.LengthSquared();
			float num10 = displacement.LengthSquared();
			if (num9 > float.Epsilon && num10 > float.Epsilon)
			{
				float num11 = MathF.Sqrt(num9);
				float val = MathF.Sqrt(num10);
				float num12 = Math.Min(num11, val);
				Vector2 vector4 = vector3 / num11 * (num5 * num3 * num12);
				vector += vector4;
				vector = new Vector2(Math.Clamp(vector.X, 0f - num2, num2), Math.Clamp(vector.Y, 0f - num2, num2));
			}
		}
		else
		{
			vector = InputManager.Resync(displacement, vector, 0.6f * num3);
		}
		InputManager.SetAccumulatedOffset(vector);
		return vector;
	}

	public static Vector2 GetOffset(Vector2 cursorPosition)
	{
		int time = AudioEngine.GetTime();
		if (lastAudioTime != -1 && time < lastAudioTime)
		{
			Initialize();
		}
		lastAudioTime = time;
		if (currentHitObject != null && time > currentHitObject.EndTime)
		{
			int num = currentIndex + 1;
			if (num >= HitObjectManager.GetHitObjectsCount())
			{
				Vector2 accumulatedOffset = InputManager.GetAccumulatedOffset();
				Vector2 vector = InputManager.Resync(InputManager.GetLastCursorPosition() - cursorPosition, accumulatedOffset, 0.5f);
				InputManager.SetAccumulatedOffset(vector);
				InputManager.SetLastCursorPosition(cursorPosition);
				return vector;
			}
			currentIndex = num;
			currentHitObject = HitObjectManager.GetHitObject(currentIndex);
			previousHitObject = HitObjectManager.GetHitObject(currentIndex - 1);
			currentHitObject.Position = GameField.FieldToDisplay(currentHitObject.Position) + monitorOffsets;
			previousHitObject.Position = GameField.FieldToDisplay(previousHitObject.Position) + monitorOffsets;
		}
		Vector2 accumulatedOffset2 = InputManager.GetAccumulatedOffset();
		if (currentIndex == 0)
		{
			InputManager.SetLastCursorPosition(cursorPosition);
			return accumulatedOffset2;
		}
		if (((previousHitObject != null) ? (currentHitObject.StartTime - previousHitObject.EndTime) : int.MaxValue) > preEmpt * 2)
		{
			InputManager.SetLastCursorPosition(cursorPosition);
			return accumulatedOffset2;
		}
		Vector2 result;
		if (currentHitObject is HitCircle)
		{
			result = ApplyAimAssist(cursorPosition);
		}
		else if (currentHitObject is Slider slider)
		{
			result = ((time < slider.StartTime) ? ApplyAimAssist(cursorPosition) : accumulatedOffset2);
		}
		else
		{
			result = accumulatedOffset2;
		}
		InputManager.SetLastCursorPosition(cursorPosition);
		return result;
	}
}
