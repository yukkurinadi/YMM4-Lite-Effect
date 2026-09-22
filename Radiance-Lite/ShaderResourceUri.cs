namespace RadianceLite;

internal static class ShaderResourceUri
{
    public static Uri Get(string shaderName) => new($"pack://application:,,,/RadianceLite;component/Shaders/{shaderName}.cso", UriKind.Absolute);
}
