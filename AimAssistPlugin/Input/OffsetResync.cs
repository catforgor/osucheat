using System;
using System.Numerics;

namespace AimAssistPlugin.Input;

internal static class OffsetResync
{
	public static Vector2 Apply(Vector2 displacement, Vector2 offset, float resyncFactor)
	{
		if (offset.Length() <= float.Epsilon)
		{
			return offset;
		}
		Vector2 movement = displacement * resyncFactor;
		return new Vector2(ResyncAxis(offset.X, movement.X), ResyncAxis(offset.Y, movement.Y));
	}

	private static float ResyncAxis(float offset, float movement)
	{
		return offset > 0f
			? Math.Max(0f, offset - MathF.Abs(movement))
			: Math.Min(0f, offset + MathF.Abs(movement));
	}
}
