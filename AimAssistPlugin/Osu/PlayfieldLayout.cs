using System;
using System.Numerics;

namespace AimAssistPlugin.Osu;

internal static class PlayfieldLayout
{
	private const float PlayfieldWidth = 512f;

	private const float PlayfieldHeight = 384f;

	private const float PlayfieldScreenFraction = 0.8f;

	public static (float Ratio, Vector2 Offset) ComputeStable(float width, float height)
	{
		float fieldHeight = height * PlayfieldScreenFraction;
		float fieldWidth = 4f / 3f * fieldHeight;
		return (fieldHeight / PlayfieldHeight, new Vector2((width - fieldWidth) / 2f, (height - fieldHeight) / 2f + 0.02f * height));
	}

	public static (float Ratio, Vector2 Offset) ComputeLazer(float width, float height)
	{
		float fieldWidth = Math.Min(width, height * 4f / 3f) * PlayfieldScreenFraction;
		float ratio = fieldWidth / PlayfieldWidth;
		float x = (width - PlayfieldWidth * ratio) / 2f;
		float y = (height - PlayfieldHeight * ratio) / 2f + 8f * ratio;
		return (ratio, new Vector2(x, y));
	}
}
