using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.ItemEditor.CustomVisibilityAttributes;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace LiteEffect;

[VideoEffect("軽量ピクセルソート", ["描画", "加工"], ["PixelSort", "ピクセルソート", "軽量ピクセルソート", "グリッチ", "Glitch"])]
public class PixelSortEffect : VideoEffectBase
{
    public override string Label => "軽量ピクセルソート";

    private PixelSortMode mode = PixelSortMode.TopToBottom;
    [Display(Name = "方向", Description = "ソートする方向（上→下/下→上/左→右/右→左/カスタム角度）を指定します。")]
    [EnumComboBox]
    public PixelSortMode Mode
    {
        get => mode;
        set => Set(ref mode, value);
    }

    [ShowPropertyEditorWhen(nameof(Mode), PixelSortMode.Angle)]
    [Display(Name = "角度", Description = "カスタム角度選択時のソート方向（度）を指定します。")]
    [AnimationSlider("F1", "°", -360.0, 360.0)]
    public Animation Angle { get; } = new Animation(0.0, -360.0, 360.0);

    private PixelSortCriterion criterion = PixelSortCriterion.Luminance;
    [Display(Name = "ソート基準", Description = "ピクセルを並び替える基準（輝度/色相/彩度/RGB）を指定します。")]
    [EnumComboBox]
    public PixelSortCriterion Criterion
    {
        get => criterion;
        set => Set(ref criterion, value);
    }

    [Display(Name = "閾値(下限)", Description = "ソート対象とするピクセルの下限値を指定します。")]
    [AnimationSlider("F1", "%", 0.0, 100.0)]
    public Animation ThresholdMin { get; } = new Animation(30.0, 0.0, 100.0);

    [Display(Name = "閾値(上限)", Description = "ソート対象とするピクセルの上限値を指定します。")]
    [AnimationSlider("F1", "%", 0.0, 100.0)]
    public Animation ThresholdMax { get; } = new Animation(90.0, 0.0, 100.0);

    private bool invertThreshold = false;
    [Display(Name = "閾値反転", Description = "ONにすると指定範囲外のピクセルをソート対象にします。")]
    [ToggleSlider]
    public bool InvertThreshold
    {
        get => invertThreshold;
        set => Set(ref invertThreshold, value);
    }

    private bool includeTransparent = true;
    [Display(Name = "枠全体までソート", Description = "ONにすると透明部分も含めて画像の枠全体までソートします（透明は上/先頭へ移動）。")]
    [ToggleSlider]
    public bool IncludeTransparent
    {
        get => includeTransparent;
        set => Set(ref includeTransparent, value);
    }

    [Display(Name = "ランダム区間", Description = "区間をランダムに分割して有機的なグリッチ感を付加します。")]
    [AnimationSlider("F1", "%", 0.0, 100.0)]
    public Animation RandomInterval { get; } = new Animation(0.0, 0.0, 100.0);

    private int seed = 0;
    [Display(Name = "シード値", Description = "ランダム区間のシード値を指定します。")]
    [TextBoxSlider("F0", "", 0, 10000)]
    [Range(0, 100000)]
    [DefaultValue(0)]
    public int Seed
    {
        get => seed;
        set => Set(ref seed, value);
    }

    [Display(Name = "適用率", Description = "元画像とソート後画像のブレンド割合を指定します。")]
    [AnimationSlider("F1", "%", 0.0, 100.0)]
    public Animation Blend { get; } = new Animation(100.0, 0.0, 100.0);

    public PixelSortEffect()
    {
    }

    public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
    {
        return new PixelSortEffectProcessor(devices, this);
    }

    protected override IEnumerable<IAnimatable> GetAnimatables()
    {
        return [Angle, ThresholdMin, ThresholdMax, RandomInterval, Blend];
    }

    public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription)
    {
        return [];
    }
}
