using System.Numerics;
using AimAssistPlugin.Aim;
using AimAssistPlugin.Beatmaps;
using AimAssistPlugin.Input;
using OpenTabletDriver.Plugin.Tablet;

namespace AimAssistPlugin.Tablet;

internal static class TabletReportProcessor
{
	public static void ProcessPlaying(ITabletReport report)
	{
		AimAssistService.EnsureInitialized();
		Vector2 offset = AimAssistService.GetOffset(report.Position);
		report.Position = PositionRounding.Round(report.Position + offset);
	}

	public static void ProcessIdle(ITabletReport report)
	{
		HitObjectStore.Clear();
		Vector2 lastCursorPosition = InputManager.GetLastCursorPosition();
		Vector2 accumulatedOffset = InputManager.GetAccumulatedOffset();
		AimAssistService.Reset();
		Vector2 position = report.Position;
		if (lastCursorPosition != Vector2.Zero)
		{
			InputManager.SetAccumulatedOffset(OffsetResync.Apply(lastCursorPosition - position, accumulatedOffset, AimTuning.ResyncWhenIdle));
		}
		report.Position = position + PositionRounding.Round(InputManager.GetAccumulatedOffset());
	}
}
