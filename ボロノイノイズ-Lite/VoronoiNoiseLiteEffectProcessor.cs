using System;
using System.Runtime.InteropServices;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Player.Video.Effects;

namespace VoronoiNoiseLite;

/// <summary>
/// ボロノイノイズ エフェクト プロセッサー（YMM4完全互換・超軽量版）。
/// パラメーターが変化したフレームのみ GPU 定数バッファを更新します。
/// </summary>
internal sealed class VoronoiNoiseLiteEffectProcessor(
    IGraphicsDevicesAndContext devices,
    VoronoiNoiseLiteEffect item) : VideoEffectProcessorBase(devices)
{
    private VoronoiNoiseLiteCustomEffect? _effect;

    // 前フレームの計算済みパラメーターをキャッシュ
    private ConstantBufferData _prevCb;
    private bool _isFirst = true;

    public override DrawDescription Update(EffectDescription effectDescription)
    {
        if (IsPassThroughEffect || _effect is null)
            return effectDescription.DrawDescription;

        var frame  = effectDescription.ItemPosition.Frame;
        var length = effectDescription.ItemDuration.Frame;
        var fps    = effectDescription.FPS;

        // ── YMM4本家 BasicNoiseParameterBase.SetParameterTo と完全一致の計算式 ──
        var cb = BuildConstantBuffer(frame, length, fps);

        // 前回と全フィールド一致していれば GPU 更新をスキップ
        if (_isFirst || !ConstantBufferData.Equals(in cb, in _prevCb))
        {
            _effect.UpdateBuffer(in cb);
            _prevCb  = cb;
            _isFirst = false;
        }

        return effectDescription.DrawDescription;
    }

    private ConstantBufferData BuildConstantBuffer(int frame, int length, int fps)
    {
        double num  = item.Size.GetValue(frame, length, fps) / 100.0;
        double num2 = Math.Max(0.001, item.ScaleX.GetValue(frame, length, fps) * num);
        double num3 = Math.Max(0.001, item.ScaleY.GetValue(frame, length, fps) * num);
        double num4 = Math.Max(0.001, item.ScaleZ.GetValue(frame, length, fps) * num);

        double valX = item.X.GetValue(frame, length, fps);
        double valY = item.Y.GetValue(frame, length, fps);
        double valZ = item.Z.GetValue(frame, length, fps);
        double spdX = item.SpeedX.GetValue(frame, length, fps);
        double spdY = item.SpeedY.GetValue(frame, length, fps);
        double spdZ = item.SpeedZ.GetValue(frame, length, fps);

        double num6 = (valX + spdX * frame) % 123456.789;
        double num7 = (valY + spdY * frame) % 154321.0987;
        double num8 = (valZ + spdZ * frame) % 160213.8549;
        double num9 = item.Angle.GetValue(frame, length, fps) / 180.0 * Math.PI;

        double warpScale = Math.Max(0.01, item.WarpScale.GetValue(frame, length, fps) / 100.0);
        int seedVal = item.IsUniqueSeed ? (int)item.Seed.GetValue(frame, length, fps) : 0;

        return new ConstantBufferData(
            TotalX:       (float)num6,
            TotalY:       (float)num7,
            TotalZ:       (float)num8,
            Threshold:    (float)(item.Threshold.GetValue(frame, length, fps) / 100.0),
            ScaleX:       (float)(100.0 / num2),
            ScaleY:       (float)(100.0 / num3),
            ScaleZ:       (float)(100.0 / num4),
            Strength:     (float)(item.Strength.GetValue(frame, length, fps) / 100.0),
            Octaves:      (int)item.Octaves.GetValue(frame, length, fps),
            Seed:         seedVal % 1234,
            Levels:       (int)item.Levels.GetValue(frame, length, fps),
            Angle:        (float)(0.0 - num9),
            WarpStrength: (float)item.WarpStrength.GetValue(frame, length, fps),
            WarpScale:    (float)warpScale,
            FractalMode:  (int)item.FractalBlend,
            DistanceMode: (int)item.DistanceMode,
            Lacunarity:   (float)item.Lacunarity.GetValue(frame, length, fps),
            Gain:         (float)item.Gain.GetValue(frame, length, fps),
            IsColor:      item.IsColor  ? 1 : 0,
            IsAlpha:      item.UseAlpha ? 1 : 0);
    }

    protected override ID2D1Image? CreateEffect(IGraphicsDevicesAndContext devices)
    {
        _effect = new VoronoiNoiseLiteCustomEffect(devices);

        if (!_effect.IsEnabled)
        {
            _effect.Dispose();
            _effect = null;
            return null;
        }

        disposer.Collect(_effect);

        var output = _effect.Output;
        disposer.Collect(output);
        return output;
    }

    protected override void setInput(ID2D1Image? input)
        => _effect?.SetInput(0, input, true);

    protected override void ClearEffectChain()
    {
        _effect?.SetInput(0, null, true);
        _isFirst = true;
    }
}
