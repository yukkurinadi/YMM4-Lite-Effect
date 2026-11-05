using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace VoronoiNoiseLite;

/// <summary>
/// D2D1 カスタムエフェクトラッパー（YMM4完全互換・超軽量ボロノイノイズ）。
///
/// 最適化ポイント:
///   - <see cref="UpdateBuffer"/> は <see cref="EffectImpl.BulkSet"/> を呼び、
///     20 フィールドを一括コピーして <c>SetPixelShaderConstantBuffer</c> を
///     1 回だけ呼びます（旧実装の 20 回 SetValue → 20 GPU 呼び出し → 1 GPU 呼び出し）。
///   - 実際の呼び出しは <see cref="VoronoiNoiseLiteEffectProcessor"/> / 
///     <see cref="VoronoiNoiseLiteShapeSource"/> 側の差分チェック後のみ行われます。
/// </summary>
internal sealed class VoronoiNoiseLiteCustomEffect : D2D1CustomShaderEffectBase
{
    // EffectImpl.LastCreated 経由で EffectImpl インスタンスを直接保持
    private readonly EffectImpl _impl;

    public VoronoiNoiseLiteCustomEffect(IGraphicsDevicesAndContext devices)
        : base(Create<EffectImpl>(devices))
    {
        // Create<EffectImpl> が EffectImpl のコンストラクターを同期呼び出しし、
        // LastCreated にセットする → ここで回収して null にリセット
        _impl = EffectImpl.LastCreated
            ?? throw new InvalidOperationException("EffectImpl not initialized");
        EffectImpl.LastCreated = null;
    }

    /// <summary>
    /// 全パラメーターを GPU 定数バッファへ 1 回の <c>SetPixelShaderConstantBuffer</c>
    /// 呼び出しで転送します。差分チェックは呼び出し側で完了済みです。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateBuffer(in ConstantBufferData cb)
        => _impl.BulkSet(in cb);

    // ── D2D1 カスタムエフェクト実装 ──────────────────────────────────────

    [CustomEffect(1)]
    private sealed class EffectImpl : D2D1CustomShaderEffectImplBase<EffectImpl>
    {
        /// <summary>
        /// Create&lt;EffectImpl&gt; が同期的にコンストラクターを呼ぶので、
        /// [ThreadStatic] でスレッドごとに安全にインスタンスを受け渡します。
        /// </summary>
        [ThreadStatic]
        internal static EffectImpl? LastCreated;

        // GPU 定数バッファの実体（HLSL cbuffer Constants (b0) と 1:1 対応）
        private GpuCb _cb = new()
        {
            TotalX       = 0f,
            TotalY       = 0f,
            TotalZ       = 0f,
            Threshold    = 0f,
            ScaleX       = 1f,
            ScaleY       = 1f,
            ScaleZ       = 1f,
            Strength     = 1f,
            Octaves      = 5,
            Seed         = 0,
            Levels       = 256,
            Angle        = 0f,
            WarpStrength = 0f,
            WarpScale    = 1f,
            FractalMode  = 0,
            DistanceMode = 0,
            Lacunarity   = 2f,
            Gain         = 0.5f,
            IsColor      = 0,
            IsAlpha      = 1,
        };

        public EffectImpl() : base(ShaderResourceUri.Get("VoronoiNoiseLite"))
        {
            // 外側クラスが回収できるよう自身を ThreadStatic フィールドに登録
            LastCreated = this;
        }

        /// <summary>
        /// ConstantBufferData → GpuCb へ unsafe 一括コピーし、
        /// <c>SetPixelShaderConstantBuffer</c> を 1 回だけ呼びます。
        /// ConstantBufferData と GpuCb は Pack=4 で完全同一レイアウトなので安全です。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void BulkSet(in ConstantBufferData src)
        {
            // ConstantBufferData と GpuCb は同一レイアウト → Unsafe.As で 0 コピー変換
            _cb = Unsafe.As<ConstantBufferData, GpuCb>(ref Unsafe.AsRef(in src));
            drawInformation?.SetPixelShaderConstantBuffer(in _cb);
        }

        // ── D2D1 プロパティ登録（CustomEffectProperty 属性が必須） ─────────
        // 注: ホットパスでは BulkSet を使うので、以下の setter は UI/デバッグ用途のみ。
        //     UpdateConstants() の呼び出しは各 setter で完結（問題なし）。

        [CustomEffectProperty(PropertyType.Float, 0)]
        public float TotalX
        {
            get => _cb.TotalX;
            set { _cb.TotalX = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Float, 1)]
        public float TotalY
        {
            get => _cb.TotalY;
            set { _cb.TotalY = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Float, 2)]
        public float TotalZ
        {
            get => _cb.TotalZ;
            set { _cb.TotalZ = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Float, 3)]
        public float Threshold
        {
            get => _cb.Threshold;
            set { _cb.Threshold = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Float, 4)]
        public float ScaleX
        {
            get => _cb.ScaleX;
            set { _cb.ScaleX = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Float, 5)]
        public float ScaleY
        {
            get => _cb.ScaleY;
            set { _cb.ScaleY = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Float, 6)]
        public float ScaleZ
        {
            get => _cb.ScaleZ;
            set { _cb.ScaleZ = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Float, 7)]
        public float Strength
        {
            get => _cb.Strength;
            set { _cb.Strength = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Int32, 8)]
        public int Octaves
        {
            get => _cb.Octaves;
            set { _cb.Octaves = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Int32, 9)]
        public int Seed
        {
            get => _cb.Seed;
            set { _cb.Seed = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Int32, 10)]
        public int Levels
        {
            get => _cb.Levels;
            set { _cb.Levels = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Float, 11)]
        public float Angle
        {
            get => _cb.Angle;
            set { _cb.Angle = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Float, 12)]
        public float WarpStrength
        {
            get => _cb.WarpStrength;
            set { _cb.WarpStrength = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Float, 13)]
        public float WarpScale
        {
            get => _cb.WarpScale;
            set { _cb.WarpScale = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Int32, 14)]
        public int FractalMode
        {
            get => _cb.FractalMode;
            set { _cb.FractalMode = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Int32, 15)]
        public int DistanceMode
        {
            get => _cb.DistanceMode;
            set { _cb.DistanceMode = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Float, 16)]
        public float Lacunarity
        {
            get => _cb.Lacunarity;
            set { _cb.Lacunarity = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Float, 17)]
        public float Gain
        {
            get => _cb.Gain;
            set { _cb.Gain = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Int32, 18)]
        public int IsColor
        {
            get => _cb.IsColor;
            set { _cb.IsColor = value; UpdateConstants(); }
        }

        [CustomEffectProperty(PropertyType.Int32, 19)]
        public int IsAlpha
        {
            get => _cb.IsAlpha;
            set { _cb.IsAlpha = value; UpdateConstants(); }
        }

        protected override void UpdateConstants()
        {
            drawInformation?.SetPixelShaderConstantBuffer(in _cb);
        }

        public override void MapOutputRectToInputRects(RawRect outputRect, RawRect[] inputRects)
        {
            inputRects[0] = outputRect;
        }

        // ── GPU 定数バッファ構造体（HLSL cbuffer Constants (b0) と完全一致） ──
        // ConstantBufferData と Pack=4・フィールド順が同一なので Unsafe.As が安全。
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct GpuCb
        {
            public float TotalX;       // c0.x
            public float TotalY;       // c0.y
            public float TotalZ;       // c0.z
            public float Threshold;    // c0.w
            public float ScaleX;       // c1.x
            public float ScaleY;       // c1.y
            public float ScaleZ;       // c1.z
            public float Strength;     // c1.w
            public int   Octaves;      // c2.x
            public int   Seed;         // c2.y
            public int   Levels;       // c2.z
            public float Angle;        // c2.w
            public float WarpStrength; // c3.x
            public float WarpScale;    // c3.y
            public int   FractalMode;  // c3.z
            public int   DistanceMode; // c3.w
            public float Lacunarity;   // c4.x
            public float Gain;         // c4.y
            public int   IsColor;      // c4.z
            public int   IsAlpha;      // c4.w
        }
    }
}
