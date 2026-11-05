using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VoronoiNoiseLite;

/// <summary>
/// GPU 定数バッファと 1:1 対応するブリッタブル構造体。
/// EffectProcessor と ShapeSource が共有し、unsafe ビット比較で差分検出します。
/// レイアウトは HLSL cbuffer Constants (register b0) と完全一致。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal readonly struct ConstantBufferData
{
    // c0 (float4)
    public readonly float TotalX;        // posScene.x オフセット
    public readonly float TotalY;        // posScene.y オフセット
    public readonly float TotalZ;        // Z 軸位置
    public readonly float Threshold;     // しきい値 / 100

    // c1 (float4)
    public readonly float ScaleX;        // 100 / (ScaleX * Size / 100)
    public readonly float ScaleY;
    public readonly float ScaleZ;
    public readonly float Strength;      // 強さ / 100

    // c2 (int, int, int, float) — HLSL では int3 + float
    public readonly int   Octaves;       // フラクタル層数
    public readonly int   Seed;          // シード % 1234
    public readonly int   Levels;        // 階調数
    public readonly float Angle;         // -(angle_rad)

    // c3 (float, float, int, int)
    public readonly float WarpStrength;  // 歪み強さ px
    public readonly float WarpScale;     // 歪みスケール / 100
    public readonly int   FractalMode;   // 0=通常 1=加算 2=乗算 3=スクリーン
    public readonly int   DistanceMode;  // 0=Euclidean 1=Manhattan 2=Chebyshev

    // c4 (float, float, int, int)
    public readonly float Lacunarity;    // 周波数倍率
    public readonly float Gain;          // 振幅減衰
    public readonly int   IsColor;       // 0=モノクロ 1=カラー
    public readonly int   IsAlpha;       // 0=透過 1=不透明

    public ConstantBufferData(
        float TotalX, float TotalY, float TotalZ, float Threshold,
        float ScaleX, float ScaleY, float ScaleZ, float Strength,
        int Octaves, int Seed, int Levels, float Angle,
        float WarpStrength, float WarpScale, int FractalMode, int DistanceMode,
        float Lacunarity, float Gain, int IsColor, int IsAlpha)
    {
        this.TotalX       = TotalX;
        this.TotalY       = TotalY;
        this.TotalZ       = TotalZ;
        this.Threshold    = Threshold;
        this.ScaleX       = ScaleX;
        this.ScaleY       = ScaleY;
        this.ScaleZ       = ScaleZ;
        this.Strength     = Strength;
        this.Octaves      = Octaves;
        this.Seed         = Seed;
        this.Levels       = Levels;
        this.Angle        = Angle;
        this.WarpStrength = WarpStrength;
        this.WarpScale    = WarpScale;
        this.FractalMode  = FractalMode;
        this.DistanceMode = DistanceMode;
        this.Lacunarity   = Lacunarity;
        this.Gain         = Gain;
        this.IsColor      = IsColor;
        this.IsAlpha      = IsAlpha;
    }

    /// <summary>
    /// 全バイトを unsafe 比較（MemoryMarshal）して O(1) で等価判定。
    /// float の NaN 正規化問題を避けるため SequenceEqual でなく直接スパン比較を使います。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(in ConstantBufferData a, in ConstantBufferData b)
    {
        return MemoryMarshal.AsBytes(new ReadOnlySpan<ConstantBufferData>(in a))
            .SequenceEqual(MemoryMarshal.AsBytes(new ReadOnlySpan<ConstantBufferData>(in b)));
    }
}
