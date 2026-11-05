using YukkuriMovieMaker.Plugin;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Plugin.Update;
using YukkuriMovieMaker.Project;

namespace VoronoiNoiseLite;

/// <summary>
/// ボロノイノイズ 図形プラグイン（軽量版）。
/// YMM4 の「図形」として配置し、ボロノイノイズ画像を直接生成します。
/// </summary>
public sealed class VoronoiNoiseLiteShapePlugin : IShapePlugin
{
    // ── IPlugin ───────────────────────────────────────────────────────────
    public string               Name    => "ボロノイノイズ (軽量版・図形)";
    public PluginDetailsAttribute Details => new() { AuthorName = "VoronoiNoiseLite" };
    public IPluginUpdater?      Updater => null;

    // ── IShapePlugin ──────────────────────────────────────────────────────
    public bool   IsExoShapeSupported => false;
    public bool   IsExoMaskSupported  => false;
    public string DefaultGroupName    => "ノイズ";
    public int    DefaultOrder        => 100;

    public IShapeParameter CreateShapeParameter(SharedDataStore? sharedData)
        => new VoronoiNoiseLiteShapeParameter();
}
