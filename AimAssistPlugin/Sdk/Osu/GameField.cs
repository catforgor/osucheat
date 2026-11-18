using System;
using System.Numerics;
using System.Runtime.InteropServices;
using AimAssistPlugin.Services;

namespace AimAssistPlugin.Sdk.Osu;

internal class GameField
{
	private static class ScreenInfo
	{
		public static int ScreenWidth => GetSystemMetrics(0);

		public static int ScreenHeight => GetSystemMetrics(1);

		public static int VirtualX => GetSystemMetrics(76);

		public static int VirtualY => GetSystemMetrics(77);

		[DllImport("user32.dll")]
		private static extern int GetSystemMetrics(int nIndex);
	}

	public static Vector2 CalculateScreenOffset()
	{
		float x = -ScreenInfo.VirtualX;
		float y = -ScreenInfo.VirtualY;
		return new Vector2(x, y);
	}

	private static float GetWidth()
	{
		return ScreenInfo.ScreenWidth;
	}

	private static float GetHeight()
	{
		return ScreenInfo.ScreenHeight;
	}
	public static (float Ratio, Vector2 Offset) ComputeStableLayout(float width, float height)
	{
		float num = height * 0.8f;
		float num2 = 1.3333334f * num;
		return (num / 384f, new Vector2((width - num2) / 2f, (height - num) / 2f + 0.02f * height));
	}

	public static (float Ratio, Vector2 Offset) ComputeLazerLayout(float width, float height)
	{
		float fieldWidth = Math.Min(width, height * 4f / 3f) * 0.8f;
		float ratio = fieldWidth / 512f;
		float x = (width - 512f * ratio) / 2f;
		float y = (height - 384f * ratio) / 2f + 8f * ratio;
		return (ratio, new Vector2(x, y));
	}

	private static (float Ratio, Vector2 Offset) GetLayout()
	{
		return TosuService.IsLazer ? ComputeLazerLayout(GetWidth(), GetHeight()) : ComputeStableLayout(GetWidth(), GetHeight());
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
