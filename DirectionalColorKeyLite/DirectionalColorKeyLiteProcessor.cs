using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Vortice.DCommon;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using Vortice.DXGI;
using Vortice.Mathematics;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using Color = System.Windows.Media.Color;
using PixelFormat = Vortice.DCommon.PixelFormat;

namespace DirectionalColorKeyLite;

/// <summary>
/// 超軽量・爆速透過クロマキー プロセッサ (Vlahos方程式・完全ゼロループ・高速並列演算モデル)
/// 髪の毛などの半透明ディテールを高品位に抽出しつつ、四則演算とmaxのみで低スペックPCでも数ミリ秒未満で処理します。
/// </summary>
public sealed class DirectionalColorKeyLiteProcessor : IVideoEffectProcessor, IDisposable
{
    private static readonly float[] InvByteLut = new float[256];

    static DirectionalColorKeyLiteProcessor()
    {
        for (int i = 0; i < 256; i++)
        {
            InvByteLut[i] = i > 0 ? 1.0f / i : 0f;
        }
    }

    private readonly IGraphicsDevicesAndContext devices;
    private readonly DirectionalColorKeyLiteEffect item;

    private ID2D1Image? input;
    private ID2D1Bitmap1? renderTargetBitmap;
    private ID2D1Bitmap1? readableBitmap;
    private ID2D1Bitmap1? outputBitmap;
    private AffineTransform2D? transformEffect;
    private ID2D1Image? outputImage;

    private int currentWidth = 0;
    private int currentHeight = 0;
    private bool disposedValue = false;

    private const int MaxDimensionLimit = 4096;

    public ID2D1Image Output => outputImage ?? input ?? throw new InvalidOperationException("Effect not initialized.");

    public DirectionalColorKeyLiteProcessor(IGraphicsDevicesAndContext devices, DirectionalColorKeyLiteEffect item)
    {
        this.devices = devices;
        this.item = item;
    }

    public void SetInput(ID2D1Image? input) => this.input = input;
    public void ClearInput() => this.input = null;

    public DrawDescription Update(EffectDescription effectDescription)
    {
        var drawDesc = effectDescription.DrawDescription;
        var dc = devices.DeviceContext;
        if (input == null || dc == null)
            return drawDesc;

        int frame = effectDescription.ItemPosition.Frame;
        int length = effectDescription.ItemDuration.Frame;
        int fps = effectDescription.FPS;

        var bg = item.BackgroundColor;
        float sensitivity = (float)(item.Sensitivity.GetValue(frame, length, fps) / 100.0);
        float threshold = (float)(item.Threshold.GetValue(frame, length, fps) / 100.0);
        float edgeSoftness = (float)(item.EdgeSoftness.GetValue(frame, length, fps) / 100.0);
        float spillStrength = (float)(item.SpillStrength.GetValue(frame, length, fps) / 100.0);
        bool outputForeground = item.OutputForeground;

        try
        {
            var bounds = dc.GetImageLocalBounds(input);
            int origWidth = (int)Math.Ceiling(Math.Max(1.0, bounds.Right - bounds.Left));
            int origHeight = (int)Math.Ceiling(Math.Max(1.0, bounds.Bottom - bounds.Top));

            if (origWidth <= 0 || origHeight <= 0)
                return drawDesc;

            int maxAllowed = Math.Min(MaxDimensionLimit, (int)dc.MaximumBitmapSize);
            float scale = 1.0f;
            if (origWidth > maxAllowed || origHeight > maxAllowed)
                scale = Math.Min((float)maxAllowed / origWidth, (float)maxAllowed / origHeight);

            int width = Math.Max(1, (int)Math.Round(origWidth * scale));
            int height = Math.Max(1, (int)Math.Round(origHeight * scale));

            if (!EnsureBuffers(width, height))
                return drawDesc;

            // 1. Render input to renderTargetBitmap
            var prevTarget = dc.Target;
            dc.Target = renderTargetBitmap;
            dc.BeginDraw();
            dc.Clear(new Color4(0, 0, 0, 0));
            if (Math.Abs(scale - 1.0f) > 0.001f)
            {
                var prevTransform = dc.Transform;
                dc.Transform = Matrix3x2.CreateTranslation(-bounds.Left, -bounds.Top) * Matrix3x2.CreateScale(scale);
                dc.DrawImage(input);
                dc.Transform = prevTransform;
            }
            else
            {
                dc.DrawImage(input, new Vector2(-bounds.Left, -bounds.Top));
            }
            dc.EndDraw();

            // 2. Copy to CPU-readable bitmap
            readableBitmap!.CopyFromBitmap(renderTargetBitmap!);

            // 3. Map and process on CPU
            var map = readableBitmap.Map(MapOptions.Read);
            int stride = map.Pitch;
            int dataSize = stride * height;

            byte[] buffer = ArrayPool<byte>.Shared.Rent(dataSize);
            try
            {
                unsafe
                {
                    fixed (byte* pDst = buffer)
                    {
                        Buffer.MemoryCopy((void*)map.Bits, pDst, dataSize, dataSize);
                    }
                }
                readableBitmap.Unmap();

                // Execute ultra-fast Vlahos color keying
                ProcessVlahosKey(buffer, width, height, stride,
                    bg, sensitivity, threshold, edgeSoftness, spillStrength, outputForeground);

                outputBitmap!.CopyFromMemory(buffer, stride);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            // 4. Restore position
            transformEffect!.TransformMatrix = Math.Abs(scale - 1.0f) > 0.001f
                ? Matrix3x2.CreateScale(1.0f / scale) * Matrix3x2.CreateTranslation(bounds.Left, bounds.Top)
                : Matrix3x2.CreateTranslation(bounds.Left, bounds.Top);

            dc.Target = prevTarget;
        }
        catch
        {
            return drawDesc;
        }

        return drawDesc;
    }

    // ──────────────────────────────────────────────
    // Vlahos 式 色分離コアエンジン (完全ゼロループ・超高速並列処理)
    // ──────────────────────────────────────────────
    private static unsafe void ProcessVlahosKey(
        byte[] buffer, int width, int height, int stride,
        Color bg, float a1, float a2, float edgeSoftness, float spillStrength, bool outputForeground)
    {
        float invOneMinusEdge = 1.0f / MathF.Max(1.0f - edgeSoftness, 1e-5f);

        // 背景色のRGB値から主要チャンネルを判定
        byte bgR = bg.R;
        byte bgG = bg.G;
        byte bgB = bg.B;

        // 代表的な単色（グリーン/ブルー/レッド）か汎用カスタム色かを事前判定
        int mode;
        if (bgG >= bgR * 1.3f && bgG >= bgB * 1.3f)
        {
            mode = 0; // Pure Green
        }
        else if (bgB >= bgR * 1.3f && bgB >= bgG * 1.3f)
        {
            mode = 1; // Pure Blue
        }
        else if (bgR >= bgG * 1.3f && bgR >= bgB * 1.3f)
        {
            mode = 2; // Pure Red
        }
        else
        {
            mode = 3; // Custom / Arbitrary color
        }

        float cR = bgR / 255f;
        float cG = bgG / 255f;
        float cB = bgB / 255f;
        float cSum = MathF.Max(cR + cG + cB, 1e-4f);
        float wR = cR / cSum;
        float wG = cG / cSum;
        float wB = cB / cSum;

        fixed (byte* pBuf = buffer)
        {
            IntPtr baseAddr = (IntPtr)pBuf;
            Parallel.For(0, height, y =>
            {
                byte* row = (byte*)baseAddr + y * stride;
                for (int x = 0; x < width; x++)
                {
                    byte* px = row + x * 4;
                    byte srcA = px[3];
                    if (srcA == 0)
                        continue;

                    float b = px[0] * (1.0f / 255f);
                    float g = px[1] * (1.0f / 255f);
                    float r = px[2] * (1.0f / 255f);

                    // 透過前のストレートRGBに復元（半透明合成時）
                    if (srcA < 255)
                    {
                        float invA = InvByteLut[srcA];
                        r = Math.Clamp(px[2] * invA, 0f, 1f);
                        g = Math.Clamp(px[1] * invA, 0f, 1f);
                        b = Math.Clamp(px[0] * invA, 0f, 1f);
                    }

                    float alpha;
                    float fgR = r;
                    float fgG = g;
                    float fgB = b;

                    // ── Vlahos 色抽出・スピル除去 ──
                    switch (mode)
                    {
                        case 0: // グリーンバック (最速分岐)
                        default:
                            {
                                float maxOther = MathF.Max(r, b);
                                alpha = 1.0f - a1 * (g - a2 * maxOther);
                                alpha = Math.Clamp(alpha, 0f, 1f);

                                if (g > maxOther && spillStrength > 0.001f)
                                {
                                    float cleanG = maxOther;
                                    fgG = MathHelper.Lerp(g, cleanG, spillStrength);
                                }
                            }
                            break;

                        case 1: // ブルーバック
                            {
                                float maxOther = MathF.Max(r, g);
                                alpha = 1.0f - a1 * (b - a2 * maxOther);
                                alpha = Math.Clamp(alpha, 0f, 1f);

                                if (b > maxOther && spillStrength > 0.001f)
                                {
                                    float cleanB = maxOther;
                                    fgB = MathHelper.Lerp(b, cleanB, spillStrength);
                                }
                            }
                            break;

                        case 2: // レッドバック
                            {
                                float maxOther = MathF.Max(g, b);
                                alpha = 1.0f - a1 * (r - a2 * maxOther);
                                alpha = Math.Clamp(alpha, 0f, 1f);

                                if (r > maxOther && spillStrength > 0.001f)
                                {
                                    float cleanR = maxOther;
                                    fgR = MathHelper.Lerp(r, cleanR, spillStrength);
                                }
                            }
                            break;

                        case 3: // 任意カスタム色
                            {
                                float keyVal = r * wR + g * wG + b * wB;
                                float otherVal = (r * (1f - wR) + g * (1f - wG) + b * (1f - wB)) * 0.5f;
                                alpha = 1.0f - a1 * (keyVal - a2 * otherVal);
                                alpha = Math.Clamp(alpha, 0f, 1f);

                                if (keyVal > otherVal && spillStrength > 0.001f)
                                {
                                    float diff = (keyVal - otherVal) * spillStrength;
                                    fgR = MathF.Max(0f, r - diff * wR);
                                    fgG = MathF.Max(0f, g - diff * wG);
                                    fgB = MathF.Max(0f, b - diff * wB);
                                }
                            }
                            break;
                    }

                    // エッジ柔らかさ（低アルファ側の滑らかな足切り・フェード）
                    if (edgeSoftness > 0.001f)
                    {
                        alpha = Math.Clamp((alpha - edgeSoftness) * invOneMinusEdge, 0f, 1f);
                    }

                    if (alpha <= 0f)
                    {
                        *(uint*)px = 0;
                        continue;
                    }

                    float srcAlphaNorm = srcA * (1.0f / 255f);
                    float outAlpha = alpha * srcAlphaNorm;

                    if (!outputForeground)
                    {
                        // アルファマスク（白黒）のみ出力
                        byte m = (byte)Math.Clamp((int)(outAlpha * 255f + 0.5f), 0, 255);
                        px[0] = m; px[1] = m; px[2] = m; px[3] = m;
                        continue;
                    }

                    // 乗算済みアルファ (Premultiplied Alpha) 出力
                    px[0] = (byte)Math.Clamp((int)(fgB * outAlpha * 255f + 0.5f), 0, 255);
                    px[1] = (byte)Math.Clamp((int)(fgG * outAlpha * 255f + 0.5f), 0, 255);
                    px[2] = (byte)Math.Clamp((int)(fgR * outAlpha * 255f + 0.5f), 0, 255);
                    px[3] = (byte)Math.Clamp((int)(outAlpha * 255f + 0.5f), 0, 255);
                }
            });
        }
    }

    // ──────────────────────────────────────────────
    // Buffer management
    // ──────────────────────────────────────────────
    private bool EnsureBuffers(int width, int height)
    {
        if (currentWidth == width && currentHeight == height
            && outputBitmap != null && transformEffect != null && outputImage != null)
            return true;

        DisposeBuffers();

        try
        {
            currentWidth = width;
            currentHeight = height;

            var dc = devices.DeviceContext;
            var pixFmt = new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied);
            var size = new SizeI(width, height);

            renderTargetBitmap = dc.CreateBitmap(size, IntPtr.Zero, 0, new BitmapProperties1
            {
                PixelFormat = pixFmt, DpiX = 96f, DpiY = 96f,
                BitmapOptions = BitmapOptions.Target
            });
            readableBitmap = dc.CreateBitmap(size, IntPtr.Zero, 0, new BitmapProperties1
            {
                PixelFormat = pixFmt, DpiX = 96f, DpiY = 96f,
                BitmapOptions = BitmapOptions.CpuRead | BitmapOptions.CannotDraw
            });
            outputBitmap = dc.CreateBitmap(size, IntPtr.Zero, 0, new BitmapProperties1
            {
                PixelFormat = pixFmt, DpiX = 96f, DpiY = 96f,
                BitmapOptions = BitmapOptions.None
            });

            transformEffect = new AffineTransform2D(dc);
            transformEffect.SetInput(0, outputBitmap, true);
            outputImage = transformEffect.Output;

            return true;
        }
        catch
        {
            DisposeBuffers();
            return false;
        }
    }

    private void DisposeBuffers()
    {
        outputImage?.Dispose(); outputImage = null;
        transformEffect?.Dispose(); transformEffect = null;
        outputBitmap?.Dispose(); outputBitmap = null;
        readableBitmap?.Dispose(); readableBitmap = null;
        renderTargetBitmap?.Dispose(); renderTargetBitmap = null;
        currentWidth = 0;
        currentHeight = 0;
    }

    public void Dispose()
    {
        if (!disposedValue)
        {
            DisposeBuffers();
            input = null;
            disposedValue = true;
        }
        GC.SuppressFinalize(this);
    }
}
