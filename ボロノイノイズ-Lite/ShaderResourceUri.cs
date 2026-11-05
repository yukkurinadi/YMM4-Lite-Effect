namespace VoronoiNoiseLite;

internal static class ShaderResourceUri
{
    public static Uri Get(string shaderName) =>
        new($"pack://application:,,,/VoronoiNoiseLite;component/Shaders/{shaderName}.cso", UriKind.Absolute);
}
