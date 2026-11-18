using System;
using OpenTabletDriver.Plugin;

namespace AimAssistPlugin.Tosu;

internal static class TosuLog
{
	private static string? _lastError;

	public static void Warning(string message)
	{
		Log.Write(TosuSettings.LogGroup, message, LogLevel.Warning);
	}

	public static void Error(string message)
	{
		Log.Write(TosuSettings.LogGroup, message, LogLevel.Error);
	}

	public static void ErrorOnce(string source, Exception ex)
	{
		string message = source + ": " + ex.Message;
		if (_lastError == message)
		{
			return;
		}
		_lastError = message;
		Error(message);
	}

	public static void ClearLastError()
	{
		_lastError = null;
	}
}
