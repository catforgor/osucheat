using System.Runtime.InteropServices;

namespace AimAssistPlugin.Osu;

internal static partial class ScreenInfo
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
