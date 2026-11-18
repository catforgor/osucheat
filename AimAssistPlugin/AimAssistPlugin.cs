using System;
using System.Numerics;
using AimAssistPlugin.Sdk.Input;
using AimAssistPlugin.Sdk.Player;
using AimAssistPlugin.Services;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;

namespace AimAssistPlugin;

[PluginName("Reconstructor")]
public class AimAssistPlugin : IPositionedPipelineElement<IDeviceReport>, IPipelineElement<IDeviceReport>
{
	private const long ErrorLogIntervalMs = 1000;

	private static readonly object _syncLock = new object();

	private long _lastErrorLogMs = long.MinValue;

	public PipelinePosition Position => (PipelinePosition)2;

	[Property("EMA Weight")]
	[DefaultPropertyValue(0.5f)]
	[ToolTip("Default: 0.5\n\nDefines the weight of the latest sample against previous ones [Range: 0.0 - 1.0]\n  Lower == More hardware smoothing removed\n  1 == No effect")]
	public float Power
	{
		get
		{
			return AimAssistService.Power;
		}
		set
		{
			AimAssistService.Power = float.IsNaN(value) ? 0f : Math.Max(0f, value);
		}
	}

	public event Action<IDeviceReport>? Emit;

	private static float RoundOff(float val)
	{
		return MathF.Round(val, MidpointRounding.AwayFromZero);
	}

	private static Vector2 RoundVec(Vector2 v)
	{
		return new Vector2(RoundOff(v.X), RoundOff(v.Y));
	}

	public void Consume(IDeviceReport report)
	{
		if (report is not ITabletReport tabletReport)
		{
			Emit?.Invoke(report);
			return;
		}

		TosuService.EnsureStarted();

		lock (_syncLock)
		{
			try
			{
				if (Player.IsPlaying)
				{
					ProcessPlayingReport(tabletReport);
				}
				else
				{
					ProcessIdleReport(tabletReport);
				}
			}
			catch (Exception ex) when (ex is not OutOfMemoryException)
			{
				LogThrottled(ex);
			}
		}

		Emit?.Invoke(tabletReport);
	}

	private static void ProcessPlayingReport(ITabletReport report)
	{
		AimAssistService.EnsureInitialized();
		Vector2 offset = AimAssistService.GetOffset(report.Position);
		report.Position = RoundVec(report.Position + offset);
	}

	private static void ProcessIdleReport(ITabletReport report)
	{
		HitObjectManager.hitObjects.Clear();
		Vector2 lastCursorPosition = InputManager.GetLastCursorPosition();
		Vector2 accumulatedOffset = InputManager.GetAccumulatedOffset();
		AimAssistService.Reset();
		Vector2 position = report.Position;
		if (lastCursorPosition != Vector2.Zero)
		{
			InputManager.SetAccumulatedOffset(InputManager.Resync(lastCursorPosition - position, accumulatedOffset, 0.5f));
		}
		report.Position = position + RoundVec(InputManager.GetAccumulatedOffset());
	}

	private void LogThrottled(Exception ex)
	{
		long now = Environment.TickCount64;
		if (now - _lastErrorLogMs < ErrorLogIntervalMs)
		{
			return;
		}
		_lastErrorLogMs = now;
		Log.Write("Aim Assist", "Error: " + ex.Message, LogLevel.Error);
	}
}
