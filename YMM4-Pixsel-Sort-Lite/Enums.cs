using System.ComponentModel.DataAnnotations;

namespace LiteEffect;

/// <summary>
/// ソートの方向モード
/// </summary>
public enum PixelSortMode
{
    [Display(Name = "垂直 (上 → 下)")]
    TopToBottom,

    [Display(Name = "垂直 (下 → 上)")]
    BottomToTop,

    [Display(Name = "水平 (左 → 右)")]
    LeftToRight,

    [Display(Name = "水平 (右 → 左)")]
    RightToLeft,

    [Display(Name = "カスタム角度")]
    Angle
}

/// <summary>
/// ピクセルのソート基準
/// </summary>
public enum PixelSortCriterion
{
    [Display(Name = "輝度 / 明度 (Luminance)")]
    Luminance,

    [Display(Name = "色相 (Hue)")]
    Hue,

    [Display(Name = "彩度 (Saturation)")]
    Saturation,

    [Display(Name = "赤 (Red)")]
    Red,

    [Display(Name = "緑 (Green)")]
    Green,

    [Display(Name = "青 (Blue)")]
    Blue
}
