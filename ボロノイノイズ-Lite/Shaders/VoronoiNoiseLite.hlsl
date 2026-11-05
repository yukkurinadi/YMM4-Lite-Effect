Texture2D InputTexture : register(t0);
SamplerState InputSampler : register(s0);

cbuffer Constants : register(b0)
{
    float totalX;        // 0: (X + SpeedX * frame) % 123456.789
    float totalY;        // 1: (Y + SpeedY * frame) % 154321.0987
    float totalZ;        // 2: (Z + SpeedZ * frame) % 160213.8549
    float threshold;     // 3: しきい値 / 100.0
    
    float scaleX;        // 4: 100.0 / (ScaleX * Size / 100)
    float scaleY;        // 5: 100.0 / (ScaleY * Size / 100)
    float scaleZ;        // 6: 100.0 / (ScaleZ * Size / 100)
    float strength;      // 7: 強さ / 100.0 (半透明度 / ブレンド率)
    
    int   octaves;       // 8: 繰り返し層数 (1〜8)
    int   seed;          // 9: 乱数シード % 1234
    int   levels;        // 10: 階調数 (2〜256)
    float angle;         // 11: - (Angle / 180 * PI)
    
    float warpStrength;  // 12: 歪み強さ (px)
    float warpScale;     // 13: 歪みスケール / 100.0
    int   fractalMode;   // 14: 合成方式 (0=通常, 1=加算, 2=乗算, 3=スクリーン)
    int   distanceMode;  // 15: 距離方式 (0=ユークリッド, 1=マンハッタン, 2=チェビシェフ)
    
    float lacunarity;    // 16: 周波数倍率
    float gain;          // 17: 振幅減衰
    int   isColor;       // 18: 色 (0=モノクロ, 1=カラー)
    int   isAlpha;       // 19: 不透明度 (0=透過, 1=不透明)
};

// ── 高速・軽量整数ハッシュ (PCG風・超低負荷) ──────────────────────────
uint Hash21(uint2 p, uint s)
{
    p += s;
    p = p * uint2(1597334677u, 3812015801u);
    uint h = (p.x ^ p.y) * 1597334677u;
    h = h ^ (h >> 16);
    return h;
}

// ── セル内母点オフセット (0.0 〜 1.0) ────────────────────────────────
float2 CellPoint(int2 cell, int s, float z)
{
    uint2 p = (uint2)cell;
    uint h1 = Hash21(p, (uint)s);
    uint h2 = Hash21(p + 1u, (uint)s + 1013904223u);
    
    float2 pt = float2(h1 & 0xFFFF, h2 & 0xFFFF) * (1.0f / 65535.0f);
    if (abs(z) > 1e-4f)
    {
        pt = frac(pt + float2(sin(z * 0.7f + pt.y * 6.28318f), cos(z * 0.7f + pt.x * 6.28318f)) * 0.25f);
    }
    return pt;
}

// ── セル固有値（モノクロ時は均一分布スカラー、カラー時は独立RGB） ─────
float3 CellValue(int2 cell, int s, int isCol)
{
    uint2 p = (uint2)cell;
    uint h0 = Hash21(p, (uint)s);
    float v0 = (h0 & 0xFFFFFF) * (1.0f / 16777215.0f);
    
    if (isCol == 0)
    {
        return float3(v0, v0, v0);
    }
    
    uint h1 = Hash21(p + 137u, (uint)s + 2246822519u);
    uint h2 = Hash21(p + 389u, (uint)s + 3266489917u);
    float v1 = (h1 & 0xFFFFFF) * (1.0f / 16777215.0f);
    float v2 = (h2 & 0xFFFFFF) * (1.0f / 16777215.0f);
    return float3(v0, v1, v2);
}

// ── 距離計算（ユークリッド / マンハッタン / チェビシェフ） ───────────
float CalcDist(float2 diff, int mode)
{
    if (mode == 1) // マンハッタン距離
        return abs(diff.x) + abs(diff.y);
    if (mode == 2) // チェビシェフ距離
        return max(abs(diff.x), abs(diff.y));
    // ユークリッド距離 (二乗距離比較で超高速)
    return dot(diff, diff);
}

// ── 3x3 Jittered Grid による最寄母点探索（セル値固定） ────────────────
float3 VoronoiCellLayer(float2 uv, int s, float z, int distMode, int isCol)
{
    int2 baseCell = (int2)floor(uv);
    float2 fracPos = frac(uv);
    
    float bestDist = 1e9f;
    int2 bestCell = baseCell;
    
    [unroll]
    for (int dy = -1; dy <= 1; dy++)
    {
        [unroll]
        for (int dx = -1; dx <= 1; dx++)
        {
            int2 cell = baseCell + int2(dx, dy);
            float2 pt = CellPoint(cell, s, z);
            float2 diff = (float2(dx, dy) + pt) - fracPos;
            
            float d = CalcDist(diff, distMode);
            
            if (d < bestDist)
            {
                bestDist = d;
                bestCell = cell;
            }
        }
    }
    
    return CellValue(bestCell, s, isCol);
}

// ── ピクセルシェーダーメイン ──────────────────────────────────────────
float4 main(
    float4 pos      : SV_POSITION,
    float4 posScene : SCENE_POSITION,
    float4 uv0      : TEXCOORD0
) : SV_TARGET
{
    // 1. 移動座標 (YMM4本家準拠: シーン座標からのオフセット)
    float2 p = posScene.xy - float2(totalX, totalY);
    
    // 2. 回転 (YMM4本家準拠)
    if (abs(angle) > 1e-4f)
    {
        float cosA = cos(angle);
        float sinA = sin(angle);
        p = float2(p.x * cosA - p.y * sinA, p.x * sinA + p.y * cosA);
    }
    
    // 3. 歪み (Warping: YMM4本家準拠)
    if (warpStrength > 0.01f)
    {
        float ws = max(warpScale, 0.001f);
        float2 wp = p * ws * 0.01f;
        float wx = sin(wp.x * 2.3f + wp.y * 1.7f);
        float wy = cos(wp.x * 1.9f + wp.y * 2.7f);
        p += float2(wx, wy) * warpStrength;
    }
    
    // 4. スケール (YMM4本家準拠: scaleX = 100.0 / num2 なので乗算)
    float2 st = p * float2(scaleX, scaleY) * 0.01f;
    
    // 5. フラクタル多重合成 (出力モードはセル値固定)
    float3 total = float3(0, 0, 0);
    float maxAmp = 0.0f;
    float amp = 1.0f;
    float freq = 1.0f;
    
    int oct = clamp(octaves, 1, 8);
    
    for (int i = 0; i < oct; i++)
    {
        float3 c = VoronoiCellLayer(st * freq, seed + i * 31, totalZ + (float)i * 0.35f, distanceMode, isColor);
        
        if (fractalMode == 0) // 通常 (加重平均)
        {
            total += c * amp;
            maxAmp += amp;
        }
        else if (fractalMode == 1) // 加算
        {
            total += c * amp;
            maxAmp = 1.0f;
        }
        else if (fractalMode == 2) // 乗算
        {
            if (i == 0) total = c;
            else total = lerp(total, total * c, amp);
            maxAmp = 1.0f;
        }
        else // スクリーン
        {
            if (i == 0) total = c;
            else total = lerp(total, 1.0f - (1.0f - total) * (1.0f - c), amp);
            maxAmp = 1.0f;
        }
        
        freq *= max(lacunarity, 1.0f);
        amp *= gain;
    }
    
    float3 rawNoise = (fractalMode == 0) ? (total / max(maxAmp, 1e-4f)) : saturate(total);
    
    // 6. 濃淡: しきい値 (YMM4本家準拠)
    if (threshold > 1e-4f)
    {
        rawNoise = saturate((rawNoise - threshold) / max(1.0f - threshold, 1e-4f));
    }
    
    // 7. 濃淡: 階調数 (YMM4本家準拠: levels < 256)
    if (levels < 256 && levels >= 2)
    {
        float lev = (float)(levels - 1);
        rawNoise = round(rawNoise * lev) / lev;
    }
    
    // 8. 強さ (YMM4本家準拠: 半透明度 / ブレンド率)
    float str = saturate(strength);
    
    // 9. YMM4完全再現ブレンド係数の算出
    // YMM4のノイズ合成仕様:
    // - 色OFF / 不透明度OFF: RGBにモノクロノイズ適用 (Alphaは不変)
    // - 色ON: RGBにカラーノイズ適用
    // - 不透明度ON: Alphaにノイズ適用 (元アイテムのRGBはそのまま保持され、ノイズ模様で半透明化)
    float3 rgbMult;
    float alphaMult;
    
    if (isColor == 0 && isAlpha == 0)
    {
        rgbMult = lerp(float3(1.0f, 1.0f, 1.0f), rawNoise, str);
        alphaMult = 1.0f;
    }
    else
    {
        if (isColor != 0)
        {
            rgbMult = lerp(float3(1.0f, 1.0f, 1.0f), rawNoise, str);
        }
        else
        {
            rgbMult = float3(1.0f, 1.0f, 1.0f);
        }
        
        if (isAlpha != 0)
        {
            float lum = (isColor != 0) ? dot(rawNoise, float3(0.299f, 0.587f, 0.114f)) : rawNoise.r;
            alphaMult = lerp(1.0f, lum, str);
        }
        else
        {
            alphaMult = 1.0f;
        }
    }
    
    // 10. 元テクスチャのサンプリング
    float4 src = InputTexture.SampleLevel(InputSampler, uv0.xy, 0);
    
    // Direct2D プリマルチプライド・アルファ準拠
    float4 outColor;
    outColor.rgb = src.rgb * rgbMult * alphaMult;
    outColor.a   = src.a   * alphaMult;
    return outColor;
}
