namespace Archery.Assets;

/// <summary>
/// Single source of truth for asset file names, relative to the <c>assets</c> root.
/// Linked into Archery.AssetGen so the generator writes exactly what the game loads.
/// </summary>
public static class AssetIds
{
    public const string Sky = "textures/sky.png";
    public const string Ground = "textures/ground.png";
    public const string Target = "textures/target.png";
    public const string Bow = "textures/bow.png";
    public const string Arrow = "textures/arrow.png";
    public const string Crosshair = "textures/crosshair.png";

    public static readonly string[] AllTextures = [Sky, Ground, Target, Bow, Arrow, Crosshair];
}
