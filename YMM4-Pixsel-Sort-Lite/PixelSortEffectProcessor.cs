using System;
using System.Buffers;
using System.IO;
using System.Numerics;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using Vortice.DCommon;
using Vortice.DXGI;
using Vortice.Mathematics;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace LiteEffect;

public class PixelSortEffectProcessor : IVideoEffectProcessor, IDisposable
{
    private readonly IGraphicsDevicesAndContext devices;
    private readonly PixelSortEffect item;

    private ID2D1Image? input;
    private ID2D1Bitmap1? renderTargetBitmap;
    private ID2D1Bitmap1? readableBitmap;
    private ID2D1Bitmap1? outputBitmap;
    private AffineTransform2D? transformEffect;
    private ID2D1Image? outputImage;

    private int currentWidth = 0;
    private int currentHeight = 0;
    private bool disposedValue = false;

    // 安全のための最大解像度（4K: 3840x2160 または 4096）
    private const int MaxDimensionLimit = 4096;

    public ID2D1Image Output => outputImage ?? input!;

    public PixelSortEffectProcessor(IGraphicsDevicesAndContext devices, PixelSortEffect item)
    {
        this.devices = devices;
        this.item = item;
    }

    public void SetInput(ID2D1Image? input)
    {
        this.input = input;
    }

    public void ClearInput()
    {
        input = null;
    }

    public DrawDescription Update(EffectDescription effectDescription)
    {
        var drawDesc = effectDescription.DrawDescription;
        if (input == null)
            return drawDesc;

        int frame = effectDescription.ItemPosition.Frame;
        int length = effectDescription.ItemDuration.Frame;
        int fps = effectDescription.FPS;

        // アニメーション値の取得
        float angle = (float)item.Angle.GetValue(frame, length, fps);
        float threshMin = (float)(item.ThresholdMin.GetValue(frame, length, fps) / 100.0);
        float threshMax = (float)(item.ThresholdMax.GetValue(frame, length, fps) / 100.0);
        float randomInterval = (float)(item.RandomInterval.GetValue(frame, length, fps) / 100.0);
        float blend = (float)(item.Blend.GetValue(frame, length, fps) / 100.0);

        var mode = item.Mode;
        var criterion = item.Criterion;
        bool invert = item.InvertThreshold;
        bool includeTransparent = item.IncludeTransparent;
        int seed = item.Seed;

        var d2dContext = devices.DeviceContext;

        try
        {
            // 入力画像のバウンディングボックスを取得
            var bounds = d2dContext.GetImageLocalBounds(input);
            float rawWidth = Math.Max(1.0f, bounds.Right - bounds.Left);
            float rawHeight = Math.Max(1.0f, bounds.Bottom - bounds.Top);

            int origWidth = (int)Math.Ceiling(rawWidth);
            int origHeight = (int)Math.Ceiling(rawHeight);

            if (origWidth <= 0 || origHeight <= 0 || blend <= 0.0001f)
            {
                return drawDesc;
            }

            // 8Kなどの超巨大画像時のメモリ枯渇(E_OUTOFMEMORY)を防ぐため、安全な最大解像度に制限
            int maxAllowed = Math.Min(MaxDimensionLimit, (int)d2dContext.MaximumBitmapSize);
            float scale = 1.0f;
            if (origWidth > maxAllowed || origHeight > maxAllowed)
            {
                scale = Math.Min((float)maxAllowed / origWidth, (float)maxAllowed / origHeight);
            }

            int width = Math.Max(1, (int)Math.Round(origWidth * scale));
            int height = Math.Max(1, (int)Math.Round(origHeight * scale));

            // 必要に応じてビットマップリソースを確保/再作成
            if (!EnsureBuffers(width, height))
            {
                return drawDesc;
            }

            if (renderTargetBitmap == null || readableBitmap == null || outputBitmap == null || transformEffect == null)
            {
                return drawDesc;
            }

            // 1. 入力画像を renderTargetBitmap にオフスクリーン描画（スケール考慮）
            var prevTarget = d2dContext.Target;
            d2dContext.Target = renderTargetBitmap;
            d2dContext.BeginDraw();
            d2dContext.Clear(new Color4(0, 0, 0, 0));

            if (Math.Abs(scale - 1.0f) > 0.001f)
            {
                var prevTransform = d2dContext.Transform;
                d2dContext.Transform = Matrix3x2.CreateTranslation(-bounds.Left, -bounds.Top) * Matrix3x2.CreateScale(scale);
                d2dContext.DrawImage(input);
                d2dContext.Transform = prevTransform;
            }
            else
            {
                d2dContext.DrawImage(input, new Vector2(-bounds.Left, -bounds.Top));
            }
            d2dContext.EndDraw();

            // 2. CPU Read 用の readableBitmap にコピー
            readableBitmap.CopyFromBitmap(renderTargetBitmap);

            // 3. ピクセルデータをマップして CPU バッファに読み込み
            var map = readableBitmap.Map(MapOptions.Read);
            int stride = map.Pitch;
            int dataSize = stride * height;

            byte[] srcBuf = ArrayPool<byte>.Shared.Rent(dataSize);
            byte[] dstBuf = ArrayPool<byte>.Shared.Rent(dataSize);
            try
            {
                unsafe
                {
                    new ReadOnlySpan<byte>((void*)map.Bits, dataSize).CopyTo(srcBuf);
                }
                readableBitmap.Unmap();

                // 4. 高速ピクセルソート処理の実行
                PixelSorter.SortPixels(
                    srcBuf,
                    dstBuf,
                    width,
                    height,
                    stride,
                    mode,
                    angle,
                    criterion,
                    threshMin,
                    threshMax,
                    invert,
                    includeTransparent,
                    randomInterval,
                    seed,
                    blend);

                // 5. 処理結果を outputBitmap に書き込み
                outputBitmap.CopyFromMemory(dstBuf, stride);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(srcBuf);
                ArrayPool<byte>.Shared.Return(dstBuf);
            }

            // 6. 出力位置を元の bounds.Left, bounds.Top に合わせて復元（スケール考慮）
            if (Math.Abs(scale - 1.0f) > 0.001f)
            {
                transformEffect.TransformMatrix = Matrix3x2.CreateScale(1.0f / scale) * Matrix3x2.CreateTranslation(bounds.Left, bounds.Top);
            }
            else
            {
                transformEffect.TransformMatrix = Matrix3x2.CreateTranslation(bounds.Left, bounds.Top);
            }

            // コンテキストのターゲットを元に戻す
            d2dContext.Target = prevTarget;
        }
        catch (Exception)
        {
            // メモリ不足等の万一の例外時も YMM4 をクラッシュさせず安全にフォールバック
            return drawDesc;
        }

        return drawDesc;
    }

    private bool EnsureBuffers(int width, int height)
    {
        if (currentWidth == width && currentHeight == height && outputBitmap != null && transformEffect != null && outputImage != null)
            return true;

        DisposeBuffers();

        try
        {
            currentWidth = width;
            currentHeight = height;

            var d2dContext = devices.DeviceContext;
            var pixelFormat = new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied);
            var size = new SizeI(width, height);

            // レンダーターゲット用
            var rtProps = new BitmapProperties1
            {
                PixelFormat = pixelFormat,
                DpiX = 96.0f,
                DpiY = 96.0f,
                BitmapOptions = BitmapOptions.Target
            };
            renderTargetBitmap = d2dContext.CreateBitmap(size, IntPtr.Zero, 0, rtProps);

            // CPU 読み込み用
            var readProps = new BitmapProperties1
            {
                PixelFormat = pixelFormat,
                DpiX = 96.0f,
                DpiY = 96.0f,
                BitmapOptions = BitmapOptions.CpuRead | BitmapOptions.CannotDraw
            };
            readableBitmap = d2dContext.CreateBitmap(size, IntPtr.Zero, 0, readProps);

            // 出力・描画用
            var outProps = new BitmapProperties1
            {
                PixelFormat = pixelFormat,
                DpiX = 96.0f,
                DpiY = 96.0f,
                BitmapOptions = BitmapOptions.None
            };
            outputBitmap = d2dContext.CreateBitmap(size, IntPtr.Zero, 0, outProps);

            // 元の位置へ復元する AffineTransform2D エフェクト
            transformEffect = new AffineTransform2D(d2dContext);
            transformEffect.SetInput(0, outputBitmap, true);

            // Output COM ラッパーを一度だけ取得してキャッシュ
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
        outputImage?.Dispose();
        outputImage = null;

        transformEffect?.Dispose();
        transformEffect = null;

        renderTargetBitmap?.Dispose();
        renderTargetBitmap = null;

        readableBitmap?.Dispose();
        readableBitmap = null;

        outputBitmap?.Dispose();
        outputBitmap = null;

        currentWidth = 0;
        currentHeight = 0;
    }

    public void Dispose()
    {
        if (!disposedValue)
        {
            DisposeBuffers();
            disposedValue = true;
        }
        GC.SuppressFinalize(this);
    }
}
