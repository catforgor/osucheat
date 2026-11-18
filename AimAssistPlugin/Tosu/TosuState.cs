using System;
using System.Threading;

namespace AimAssistPlugin.Tosu;

internal static class TosuState
{
	private const int NoTime = int.MinValue;

	private static TosuSnapshot? _snapshot;

	private static int _preciseTime = NoTime;

	public static volatile bool IsLazer;

	public static volatile bool OsuRunning;

	public static volatile bool TosuRunning;

	public static TosuSnapshot? Snapshot => Volatile.Read(ref _snapshot);

	public static int? PreciseTime
	{
		get
		{
			int time = Volatile.Read(ref _preciseTime);
			return time == NoTime ? null : time;
		}
	}

	public static void Publish(TosuSnapshot snapshot, int? preciseTime)
	{
		if (snapshot.Client != null)
		{
			IsLazer = string.Equals(snapshot.Client, "lazer", StringComparison.OrdinalIgnoreCase);
		}
		Volatile.Write(ref _snapshot, snapshot);
		Volatile.Write(ref _preciseTime, preciseTime ?? NoTime);
	}

	public static void Clear()
	{
		Volatile.Write(ref _snapshot, null);
		Volatile.Write(ref _preciseTime, NoTime);
	}
}
