using System;
using System.Runtime.InteropServices;
using Vortice;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace RadianceLite;

internal sealed class RadianceLiteCompositeCustomEffect(IGraphicsDevicesAndContext devices) : D2D1CustomShaderEffectBase(Create<EffectImpl>(devices))
{
    public float Strength { set => SetValue((int)EffectImpl.Properties.Strength, value); }
    public float Diffuse { set => SetValue((int)EffectImpl.Properties.Diffuse, value); }
    public float Ambient { set => SetValue((int)EffectImpl.Properties.Ambient, value); }
    public float Occlusion { set => SetValue((int)EffectImpl.Properties.Occlusion, value); }
    public float RangePx { set => SetValue((int)EffectImpl.Properties.RangePx, value); }

    [CustomEffect(4)]
    private sealed class EffectImpl : D2D1CustomShaderEffectImplBase<EffectImpl>
    {
        private ConstantBuffer _cb = new() { Strength = 1f, Diffuse = 0.6f, Ambient = 1f, Occlusion = 0.8f, NearW = 1.35f, MidW = 1.85f, FarW = 1.30f };
        private float _rangePx = 300f;
        private RawRect _outputRect;

        [CustomEffectProperty(PropertyType.Float, (int)Properties.Strength)]
        public float Strength { get => _cb.Strength; set { _cb.Strength = Math.Clamp(value, 0f, 8f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.Diffuse)]
        public float Diffuse { get => _cb.Diffuse; set { _cb.Diffuse = Math.Clamp(value, 0f, 1f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.Ambient)]
        public float Ambient { get => _cb.Ambient; set { _cb.Ambient = Math.Clamp(value, 0f, 1f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.Occlusion)]
        public float Occlusion { get => _cb.Occlusion; set { _cb.Occlusion = Math.Clamp(value, 0f, 1f); UpdateConstants(); } }

        [CustomEffectProperty(PropertyType.Float, (int)Properties.RangePx)]
        public float RangePx { get => _rangePx; set => _rangePx = Math.Clamp(value, 1f, 4096f); }

        public EffectImpl() : base(ShaderResourceUri.Get("RadianceLiteComposite"))
        {
        }

        protected override void UpdateConstants()
        {
            drawInformation?.SetPixelShaderConstantBuffer(_cb);
        }

        public override void MapInputRectsToOutputRect(RawRect[] inputRects, RawRect[] inputOpaqueSubRects, out RawRect outputRect, out RawRect outputOpaqueSubRect)
        {
            if (inputRects.Length != 4)
                throw new ArgumentException("InputRects must be length of 4", nameof(inputRects));

            var inputRect = ClampInputRect(inputRects[0]);
            outputOpaqueSubRect = default;

            if (inputRect.Right <= inputRect.Left || inputRect.Bottom <= inputRect.Top)
            {
                outputRect = inputRect;
                return;
            }

            var pad = (int)MathF.Ceiling(Math.Clamp(_rangePx, 1f, 4096f)) + 8;
            var worldL = (long)inputRect.Left - pad;
            var worldT = (long)inputRect.Top - pad;
            var worldW = (long)inputRect.Right - inputRect.Left + pad * 2;
            var worldH = (long)inputRect.Bottom - inputRect.Top + pad * 2;

            outputRect = new RawRect(
                Saturate(worldL),
                Saturate(worldT),
                Saturate(worldL + worldW),
                Saturate(worldT + worldH));

            _outputRect = outputRect;
        }

        public override void MapOutputRectToInputRects(RawRect outputRect, RawRect[] inputRects)
        {
            inputRects[0] = outputRect;
            inputRects[1] = outputRect;
            inputRects[2] = outputRect;
            inputRects[3] = outputRect;
        }

        private static int Saturate(long value) => (int)Math.Clamp(value, int.MinValue, int.MaxValue);


        [StructLayout(LayoutKind.Sequential)]
        private struct ConstantBuffer
        {
            public float Strength;
            public float Diffuse;
            public float Ambient;
            public float Occlusion;
            public float NearW;
            public float MidW;
            public float FarW;
            public float Pad0;
        }

        public enum Properties : int
        {
            Strength = 0,
            Diffuse = 1,
            Ambient = 2,
            Occlusion = 3,
            RangePx = 4,
        }
    }
}
