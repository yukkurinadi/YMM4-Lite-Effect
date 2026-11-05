using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace VoronoiNoiseLite;

/// <summary>
/// ボロノイノイズ エフェクト（超軽量版・セル値固定）。
/// </summary>
[VideoEffect(
    "ボロノイノイズ (軽量版)",
    [VideoEffectCategories.Decoration],
    ["VoronoiNoise", "ボロノイ", "Voronoi", "ノイズ", "noise", "軽量", "Lite", "セル", "結晶", "モザイク"],
    IsAviUtlSupported = false)]
public sealed class VoronoiNoiseLiteEffect : VideoEffectBase
{
    public override string Label => "ボロノイノイズ (軽量版)";

    // ── ノイズ ───────────────────────────────────────────────────────────

    [Display(GroupName = "ノイズ", Name = "距離方式", Description = "母点までの距離の計算方法", Order = 0)]
    [EnumComboBox]
    public DistanceModeType DistanceMode
    {
        get => _distanceMode;
        set => Set(ref _distanceMode, value);
    }
    private DistanceModeType _distanceMode = DistanceModeType.Euclidean;

    [Display(GroupName = "ノイズ", Name = "色", Description = "有効にするとセルごとにランダムなRGBカラーを出力します", Order = 1)]
    [ToggleSlider]
    public bool IsColor
    {
        get => _isColor;
        set => Set(ref _isColor, value);
    }
    private bool _isColor = false;

    [Display(GroupName = "ノイズ", Name = "不透明度", Description = "有効にするとノイズが不透明になります", Order = 2)]
    [ToggleSlider]
    public bool UseAlpha
    {
        get => _useAlpha;
        set => Set(ref _useAlpha, value);
    }
    private bool _useAlpha = true;

    [Display(GroupName = "ノイズ", Name = "個別シード値", Description = "シード値を手動で指定します", Order = 3)]
    [ToggleSlider]
    public bool IsUniqueSeed
    {
        get => _isUniqueSeed;
        set => Set(ref _isUniqueSeed, value);
    }
    private bool _isUniqueSeed = false;

    [Display(GroupName = "ノイズ", Name = "シード値", Description = "乱数シード", Order = 4)]
    [AnimationSlider("F0", "", 0d, 100d)]
    public Animation Seed { get; } = new Animation(0, 0, 9999);

    // ── ノイズ / 濃淡 ─────────────────────────────────────────────────────

    [Display(GroupName = "ノイズ / 濃淡", Name = "強さ", Description = "ノイズのコントラスト強度", Order = 10)]
    [AnimationSlider("F1", "%", 0d, 200d)]
    public Animation Strength { get; } = new Animation(100, 0, 800);

    [Display(GroupName = "ノイズ / 濃淡", Name = "しきい値", Description = "0より大きくすると二値化されます", Order = 11)]
    [AnimationSlider("F1", "%", 0d, 100d)]
    public Animation Threshold { get; } = new Animation(0, 0, 100);

    [Display(GroupName = "ノイズ / 濃淡", Name = "階調数", Description = "階調の分割数（256で滑らか）", Order = 12)]
    [AnimationSlider("F0", "", 2d, 256d)]
    public Animation Levels { get; } = new Animation(256, 2, 256);

    // ── ノイズ / フラクタル ───────────────────────────────────────────────

    [Display(GroupName = "ノイズ / フラクタル", Name = "繰り返し", Description = "オクターブ多重合成の層数", Order = 20)]
    [AnimationSlider("F0", "", 1d, 8d)]
    public Animation Octaves { get; } = new Animation(5, 1, 8);

    [Display(GroupName = "ノイズ / フラクタル", Name = "合成方式", Description = "オクターブ同士の重ね合わせ方法", Order = 21)]
    [EnumComboBox]
    public FractalBlendModeType FractalBlend
    {
        get => _fractalBlend;
        set => Set(ref _fractalBlend, value);
    }
    private FractalBlendModeType _fractalBlend = FractalBlendModeType.Normal;

    [Display(GroupName = "ノイズ / フラクタル", Name = "周波数倍率", Description = "各オクターブの周波数倍率", Order = 22)]
    [AnimationSlider("F2", "", 1d, 4d)]
    public Animation Lacunarity { get; } = new Animation(2, 1, 5);

    [Display(GroupName = "ノイズ / フラクタル", Name = "振幅減衰", Description = "各オクターブの振幅減衰率", Order = 23)]
    [AnimationSlider("F2", "", 0d, 1d)]
    public Animation Gain { get; } = new Animation(0.5, 0, 1);

    // ── ノイズ / 歪み ─────────────────────────────────────────────────────

    [Display(GroupName = "ノイズ / 歪み", Name = "歪み強さ", Description = "ノイズ空間の歪み量", Order = 30)]
    [AnimationSlider("F1", "px", 0d, 200d)]
    public Animation WarpStrength { get; } = new Animation(0, 0, 1000);

    [Display(GroupName = "ノイズ / 歪み", Name = "歪みスケール", Description = "歪みパターンの大きさ", Order = 31)]
    [AnimationSlider("F1", "%", 1d, 500d)]
    public Animation WarpScale { get; } = new Animation(100, 1, 1000);

    // ── ノイズ / 変形 ─────────────────────────────────────────────────────

    [Display(GroupName = "ノイズ / 変形", Name = "サイズ", Description = "全体の基本サイズ", Order = 40)]
    [AnimationSlider("F1", "%", 1d, 1000d)]
    public Animation Size { get; } = new Animation(100, 1, 2000);

    [Display(GroupName = "ノイズ / 変形", Name = "サイズX", Description = "横方向のスケール倍率", Order = 41)]
    [AnimationSlider("F1", "%", 1d, 1000d)]
    public Animation ScaleX { get; } = new Animation(100, 1, 2000);

    [Display(GroupName = "ノイズ / 変形", Name = "サイズY", Description = "縦方向のスケール倍率", Order = 42)]
    [AnimationSlider("F1", "%", 1d, 1000d)]
    public Animation ScaleY { get; } = new Animation(100, 1, 2000);

    [Display(GroupName = "ノイズ / 変形", Name = "サイズZ", Description = "時間方向のスケール倍率", Order = 43)]
    [AnimationSlider("F1", "%", 1d, 1000d)]
    public Animation ScaleZ { get; } = new Animation(100, 1, 2000);

    [Display(GroupName = "ノイズ / 変形", Name = "回転角", Description = "パターンの回転角度", Order = 44)]
    [AnimationSlider("F1", "°", -360d, 360d)]
    public Animation Angle { get; } = new Animation(0, -3600, 3600);

    // ── ノイズ / 移動 ─────────────────────────────────────────────────────

    [Display(GroupName = "ノイズ / 移動", Name = "X", Description = "X方向の移動量", Order = 50)]
    [AnimationSlider("F1", "px", -1000d, 1000d)]
    public Animation X { get; } = new Animation(0, -9999, 9999);

    [Display(GroupName = "ノイズ / 移動", Name = "Y", Description = "Y方向の移動量", Order = 51)]
    [AnimationSlider("F1", "px", -1000d, 1000d)]
    public Animation Y { get; } = new Animation(0, -9999, 9999);

    [Display(GroupName = "ノイズ / 移動", Name = "Z", Description = "Z方向の移動量", Order = 52)]
    [AnimationSlider("F1", "px", -1000d, 1000d)]
    public Animation Z { get; } = new Animation(0, -9999, 9999);

    [Display(GroupName = "ノイズ / 移動", Name = "移動速度X", Description = "フレームごとのX移動速度", Order = 53)]
    [AnimationSlider("F2", "px/f", -100d, 100d)]
    public Animation SpeedX { get; } = new Animation(0, -1000, 1000);

    [Display(GroupName = "ノイズ / 移動", Name = "移動速度Y", Description = "フレームごとのY移動速度", Order = 54)]
    [AnimationSlider("F2", "px/f", -100d, 100d)]
    public Animation SpeedY { get; } = new Animation(0, -1000, 1000);

    [Display(GroupName = "ノイズ / 移動", Name = "変化速度", Description = "フレームごとのZ変化速度", Order = 55)]
    [AnimationSlider("F2", "/f", -100d, 100d)]
    public Animation SpeedZ { get; } = new Animation(0, -1000, 1000);

    // ── IVideoEffect 実装 ─────────────────────────────────────────────────

    public override IEnumerable<string> CreateExoVideoFilters(
        int keyFrameIndex, ExoOutputDescription exoOutputDescription) => [];

    public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
        => new VoronoiNoiseLiteEffectProcessor(devices, this);

    protected override IEnumerable<IAnimatable> GetAnimatables()
        => [Seed, Strength, Threshold, Levels, Octaves, Lacunarity, Gain,
            WarpStrength, WarpScale, Size, ScaleX, ScaleY, ScaleZ, Angle,
            X, Y, Z, SpeedX, SpeedY, SpeedZ];
}
