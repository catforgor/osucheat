using System;

namespace AimAssistPlugin.Tosu;

internal static class TosuSettings
{
	public const string StateUrl = "http://localhost:24050/json/v2";
	public const string PreciseUrl = "http://localhost:24050/json/v2/precise";

	// tosu = 15ms
	public static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(8);

	public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

	public static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(2);

	public const int FailureLimit = 5;
}
