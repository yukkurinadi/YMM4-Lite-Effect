using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.ItemEditor;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Player.Video.Effects;
using YukkuriMovieMaker.Plugin.Effects;
using YukkuriMovieMaker.Project.Effects.NoiseParameters;
using YukkuriMovieMaker.Resources.Localization;

namespace YukkuriMovieMaker.Project.Effects;

[VideoEffect("NoiseEffectName", new string[] { "YMM4Key_EffectCategoryFilteringName" }, new string[] { "noise", "砂嵐" }, true, true, ResourceType = typeof(Texts))]
internal class NoiseEffect : VideoEffectBase
{
	private NoiseType noiseType;

	private NoiseParameterBase noiseParameter = new FractalNoiseParameter(null);

	private bool isColor;

	private bool isAlpha = true;

	private bool isUniqueSeed;

	public override string Label => Texts.NoiseEffectName;

	[Display(GroupName = "NoiseGroupName", Name = "NoiseEffectNoiseTypeName", Description = "NoiseEffectNoiseTypeDesc", Order = 100, ResourceType = typeof(Texts))]
	[EnumComboBox(EnumComboBoxItemOrderRule.DisplayName)]
	public NoiseType NoiseType
	{
		get
		{
			return noiseType;
		}
		set
		{
			Set(ref noiseType, value, "NoiseType");
		}
	}

	[Display(GroupName = "NoiseGroupName", Order = 100, AutoGenerateField = true, ResourceType = typeof(Texts))]
	public NoiseParameterBase NoiseParameter
	{
		get
		{
			return noiseParameter;
		}
		set
		{
			Set(ref noiseParameter, value, "NoiseParameter");
		}
	}

	[Display(GroupName = "NoiseGroupName", Name = "BasicNoiseParameterBaseIsColorName", Description = "BasicNoiseParameterBaseIsColorDesc", Order = 1000, ResourceType = typeof(Texts))]
	[ToggleSlider]
	[YMM4Only]
	public bool IsColor
	{
		get
		{
			return isColor;
		}
		set
		{
			Set(ref isColor, value, "IsColor");
		}
	}

	[Display(GroupName = "NoiseGroupName", Name = "BasicNoiseParameterBaseIsAlphaName", Description = "BasicNoiseParameterBaseIsAlphaDesc", Order = 1000, ResourceType = typeof(Texts))]
	[ToggleSlider]
	[YMM4Only]
	public bool IsAlpha
	{
		get
		{
			return isAlpha;
		}
		set
		{
			Set(ref isAlpha, value, "IsAlpha");
		}
	}

	[Display(GroupName = "NoiseGroupName", Name = "BasicNoiseParameterBaseIsUniqueSeedName", Description = "BasicNoiseParameterBaseIsUniqueSeedDesc", Order = 1000, ResourceType = typeof(Texts))]
	[ToggleSlider]
	public bool IsUniqueSeed
	{
		get
		{
			return isUniqueSeed;
		}
		set
		{
			Set(ref isUniqueSeed, value, "IsUniqueSeed");
		}
	}

	protected override IEnumerable<IAnimatable> GetAnimatables()
	{
		return new _003C_003Ez__ReadOnlySingleElementList<IAnimatable>(NoiseParameter);
	}

	public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription)
	{
		return noiseParameter.CreateExoVideoFilters(keyFrameIndex, exoOutputDescription, IsEnabled, IsUniqueSeed ? (NoiseParameter?.GetHashCode() ?? 0) : 0);
	}

	public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
	{
		return new YukkuriMovieMaker.Player.Video.Effects.NoiseEffect(devices, this);
	}

	public override async ValueTask EndEditAsync()
	{
		NoiseParameter = NoiseParameterFactory.Default.CreateParameterIfNeed(NoiseType, NoiseParameter);
		await base.EndEditAsync();
	}
}
