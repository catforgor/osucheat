using System;
using System.Numerics;

namespace AimAssistPlugin.Sdk.Input;

internal static class InputManager
{
	private static Vector2 lastCursorPos = Vector2.Zero;

	private static Vector2 accumulatedOffset = Vector2.Zero;

	public static Vector2 GetLastCursorPosition()
	{
		return lastCursorPos;
	}

	public static Vector2 GetAccumulatedOffset()
	{
		return accumulatedOffset;
	}

	public static void SetLastCursorPosition(Vector2 value)
	{
		lastCursorPos = value;
	}

	public static void SetAccumulatedOffset(Vector2 value)
	{
		accumulatedOffset = value;
	}

	public static Vector2 Resync(Vector2 displacement, Vector2 offset, float resyncFactor)
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
