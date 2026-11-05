using System;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace VoronoiNoiseLite;

/// <summary>
/// ボロノイノイズ 図形ソース（YMM4完全互換・超軽量版）。
/// パラメーターが変化したフレームのみ GPU 定数バッファを更新します。
/// </summary>
internal sealed class VoronoiNoiseLiteShapeSource : IShapeSource
{
    private readonly IGraphicsDevicesAndContext _devices;
    private readonly VoronoiNoiseLiteShapeParameter _param;

    private VoronoiNoiseLiteCustomEffect? _effect;
    private Flood? _flood;
    private ID2D1Image? _output;

    private ConstantBufferData _prevCb;
    private bool _isFirst = true;

    public ID2D1Image Output => _output ?? throw new InvalidOperationException("Not initialized");

    public VoronoiNoiseLiteShapeSource(
        IGraphicsDevicesAndContext devices,
        VoronoiNoiseLiteShapeParameter param)
    {
        _devices = devices;
        _param = param;
        Initialize();
    }

    private void Initialize()
    {
        _effect = new VoronoiNoiseLiteCustomEffect(_devices);

        if (!_effect.IsEnabled)
        {
            _effect.Dispose();
            _effect = null;
            return;
        }

        _flood = new Flood(_devices.DeviceContext);
        _flood.Color = new Vortice.Mathematics.Color4(1f, 1f, 1f, 1f);

        using var floodOut = _flood.Output;
        _effect.SetInput(0, floodOut, true);

        _output = _effect.Output;
    }

    public void Update(TimelineItemSourceDescription description)
    {
        if (_effect is null) return;

        var frame  = description.ItemPosition.Frame;
        var length = description.ItemDuration.Frame;
        var fps    = description.FPS;

        var cb = BuildConstantBuffer(frame, length, fps);

        if (_isFirst || !ConstantBufferData.Equals(in cb, in _prevCb))
        {
            _effect.UpdateBuffer(in cb);
            _prevCb  = cb;
            _isFirst = false;
        }
    }

    private ConstantBufferData BuildConstantBuffer(int frame, int length, int fps)
    {
        double num  = _param.Size.GetValue(frame, length, fps) / 100.0;
        double num2 = Math.Max(0.001, _param.ScaleX.GetValue(frame, length, fps) * num);
        double num3 = Math.Max(0.001, _param.ScaleY.GetValue(frame, length, fps) * num);
        double num4 = Math.Max(0.001, _param.ScaleZ.GetValue(frame, length, fps) * num);

        double valX = _param.X.GetValue(frame, length, fps);
        double valY = _param.Y.GetValue(frame, length, fps);
        double valZ = _param.Z.GetValue(frame, length, fps);
        double spdX = _param.SpeedX.GetValue(frame, length, fps);
        double spdY = _param.SpeedY.GetValue(frame, length, fps);
        double spdZ = _param.SpeedZ.GetValue(frame, length, fps);

        double num6 = (valX + spdX * frame) % 123456.789;
        double num7 = (valY + spdY * frame) % 154321.0987;
        double num8 = (valZ + spdZ * frame) % 160213.8549;
        double num9 = _param.Angle.GetValue(frame, length, fps) / 180.0 * Math.PI;

        double warpScale = Math.Max(0.01, _param.WarpScale.GetValue(frame, length, fps) / 100.0);
        int seedVal = _param.IsUniqueSeed ? (int)_param.Seed.GetValue(frame, length, fps) : 0;

        return new ConstantBufferData(
            TotalX:       (float)num6,
            TotalY:       (float)num7,
            TotalZ:       (float)num8,
            Threshold:    (float)(_param.Threshold.GetValue(frame, length, fps) / 100.0),
            ScaleX:       (float)(100.0 / num2),
            ScaleY:       (float)(100.0 / num3),
            ScaleZ:       (float)(100.0 / num4),
            Strength:     (float)(_param.Strength.GetValue(frame, length, fps) / 100.0),
            Octaves:      (int)_param.Octaves.GetValue(frame, length, fps),
            Seed:         seedVal % 1234,
            Levels:       (int)_param.Levels.GetValue(frame, length, fps),
            Angle:        (float)(0.0 - num9),
            WarpStrength: (float)_param.WarpStrength.GetValue(frame, length, fps),
            WarpScale:    (float)warpScale,
            FractalMode:  (int)_param.FractalBlend,
            DistanceMode: (int)_param.DistanceMode,
            Lacunarity:   (float)_param.Lacunarity.GetValue(frame, length, fps),
            Gain:         (float)_param.Gain.GetValue(frame, length, fps),
            IsColor:      _param.IsColor  ? 1 : 0,
            IsAlpha:      _param.UseAlpha ? 1 : 0);
    }

    public void Dispose()
    {
        _output?.Dispose();
        _output = null;
        _effect?.SetInput(0, null, true);
        _effect?.Dispose();
        _effect = null;
        _flood?.Dispose();
        _flood = null;
    }
}
