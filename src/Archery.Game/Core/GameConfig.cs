namespace Archery.Core;

public sealed record GameConfig
{
    public string Title { get; init; } = "Archery";
    /// <summary>Window size as a multiple of <see cref="PixelScreen"/> (4 → 1280x720).</summary>
    public int WindowScale { get; init; } = 4;
    public int TargetFps { get; init; } = 60;
    public bool ShowFps { get; init; } = true;
    public string AssetRoot { get; init; } = Path.Combine(AppContext.BaseDirectory, "assets");
}
