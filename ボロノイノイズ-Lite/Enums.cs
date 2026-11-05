using System.ComponentModel.DataAnnotations;

namespace VoronoiNoiseLite;

public enum DistanceModeType
{
    [Display(Name = "ユークリッド")]
    Euclidean = 0,
    [Display(Name = "マンハッタン")]
    Manhattan = 1,
    [Display(Name = "チェビシェフ")]
    Chebyshev = 2,
}

public enum FractalBlendModeType
{
    [Display(Name = "通常")]
    Normal = 0,
    [Display(Name = "加算")]
    Add = 1,
    [Display(Name = "乗算")]
    Multiply = 2,
    [Display(Name = "スクリーン")]
    Screen = 3,
}
