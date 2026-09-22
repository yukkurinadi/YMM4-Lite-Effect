using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LiteEffect;

/// <summary>
/// 超高速ピクセルソート処理エンジン（ulong パックソート + マルチスレッド）
/// </summary>
public static class PixelSorter
{
    /// <summary>
    /// 32-bit BGRA ピクセルバッファに対してピクセルソートを実行します。
    /// </summary>
    public static unsafe void SortPixels(
        byte[] srcBuffer,
        byte[] dstBuffer,
        int width,
        int height,
        int stride,
        PixelSortMode mode,
        float angleDeg,
        PixelSortCriterion criterion,
        float thresholdMin,
        float thresholdMax,
        bool invertThreshold,
        bool includeTransparent,
        float randomInterval,
        int seed,
        float blendRate)
    {
        if (srcBuffer == null || dstBuffer == null || width <= 0 || height <= 0)
            return;

        int totalBytes = stride * height;
        Buffer.BlockCopy(srcBuffer, 0, dstBuffer, 0, Math.Min(srcBuffer.Length, dstBuffer.Length));

        if (blendRate <= 0.0001f)
            return;

        // 0~65535 の整数閾値に量子化
        float minF = Math.Clamp(Math.Min(thresholdMin, thresholdMax), 0f, 1f);
        float maxF = Math.Clamp(Math.Max(thresholdMin, thresholdMax), 0f, 1f);
        uint minThresh = (uint)(minF * 65535f);
        uint maxThresh = (uint)(maxF * 65535f);

        int pixelStride = stride / 4;

        fixed (byte* pSrcByte = srcBuffer)
        fixed (byte* pDstByte = dstBuffer)
        {
            uint* pSrc = (uint*)pSrcByte;
            uint* pDst = (uint*)pDstByte;

            switch (mode)
            {
                case PixelSortMode.LeftToRight:
                    SortHorizontal(pSrc, pDst, width, height, pixelStride, criterion, false, minThresh, maxThresh, invertThreshold, includeTransparent, randomInterval, seed, blendRate);
                    break;

                case PixelSortMode.RightToLeft:
                    SortHorizontal(pSrc, pDst, width, height, pixelStride, criterion, true, minThresh, maxThresh, invertThreshold, includeTransparent, randomInterval, seed, blendRate);
                    break;

                case PixelSortMode.TopToBottom:
                    SortVertical(pSrc, pDst, width, height, pixelStride, criterion, false, minThresh, maxThresh, invertThreshold, includeTransparent, randomInterval, seed, blendRate);
                    break;

                case PixelSortMode.BottomToTop:
                    SortVertical(pSrc, pDst, width, height, pixelStride, criterion, true, minThresh, maxThresh, invertThreshold, includeTransparent, randomInterval, seed, blendRate);
                    break;

                case PixelSortMode.Angle:
                    SortAngle(pSrc, pDst, width, height, pixelStride, angleDeg, criterion, minThresh, maxThresh, invertThreshold, includeTransparent, randomInterval, seed, blendRate);
                    break;
            }
        }
    }

    /// <summary>
    /// 水平方向のソート処理（reverseDirection: true で右→左）
    /// </summary>
    private static unsafe void SortHorizontal(
        uint* pSrc,
        uint* pDst,
        int width,
        int height,
        int stride,
        PixelSortCriterion criterion,
        bool reverseDirection,
        uint minThresh,
        uint maxThresh,
        bool invert,
        bool includeTransparent,
        float randomInterval,
        int seed,
        float blendRate)
    {
        Parallel.For(0, height, y =>
        {
            int rowOffset = y * stride;
            ulong[] itemPool = ArrayPool<ulong>.Shared.Rent(width);
            try
            {
                uint rowSeed = (uint)(seed ^ (y * 1103515245 + 12345));
                int x = 0;

                while (x < width)
                {
                    while (x < width)
                    {
                        uint c = pDst[rowOffset + x];
                        if (IsSortable(c, criterion, minThresh, maxThresh, invert, includeTransparent))
                            break;
                        x++;
                    }

                    int start = x;
                    int maxLen = width - start;
                    if (randomInterval > 0.001f)
                    {
                        rowSeed = (rowSeed * 1664525u + 1013904223u);
                        float r = (rowSeed & 0xFFFF) / 65535f;
                        int intervalLimit = Math.Max(2, (int)(width * (1f - randomInterval * 0.85f * r)));
                        if (intervalLimit < maxLen)
                            maxLen = intervalLimit;
                    }

                    int count = 0;
                    while (x < width && count < maxLen)
                    {
                        uint c = pDst[rowOffset + x];
                        if (!IsSortable(c, criterion, minThresh, maxThresh, invert, includeTransparent))
                            break;

                        uint val = CalculateSortKey(c, criterion);
                        itemPool[count] = ((ulong)val << 32) | c;
                        count++;
                        x++;
                    }

                    if (count > 1)
                    {
                        Array.Sort(itemPool, 0, count);

                        bool placeAscending = !reverseDirection;

                        for (int i = 0; i < count; i++)
                        {
                            int readIdx = placeAscending ? i : (count - 1 - i);
                            uint sortedColor = (uint)(itemPool[readIdx] & 0xFFFFFFFF);

                            if (blendRate < 0.999f)
                            {
                                uint origColor = pSrc[rowOffset + start + i];
                                pDst[rowOffset + start + i] = BlendColor(origColor, sortedColor, blendRate);
                            }
                            else
                            {
                                pDst[rowOffset + start + i] = sortedColor;
                            }
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<ulong>.Shared.Return(itemPool);
            }
        });
    }

    /// <summary>
    /// 垂直方向のソート処理（reverseDirection: true で下→上）
    /// </summary>
    private static unsafe void SortVertical(
        uint* pSrc,
        uint* pDst,
        int width,
        int height,
        int stride,
        PixelSortCriterion criterion,
        bool reverseDirection,
        uint minThresh,
        uint maxThresh,
        bool invert,
        bool includeTransparent,
        float randomInterval,
        int seed,
        float blendRate)
    {
        Parallel.For(0, width, x =>
        {
            ulong[] itemPool = ArrayPool<ulong>.Shared.Rent(height);
            try
            {
                uint colSeed = (uint)(seed ^ (x * 1103515245 + 54321));
                int y = 0;

                while (y < height)
                {
                    while (y < height)
                    {
                        uint c = pDst[y * stride + x];
                        if (IsSortable(c, criterion, minThresh, maxThresh, invert, includeTransparent))
                            break;
                        y++;
                    }

                    int start = y;
                    int maxLen = height - start;
                    if (randomInterval > 0.001f)
                    {
                        colSeed = (colSeed * 1664525u + 1013904223u);
                        float r = (colSeed & 0xFFFF) / 65535f;
                        int intervalLimit = Math.Max(2, (int)(height * (1f - randomInterval * 0.85f * r)));
                        if (intervalLimit < maxLen)
                            maxLen = intervalLimit;
                    }

                    int count = 0;
                    while (y < height && count < maxLen)
                    {
                        uint c = pDst[y * stride + x];
                        if (!IsSortable(c, criterion, minThresh, maxThresh, invert, includeTransparent))
                            break;

                        uint val = CalculateSortKey(c, criterion);
                        itemPool[count] = ((ulong)val << 32) | c;
                        count++;
                        y++;
                    }

                    if (count > 1)
                    {
                        Array.Sort(itemPool, 0, count);

                        bool placeAscending = !reverseDirection;

                        for (int i = 0; i < count; i++)
                        {
                            int readIdx = placeAscending ? i : (count - 1 - i);
                            uint sortedColor = (uint)(itemPool[readIdx] & 0xFFFFFFFF);

                            int pos = (start + i) * stride + x;
                            if (blendRate < 0.999f)
                            {
                                uint origColor = pSrc[pos];
                                pDst[pos] = BlendColor(origColor, sortedColor, blendRate);
                            }
                            else
                            {
                                pDst[pos] = sortedColor;
                            }
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<ulong>.Shared.Return(itemPool);
            }
        });
    }

    /// <summary>
    /// 任意角度方向のソート処理
    /// </summary>
    private static unsafe void SortAngle(
        uint* pSrc,
        uint* pDst,
        int width,
        int height,
        int stride,
        float angleDeg,
        PixelSortCriterion criterion,
        uint minThresh,
        uint maxThresh,
        bool invert,
        bool includeTransparent,
        float randomInterval,
        int seed,
        float blendRate)
    {
        float rad = (float)(angleDeg * Math.PI / 180.0);
        float cos = (float)Math.Cos(rad);
        float sin = (float)Math.Sin(rad);

        if (Math.Abs(sin) < 0.001f)
        {
            if (cos > 0)
                SortHorizontal(pSrc, pDst, width, height, stride, criterion, false, minThresh, maxThresh, invert, includeTransparent, randomInterval, seed, blendRate);
            else
                SortHorizontal(pSrc, pDst, width, height, stride, criterion, true, minThresh, maxThresh, invert, includeTransparent, randomInterval, seed, blendRate);
            return;
        }
        if (Math.Abs(cos) < 0.001f)
        {
            if (sin > 0)
                SortVertical(pSrc, pDst, width, height, stride, criterion, false, minThresh, maxThresh, invert, includeTransparent, randomInterval, seed, blendRate);
            else
                SortVertical(pSrc, pDst, width, height, stride, criterion, true, minThresh, maxThresh, invert, includeTransparent, randomInterval, seed, blendRate);
            return;
        }

        int maxDim = (int)Math.Ceiling(Math.Sqrt(width * width + height * height));
        int cx = width / 2;
        int cy = height / 2;

        Parallel.For(-maxDim, maxDim, l =>
        {
            ulong[] itemPool = ArrayPool<ulong>.Shared.Rent(maxDim * 2);
            int[] posPool = ArrayPool<int>.Shared.Rent(maxDim * 2);
            try
            {
                uint lineSeed = (uint)(seed ^ (l * 1103515245 + 98765));
                int t = -maxDim;

                while (t < maxDim)
                {
                    while (t < maxDim)
                    {
                        int px = cx + (int)Math.Round(t * cos - l * sin);
                        int py = cy + (int)Math.Round(t * sin + l * cos);

                        if (px >= 0 && px < width && py >= 0 && py < height)
                        {
                            int pos = py * stride + px;
                            if (IsSortable(pDst[pos], criterion, minThresh, maxThresh, invert, includeTransparent))
                                break;
                        }
                        t++;
                    }

                    int count = 0;
                    int maxLen = maxDim * 2;
                    if (randomInterval > 0.001f)
                    {
                        lineSeed = (lineSeed * 1664525u + 1013904223u);
                        float r = (lineSeed & 0xFFFF) / 65535f;
                        int intervalLimit = Math.Max(2, (int)(maxDim * (1f - randomInterval * 0.85f * r)));
                        if (intervalLimit < maxLen)
                            maxLen = intervalLimit;
                    }

                    while (t < maxDim && count < maxLen)
                    {
                        int px = cx + (int)Math.Round(t * cos - l * sin);
                        int py = cy + (int)Math.Round(t * sin + l * cos);

                        if (px >= 0 && px < width && py >= 0 && py < height)
                        {
                            int pos = py * stride + px;
                            uint c = pDst[pos];
                            if (IsSortable(c, criterion, minThresh, maxThresh, invert, includeTransparent))
                            {
                                uint val = CalculateSortKey(c, criterion);
                                itemPool[count] = ((ulong)val << 32) | c;
                                posPool[count] = pos;
                                count++;
                                t++;
                                continue;
                            }
                        }
                        break;
                    }

                    if (count > 1)
                    {
                        Array.Sort(itemPool, 0, count);

                        for (int i = 0; i < count; i++)
                        {
                            uint sortedColor = (uint)(itemPool[i] & 0xFFFFFFFF);
                            int pos = posPool[i];

                            if (blendRate < 0.999f)
                            {
                                uint origColor = pSrc[pos];
                                pDst[pos] = BlendColor(origColor, sortedColor, blendRate);
                            }
                            else
                            {
                                pDst[pos] = sortedColor;
                            }
                        }
                    }
                    t++;
                }
            }
            finally
            {
                ArrayPool<ulong>.Shared.Return(itemPool);
                ArrayPool<int>.Shared.Return(posPool);
            }
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSortable(uint color, PixelSortCriterion criterion, uint minThresh, uint maxThresh, bool invert, bool includeTransparent)
    {
        uint a = (color >> 24) & 0xFF;
        if (a == 0)
        {
            if (!includeTransparent)
                return false;
            bool inRange = 0 >= minThresh && 0 <= maxThresh;
            return invert ? !inRange : inRange;
        }

        uint val = CalculateSortKey(color, criterion);
        bool inRangeColor = val >= minThresh && val <= maxThresh;
        return invert ? !inRangeColor : inRangeColor;
    }

    /// <summary>
    /// ピクセルのソート用比較キー (0 ~ 65535) を整数演算で超高速算出
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint CalculateSortKey(uint color, PixelSortCriterion criterion)
    {
        uint a = (color >> 24) & 0xFF;
        if (a == 0)
            return 0; // 透明ピクセルは最小値 (0)

        uint b = color & 0xFF;
        uint g = (color >> 8) & 0xFF;
        uint r = (color >> 16) & 0xFF;

        uint val;
        switch (criterion)
        {
            case PixelSortCriterion.Luminance:
                // 54 * R + 183 * G + 19 * B ≈ ITU-R BT.709 輝度 (0 ~ 65535)
                val = (r * 13933u + g * 46871u + b * 4731u) >> 8;
                break;

            case PixelSortCriterion.Hue:
                val = GetHueKey(r, g, b);
                break;

            case PixelSortCriterion.Saturation:
                val = GetSaturationKey(r, g, b);
                break;

            case PixelSortCriterion.Red:
                val = r * 257u;
                break;

            case PixelSortCriterion.Green:
                val = g * 257u;
                break;

            case PixelSortCriterion.Blue:
                val = b * 257u;
                break;

            default:
                val = (r * 13933u + g * 46871u + b * 4731u) >> 8;
                break;
        }

        // 不透明ピクセルは透明 (0) より必ず大きくなるよう 1 以上にする
        return val == 0 ? 1u : val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetHueKey(uint r, uint g, uint b)
    {
        uint max = Math.Max(r, Math.Max(g, b));
        uint min = Math.Min(r, Math.Min(g, b));
        uint delta = max - min;

        if (delta == 0)
            return 0;

        int hue60;
        if (max == r)
        {
            hue60 = (int)(((int)g - (int)b) * 60) / (int)delta;
            if (hue60 < 0) hue60 += 360;
        }
        else if (max == g)
        {
            hue60 = (int)(((int)b - (int)r) * 60) / (int)delta + 120;
        }
        else
        {
            hue60 = (int)(((int)r - (int)g) * 60) / (int)delta + 240;
        }

        return (uint)((hue60 * 65535) / 360);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetSaturationKey(uint r, uint g, uint b)
    {
        uint max = Math.Max(r, Math.Max(g, b));
        uint min = Math.Min(r, Math.Min(g, b));
        uint delta = max - min;

        if (max == 0)
            return 0;

        return (delta * 65535u) / max;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint BlendColor(uint orig, uint sorted, float rate)
    {
        uint b0 = orig & 0xFF;
        uint g0 = (orig >> 8) & 0xFF;
        uint r0 = (orig >> 16) & 0xFF;
        uint a0 = (orig >> 24) & 0xFF;

        uint b1 = sorted & 0xFF;
        uint g1 = (sorted >> 8) & 0xFF;
        uint r1 = (sorted >> 16) & 0xFF;
        uint a1 = (sorted >> 24) & 0xFF;

        int irate = (int)(rate * 256.0f + 0.5f);
        int iinv = 256 - irate;

        uint b = (uint)((b0 * iinv + b1 * irate) >> 8);
        uint g = (uint)((g0 * iinv + g1 * irate) >> 8);
        uint r = (uint)((r0 * iinv + r1 * irate) >> 8);
        uint a = (uint)((a0 * iinv + a1 * irate) >> 8);

        return b | (g << 8) | (r << 16) | (a << 24);
    }
}
