using System;
using System.Numerics;
using System.Runtime.InteropServices;
using AimAssistPlugin.Services;

namespace AimAssistPlugin.Sdk.Osu;

internal static partial class GameField
{
	private const float PlayfieldWidth = 512f;

	private const float PlayfieldHeight = 384f;

	private const float PlayfieldScreenFraction = 0.8f;

	private static partial class ScreenInfo
	{
		private const int SmCxScreen = 0;
		private const int SmCyScreen = 1;
		private const int SmXVirtualScreen = 76;
		private const int SmYVirtualScreen = 77;

		public static int Width => GetSystemMetrics(SmCxScreen);

		public static int Height => GetSystemMetrics(SmCyScreen);

		public static int VirtualX => GetSystemMetrics(SmXVirtualScreen);

		public static int VirtualY => GetSystemMetrics(SmYVirtualScreen);

		[LibraryImport("user32.dll")]
		private static partial int GetSystemMetrics(int nIndex);
	}

	public static Vector2 CalculateScreenOffset()
	{
		return new Vector2(-ScreenInfo.VirtualX, -ScreenInfo.VirtualY);
	}

	public static (float Ratio, Vector2 Offset) ComputeStableLayout(float width, float height)
	{
		float fieldHeight = height * PlayfieldScreenFraction;
		float fieldWidth = 4f / 3f * fieldHeight;
		return (fieldHeight / PlayfieldHeight, new Vector2((width - fieldWidth) / 2f, (height - fieldHeight) / 2f + 0.02f * height));
	}

	public static (float Ratio, Vector2 Offset) ComputeLazerLayout(float width, float height)
	{
		float fieldWidth = Math.Min(width, height * 4f / 3f) * PlayfieldScreenFraction;
		float ratio = fieldWidth / PlayfieldWidth;
		float x = (width - PlayfieldWidth * ratio) / 2f;
		float y = (height - PlayfieldHeight * ratio) / 2f + 8f * ratio;
		return (ratio, new Vector2(x, y));
	}

	private static (float Ratio, Vector2 Offset) GetLayout()
	{
		float width = ScreenInfo.Width;
		float height = ScreenInfo.Height;
		return TosuService.IsLazer ? ComputeLazerLayout(width, height) : ComputeStableLayout(width, height);
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
}
