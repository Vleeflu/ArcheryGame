namespace Archery.Core;

/// <summary>One screen of the game (range, menu, results, ...).</summary>
public interface IScene
{
    void Enter();
    void Update(float dt);

    /// <summary>
    /// Back layer, drawn in native <see cref="PixelScreen"/> coordinates into a frame <see cref="PixelScreen.FineScale"/>×
    /// the native resolution: pixel art with smaller pixels. Fractional positions and sizes land on that finer grid.
    /// </summary>
    void DrawFinePixels();

    /// <summary>Front layer, drawn into the 320×180 pixel frame (transparent where nothing is drawn) and scaled up on top.</summary>
    void DrawPixelArt();

    void Exit();
}
