namespace Archery.Assets;

/// <summary>Image files under <c>assets/</c>. PNG only: the bundled raylib build has no JPG decoder.</summary>
public static class AssetIds
{
    public const string Sky = "textures/sky.png";
    public const string Hills = "textures/hills.png";
    public const string Ground = "textures/ground.png";
    public const string TargetFace = "textures/target_face.png";
    public const string TargetStand = "textures/target_stand.png";
    public const string Bow = "textures/bow.png";
    public const string BowAim = "textures/bow_aim.png";
    public const string Arrow = "textures/arrow.png";
    public const string Crosshair = "textures/crosshair.png";

    public static readonly string[] AllTextures =
        [Sky, Hills, Ground, TargetFace, TargetStand, Bow, BowAim, Arrow, Crosshair];
}
