namespace Archery.Core;

public sealed record GameConfig
{
    public string Title { get; init; } = "Archery";
    public int ScreenWidth { get; init; } = 1280;
    public int ScreenHeight { get; init; } = 720;
    public int TargetFps { get; init; } = 60;
    public bool ShowFps { get; init; } = true;
    public string AssetRoot { get; init; } = Path.Combine(AppContext.BaseDirectory, "assets");
}
