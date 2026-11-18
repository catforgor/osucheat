using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AimAssistPlugin.Beatmaps;

internal static partial class ModParser
{
	public static HashSet<string> Parse(string? mods)
	{
		if (string.IsNullOrWhiteSpace(mods))
		{
			return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		return ModToken().Matches(mods.ToUpperInvariant())
			.Select(m => m.Value)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
	}

	[GeneratedRegex("[A-Z]{2}")]
	private static partial Regex ModToken();
}
