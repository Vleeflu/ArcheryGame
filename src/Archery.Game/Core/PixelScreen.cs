namespace Archery.Core;

/// <summary>
/// Native resolution the game is drawn at. Each frame is rendered to a texture this size and scaled up by a
/// whole number to fill the window, so every art pixel stays a crisp square block.
/// </summary>
public static class PixelScreen
{
    public const int Width = 320;
    public const int Height = 180;

    /// <summary>
    /// The back layer (backdrop and target) is rendered at this many times the native resolution: still pixel art, but
    /// with smaller pixels, so the target's rings stay readable. Coordinates are native pixels in both layers.
    /// </summary>
    public const int FineScale = 2;
}
