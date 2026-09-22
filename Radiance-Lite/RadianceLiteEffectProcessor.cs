using System;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Player.Video.Effects;

namespace RadianceLite;

internal sealed class RadianceLiteEffectProcessor(
    IGraphicsDevicesAndContext devices,
    RadianceLiteEffect item) : VideoEffectProcessorBase(devices)
{
    private readonly RadianceLiteEffect _item = item;
    private RadianceLiteEmissionCustomEffect? _emissionEffect;
    private GaussianBlur? _blurNear;
    private GaussianBlur? _blurMid;
    private GaussianBlur? _blurFar;
    private RadianceLiteCompositeCustomEffect? _compositeEffect;

    private bool _isFirst = true;
    private Parameters _parameters;

    public override DrawDescription Update(EffectDescription effectDescription)
    {
        if (IsPassThroughEffect || _emissionEffect is null || _blurNear is null || _blurMid is null || _blurFar is null || _compositeEffect is null)
            return effectDescription.DrawDescription;

        var frame = effectDescription.ItemPosition.Frame;
        var length = effectDescription.ItemDuration.Frame;
        var fps = effectDescription.FPS;

        var lightColor = _item.LightColor;
        var lightAlpha = lightColor.A / 255f;

        var parameters = new Parameters(
            (float)(_item.Strength.GetValue(frame, length, fps) / 100.0),
            (float)_item.Range.GetValue(frame, length, fps),
            (float)(_item.Diffuse.GetValue(frame, length, fps) / 100.0),
            (float)(_item.Ambient.GetValue(frame, length, fps) / 100.0),
            (float)(_item.Threshold.GetValue(frame, length, fps) / 100.0),
            (float)(_item.EmissionGain.GetValue(frame, length, fps) / 100.0) * lightAlpha,
            (float)(_item.Occlusion.GetValue(frame, length, fps) / 100.0),
            lightColor.R / 255f,
            lightColor.G / 255f,
            lightColor.B / 255f);

        if (_isFirst || _parameters.Strength != parameters.Strength)
            _compositeEffect.Strength = parameters.Strength;
        if (_isFirst || _parameters.Diffuse != parameters.Diffuse)
            _compositeEffect.Diffuse = parameters.Diffuse;
        if (_isFirst || _parameters.Ambient != parameters.Ambient)
            _compositeEffect.Ambient = parameters.Ambient;
        if (_isFirst || _parameters.Occlusion != parameters.Occlusion)
        {
            _compositeEffect.Occlusion = parameters.Occlusion;
            _emissionEffect.Occlusion = parameters.Occlusion;
        }

        if (_isFirst || _parameters.Threshold != parameters.Threshold)
            _emissionEffect.Threshold = parameters.Threshold;
        if (_isFirst || _parameters.EmissionGain != parameters.EmissionGain)
            _emissionEffect.Gain = parameters.EmissionGain;

        if (_isFirst || _parameters.TintR != parameters.TintR)
            _emissionEffect.TintR = parameters.TintR;
        if (_isFirst || _parameters.TintG != parameters.TintG)
            _emissionEffect.TintG = parameters.TintG;
        if (_isFirst || _parameters.TintB != parameters.TintB)
            _emissionEffect.TintB = parameters.TintB;

        if (_isFirst || _parameters.Range != parameters.Range)
        {
            _compositeEffect.RangePx = parameters.Range;
            var r = Math.Max(parameters.Range, 2f);
            _blurNear.StandardDeviation = Math.Clamp(r * 0.04f, 0.5f, 250f);
            _blurMid.StandardDeviation = Math.Clamp(r * 0.18f, 1f, 250f);
            _blurFar.StandardDeviation = Math.Clamp(r * 0.60f, 2f, 250f);
        }

        _parameters = parameters;
        _isFirst = false;

        return effectDescription.DrawDescription;
    }

    protected override ID2D1Image? CreateEffect(IGraphicsDevicesAndContext devices)
    {
        _emissionEffect = new RadianceLiteEmissionCustomEffect(devices);
        _blurNear = new GaussianBlur(devices.DeviceContext)
        {
            Optimization = GaussianBlurOptimization.Speed,
            BorderMode = BorderMode.Soft,
            StandardDeviation = 12f
        };
        _blurMid = new GaussianBlur(devices.DeviceContext)
        {
            Optimization = GaussianBlurOptimization.Speed,
            BorderMode = BorderMode.Soft,
            StandardDeviation = 54f
        };
        _blurFar = new GaussianBlur(devices.DeviceContext)
        {
            Optimization = GaussianBlurOptimization.Speed,
            BorderMode = BorderMode.Soft,
            StandardDeviation = 180f
        };
        _compositeEffect = new RadianceLiteCompositeCustomEffect(devices);

        if (!_emissionEffect.IsEnabled || !_compositeEffect.IsEnabled)
        {
            _emissionEffect.Dispose();
            _emissionEffect = null;
            _blurNear.Dispose();
            _blurNear = null;
            _blurMid.Dispose();
            _blurMid = null;
            _blurFar.Dispose();
            _blurFar = null;
            _compositeEffect.Dispose();
            _compositeEffect = null;
            return null;
        }

        disposer.Collect(_emissionEffect);
        disposer.Collect(_blurNear);
        disposer.Collect(_blurMid);
        disposer.Collect(_blurFar);
        disposer.Collect(_compositeEffect);

        using (var emissionOutput = _emissionEffect.Output)
        {
            _blurNear.SetInput(0, emissionOutput, true);
            _blurMid.SetInput(0, emissionOutput, true);
            _blurFar.SetInput(0, emissionOutput, true);
        }

        using (var nearOutput = _blurNear.Output)
            _compositeEffect.SetInput(1, nearOutput, true);
        using (var midOutput = _blurMid.Output)
            _compositeEffect.SetInput(2, midOutput, true);
        using (var farOutput = _blurFar.Output)
            _compositeEffect.SetInput(3, farOutput, true);

        var output = _compositeEffect.Output;
        disposer.Collect(output);
        return output;
    }

    protected override void setInput(ID2D1Image? input)
    {
        _emissionEffect?.SetInput(0, input, true);
        _compositeEffect?.SetInput(0, input, true);
    }

    protected override void ClearEffectChain()
    {
        _emissionEffect?.SetInput(0, null, true);
        _blurNear?.SetInput(0, null, true);
        _blurMid?.SetInput(0, null, true);
        _blurFar?.SetInput(0, null, true);
        _compositeEffect?.SetInput(0, null, true);
        _compositeEffect?.SetInput(1, null, true);
        _compositeEffect?.SetInput(2, null, true);
        _compositeEffect?.SetInput(3, null, true);
        _isFirst = true;
    }

    private readonly record struct Parameters(
        float Strength,
        float Range,
        float Diffuse,
        float Ambient,
        float Threshold,
        float EmissionGain,
        float Occlusion,
        float TintR,
        float TintG,
        float TintB);
}
