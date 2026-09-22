Texture2D InputTexture : register(t0);
SamplerState InputSampler : register(s0);

Texture2D NearBlurTexture : register(t1);
SamplerState NearBlurSampler : register(s1);

Texture2D MidBlurTexture : register(t2);
SamplerState MidBlurSampler : register(s2);

Texture2D FarBlurTexture : register(t3);
SamplerState FarBlurSampler : register(s3);

cbuffer Constants : register(b0)
{
    float strength  : packoffset(c0.x);
    float diffuse   : packoffset(c0.y);
    float ambient   : packoffset(c0.z);
    float occlusion : packoffset(c0.w);

    float nearW     : packoffset(c1.x);
    float midW      : packoffset(c1.y);
    float farW      : packoffset(c1.z);
    float pad0      : packoffset(c1.w);
};

float4 SampleTex(Texture2D tex, SamplerState smp, float2 uv)
{
    if (uv.x < 0.0f || uv.x > 1.0f || uv.y < 0.0f || uv.y > 1.0f)
        return float4(0.0f, 0.0f, 0.0f, 0.0f);
    return tex.SampleLevel(smp, uv, 0);
}

float4 main(
    float4 pos      : SV_POSITION,
    float4 posScene : SCENE_POSITION,
    float4 uv0      : TEXCOORD0,
    float4 uv1      : TEXCOORD1,
    float4 uv2      : TEXCOORD2,
    float4 uv3      : TEXCOORD3
) : SV_TARGET
{
    float4 source = SampleTex(InputTexture, InputSampler, uv0.xy);
    float4 nearSample = SampleTex(NearBlurTexture, NearBlurSampler, uv1.xy);
    float4 midSample = SampleTex(MidBlurTexture, MidBlurSampler, uv2.xy);
    float4 farSample = SampleTex(FarBlurTexture, FarBlurSampler, uv3.xy);

    // Multi-Scale Light Pyramid Blending (本家の逆二乗・逆距離減衰カーブに一致)
    float3 light = (nearSample.rgb * nearW + midSample.rgb * midW + farSample.rgb * farW) * strength;

    // Occlusion Handling (不透明物体の内部での透過・自己遮蔽を制御しつつ、空気中・背景へは光を遮らない)
    if (source.a > 1e-3f)
    {
        float occFactor = saturate(1.0f - occlusion * 0.25f);
        light *= occFactor;
    }

    // Surface Diffuse Illumination (本家と完全一致)
    float3 surface = float3(0.0f, 0.0f, 0.0f);
    if (source.a > 1e-3f)
    {
        float3 albedo = source.rgb / source.a;
        surface = light * albedo * diffuse * source.a;
    }

    // Air Glow (大気・空気中の光: 本家と完全一致)
    float3 airGlow = light * (1.0f - diffuse);
    float glowAlpha = saturate(max(airGlow.r, max(airGlow.g, airGlow.b)));

    // Final Blend with Alpha (本家と完全一致)
    float alpha = saturate(source.a + glowAlpha * (1.0f - source.a));
    float3 rgb = min(saturate(source.rgb * ambient + surface + airGlow), alpha);

    return float4(rgb, alpha);
}

