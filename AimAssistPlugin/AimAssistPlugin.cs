using System;
using System.Threading;
using AimAssistPlugin.Aim;
using AimAssistPlugin.Game;
using AimAssistPlugin.Tablet;
using AimAssistPlugin.Tosu;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;

namespace AimAssistPlugin;

[PluginName("Reconstructor")]
public class AimAssistPlugin : IPositionedPipelineElement<IDeviceReport>, IPipelineElement<IDeviceReport>
{
	private const long ErrorLogIntervalMs = 1000;

	private static readonly Lock _syncLock = new();

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

	public void Consume(IDeviceReport report)
	{
		if (report is not ITabletReport tabletReport)
		{
			Emit?.Invoke(report);
			return;
		}

		TosuStartup.EnsureStarted();

		lock (_syncLock)
		{
			try
			{
				if (PlayState.IsPlaying)
				{
					TabletReportProcessor.ProcessPlaying(tabletReport);
				}
				else
				{
					TabletReportProcessor.ProcessIdle(tabletReport);
				}
			}
			catch (Exception ex) when (ex is not OutOfMemoryException)
			{
				LogThrottled(ex);
			}
		}

		Emit?.Invoke(tabletReport);
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
