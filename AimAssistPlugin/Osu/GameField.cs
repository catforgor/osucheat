using System.Numerics;
using AimAssistPlugin.Tosu;

namespace AimAssistPlugin.Osu;

internal static class GameField
{
	public static Vector2 CalculateScreenOffset()
	{
		return new Vector2(-ScreenInfo.VirtualX, -ScreenInfo.VirtualY);
	}

	public static float GetRatio()
	{
		return GetLayout().Ratio;
	}

	public static Vector2 FieldToDisplay(Vector2 field)
	{
		var (ratio, offset) = GetLayout();
		return field * ratio + offset;
	}

	private static (float Ratio, Vector2 Offset) GetLayout()
	{
		float width = ScreenInfo.Width;
		float height = ScreenInfo.Height;
		return TosuState.IsLazer ? PlayfieldLayout.ComputeLazer(width, height) : PlayfieldLayout.ComputeStable(width, height);
	}
}
