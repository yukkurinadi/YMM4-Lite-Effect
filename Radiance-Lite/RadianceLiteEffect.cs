using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace RadianceLite;

[VideoEffect("輻射光 (軽量版)", [VideoEffectCategories.Decoration], ["大域照明", "ライティング", "発光", "Radiance", "RadianceLite", "軽量化", "爆速", "グロー", "GI", "Global Illumination"], IsAviUtlSupported = false)]
public sealed class RadianceLiteEffect : VideoEffectBase
{
    public override string Label => "輻射光 (軽量版)";

    [Display(GroupName = "基本設定", Name = "強度", Description = "周囲へ届く光の明るさ", Order = 0)]
    [AnimationSlider("F1", "%", 0, 200)]
    public Animation Strength { get; } = new Animation(100, 0, 800);

    [Display(GroupName = "基本設定", Name = "到達距離", Description = "光が届く最大距離", Order = 1)]
    [AnimationSlider("F1", "px", 10, 800)]
    public Animation Range { get; } = new Animation(300, 10, 2000);

    [Display(GroupName = "基本設定", Name = "拡散", Description = "面を照らす光と空気中で光る光の配合。100%で面だけを照らします", Order = 2)]
    [AnimationSlider("F1", "%", 0, 100)]
    public Animation Diffuse { get; } = new Animation(60, 0, 100);

    [Display(GroupName = "基本設定", Name = "環境光", Description = "光が届かない場所の明るさ。下げると光の当たる場所だけが浮かび上がります", Order = 3)]
    [AnimationSlider("F1", "%", 0, 100)]
    public Animation Ambient { get; } = new Animation(100, 0, 100);

    [Display(GroupName = "光源設定", Name = "発光しきい値", Description = "この明るさを超えた部分が光源になります", Order = 10)]
    [AnimationSlider("F1", "%", 0, 100)]
    public Animation Threshold { get; } = new Animation(70, 0, 100);

    [Display(GroupName = "光源設定", Name = "発光の強さ", Description = "光源が放つ光の量", Order = 11)]
    [AnimationSlider("F1", "%", 0, 400)]
    public Animation EmissionGain { get; } = new Animation(150, 0, 1600);

    [Display(GroupName = "光源設定", Name = "発光色", Description = "光に乗せる色。素材の色と掛け合わされます", Order = 12)]
    [ColorPicker]
    public Color LightColor
    {
        get => _lightColor;
        set => Set(ref _lightColor, value);
    }
    private Color _lightColor = Colors.White;

    [Display(GroupName = "光源設定", Name = "遮蔽の強さ", Description = "不透明な部分が光を遮る度合い。0で光がすべてを透過します", Order = 13)]
    [AnimationSlider("F1", "%", 0, 100)]
    public Animation Occlusion { get; } = new Animation(80, 0, 100);

    private IAnimatable[]? _animatables;

    public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription) => [];

    public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
        => new RadianceLiteEffectProcessor(devices, this);

    protected override IEnumerable<IAnimatable> GetAnimatables()
        => _animatables ??= [Strength, Range, Diffuse, Ambient, Threshold, EmissionGain, Occlusion];
}
