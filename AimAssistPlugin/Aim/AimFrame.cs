using System.Numerics;

namespace AimAssistPlugin.Aim;

internal readonly struct AimFrame(float scaledRadius, float fovRadius, Vector2 targetPosition)
{
	public readonly float ScaledRadiusSquared = scaledRadius * scaledRadius;

	public readonly float FovSquared = fovRadius * fovRadius;

	public readonly Vector2 TargetPosition = targetPosition;
}
