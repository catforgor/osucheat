using System;

namespace AimAssistPlugin.Tosu;

internal static class TosuSettings
{
	public const string LogGroup = "Reconstructor";

	public const string StateUrl = "http://localhost:24050/json/v2";
	public const string PreciseUrl = "http://localhost:24050/json/v2/precise";
	public const string ReleaseUrl = "https://api.github.com/repos/tosuapp/tosu/releases/latest";

	public const string TrustedDownloadPrefix = "https://github.com/tosuapp/tosu/";

	public const string TosuProcessName = "tosu";

	public static readonly string[] GameProcessNames = ["osu!", "osulazer"];

	// tosu = 15ms
	public static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(8);

	public static readonly TimeSpan MonitorInterval = TimeSpan.FromSeconds(2);

	public static readonly TimeSpan StartRetryBackoff = TimeSpan.FromSeconds(60);

	public static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(2);

	public static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(5);

	public const int FailureLimit = 5;

	public const string DefaultConfig =
		"DEBUG_LOG=false\r\nENABLE_AUTOUPDATE=false\r\nOPEN_DASHBOARD_ON_STARTUP=false\r\n\r\n" +
		"SHOW_MP_COMMANDS=false\r\nCALCULATE_PP=false\r\n\r\n" +
		"ENABLE_KEY_OVERLAY=false\r\nENABLE_INGAME_OVERLAY=false\r\n\r\n" +
		"POLL_RATE=15\r\nPRECISE_DATA_POLL_RATE=15\r\n\r\n" +
		"INGAME_OVERLAY_KEYBIND=Control + Shift + Space\r\nINGAME_OVERLAY_MAX_FPS=60\r\n\r\n" +
		"SERVER_IP=127.0.0.1\r\nSERVER_PORT=24050\r\nALLOWED_IPS=127.0.0.1,localhost,absolute\r\n\r\n" +
		"STATIC_FOLDER_PATH=./static";
}
