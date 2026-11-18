namespace AimAssistPlugin.Aim;

internal static class AimTuning
{
	public const float FullPower = 1f;
	public const float StrengthAtFullPower = 0.7f;
	public const float StrengthPerExtraPower = 0.15f;

	public const float MaxOffsetPerPower = 16f;

	public const float MinFovInCircleRadii = 1.2f;
	public const float FovPerStrength = 4f;

	public const float InsideCircleFactor = 0.65f;

	public const float SliderCoreFraction = 0.35f;
	public const float SliderBodyPowerDivisor = 2.5f;

	public const float ResyncOutsideFov = 0.6f;
	public const float ResyncAtMapEnd = 0.5f;
	public const float ResyncWhenIdle = 0.5f;

	public const int MaxGapInPreEmpts = 2;

	public const int RewindThresholdMs = 150;
	public const long InitRetryIntervalMs = 500;
}
