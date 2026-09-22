using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace DirectionalColorKeyLite;

[VideoEffect("軽量透過クロマキー", ["合成", "クロマキー"], ["DirectionalColorKeyLite", "DirectionalColorKey", "dcsk", "chroma key", "Vlahos", "軽量透過クロマキー", "透過クロマキー", "方向クロマキー", "色分離キー", "爆速クロマキー", "半透明クロマキー"])]
public class DirectionalColorKeyLiteEffect : VideoEffectBase
{
    public override string Label => "軽量透過クロマキー";

    [Display(GroupName = "クロマキー設定", Name = "背景色", Description = "キーイング（透過）する背景色を指定します（デフォルト: 緑）", Order = 100)]
    [ColorPicker]
    public Color BackgroundColor { get => _backgroundColor; set => Set(ref _backgroundColor, value); }
    private Color _backgroundColor = Color.FromRgb(0, 255, 0);

    [Display(GroupName = "クロマキー設定", Name = "感度 (抜き強度)", Description = "背景色の判定感度（大きいほど広く透過）", Order = 110)]
    [AnimationSlider("F1", "%", 0d, 300d)]
    public Animation Sensitivity { get; } = new Animation(100, 0, 300);

    [Display(GroupName = "クロマキー設定", Name = "閾値 (境界バランス)", Description = "背景と前景の境界バランス基準 (a2パラメーター)", Order = 120)]
    [AnimationSlider("F1", "%", 0d, 200d)]
    public Animation Threshold { get; } = new Animation(100, 0, 200);

    [Display(GroupName = "クロマキー設定", Name = "エッジ柔らかさ", Description = "半透明境界のフェード・足切り調整", Order = 130)]
    [AnimationSlider("F1", "%", 0d, 100d)]
    public Animation EdgeSoftness { get; } = new Animation(0, 0, 100);

    [Display(GroupName = "クロマキー設定", Name = "スピル除去", Description = "髪の毛やフチに残る背景色の被り（反射）を除去する強度", Order = 140)]
    [AnimationSlider("F1", "%", 0d, 100d)]
    public Animation SpillStrength { get; } = new Animation(100, 0, 100);

    [Display(GroupName = "クロマキー設定", Name = "前景出力", Description = "OFFにするとアルファマスク（白黒）のみ確認できます", Order = 150)]
    [ToggleSlider]
    public bool OutputForeground { get => _outputForeground; set => Set(ref _outputForeground, value); }
    private bool _outputForeground = true;

    public DirectionalColorKeyLiteEffect()
    {
    }

    public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription) => [];

    public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
        => new DirectionalColorKeyLiteProcessor(devices, this);

    protected override IEnumerable<IAnimatable> GetAnimatables() => [Sensitivity, Threshold, EdgeSoftness, SpillStrength];
}
