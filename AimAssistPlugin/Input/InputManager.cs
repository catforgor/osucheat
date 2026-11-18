using System.Numerics;

namespace AimAssistPlugin.Input;

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
}
