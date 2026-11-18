using System;
using System.Numerics;

namespace AimAssistPlugin.Input;

internal static class PositionRounding
{
	public static float Round(float value)
	{
		return MathF.Round(value, MidpointRounding.AwayFromZero);
	}

	public static Vector2 Round(Vector2 value)
	{
		return new Vector2(Round(value.X), Round(value.Y));
	}
}
