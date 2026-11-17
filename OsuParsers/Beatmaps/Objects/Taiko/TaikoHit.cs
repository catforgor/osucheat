using System.Numerics;
using OsuParsers.Enums.Beatmaps;

namespace OsuParsers.Beatmaps.Objects.Taiko;

public class TaikoHit : HitCircle
{
	public bool IsBig
	{
		get
		{
			return HitSound.HasFlag(HitSoundType.Finish);
		}
		set
		{
			if (value && !HitSound.HasFlag(HitSoundType.Finish))
			{
				HitSound += 4;
			}
			else if (HitSound.HasFlag(HitSoundType.Finish))
			{
				HitSound -= 4;
			}
		}
	}

	public TaikoColor Color
	{
		get
		{
			if (HitSound.HasFlag(HitSoundType.Whistle) || HitSound.HasFlag(HitSoundType.Clap))
			{
				return TaikoColor.Blue;
			}
			return TaikoColor.Red;
		}
		set
		{
			switch (value)
			{
			case TaikoColor.Red:
				if (HitSound.HasFlag(HitSoundType.Whistle))
				{
					HitSound -= 2;
				}
				if (HitSound.HasFlag(HitSoundType.Clap))
				{
					HitSound -= 8;
				}
				if (!HitSound.HasFlag(HitSoundType.Normal))
				{
					HitSound++;
				}
				break;
			case TaikoColor.Blue:
				if (HitSound.HasFlag(HitSoundType.Normal))
				{
					HitSound--;
				}
				if (!HitSound.HasFlag(HitSoundType.Whistle))
				{
					HitSound += 2;
				}
				break;
			}
		}
	}

	public TaikoHit(Vector2 position, int startTime, int endTime, HitSoundType hitSound, Extras extras, bool isNewCombo, int comboOffset)
		: base(position, startTime, endTime, hitSound, extras, isNewCombo, comboOffset)
	{
	}
}
