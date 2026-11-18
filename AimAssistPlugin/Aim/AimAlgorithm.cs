using System;
using System.Numerics;
using AimAssistPlugin.Input;
using AimAssistPlugin.Osu;
using OsuParsers.Beatmaps.Objects;

namespace AimAssistPlugin.Aim;

internal static class AimAlgorithm
{
	public static Vector2 Apply(HitObject target, Vector2 cursorPosition, int time, float power, int preEmpt, float hitObjectRadius)
	{
		Vector2 offset = InputManager.GetAccumulatedOffset();
		if (power <= 0f)
		{
			return offset;
		}

		float ratio = GameField.GetRatio();
		float scaledRadius = hitObjectRadius * ratio;
		float maxOffset = power * AimTuning.MaxOffsetPerPower * ratio;
		float strength = StrengthFor(power);
		float fovRadius = Math.Max(AimTuning.MinFovInCircleRadii, AimTuning.FovPerStrength * strength) * scaledRadius;

		AimFrame frame = new(scaledRadius, fovRadius, target.Position);
		Vector2 assistedCursor = cursorPosition + offset;
		Vector2 toTarget = frame.TargetPosition - assistedCursor;
		float distanceSquared = Vector2.DistanceSquared(assistedCursor, frame.TargetPosition);
		float insideFactor = distanceSquared <= frame.ScaledRadiusSquared ? AimTuning.InsideCircleFactor : 1f;
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
				float sliderCoreRadius = scaledRadius * AimTuning.SliderCoreFraction;
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
					strength *= power / AimTuning.SliderBodyPowerDivisor;
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
			offset = OffsetResync.Apply(displacement, offset, AimTuning.ResyncOutsideFov * strength);
		}

		InputManager.SetAccumulatedOffset(offset);
		return offset;
	}

	private static float StrengthFor(float power)
	{
		return power <= AimTuning.FullPower
			? power * AimTuning.StrengthAtFullPower
			: (power - AimTuning.FullPower) * AimTuning.StrengthPerExtraPower + AimTuning.StrengthAtFullPower;
	}
}
