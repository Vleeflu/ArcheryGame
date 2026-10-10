namespace Archery.Core;

public sealed class SceneManager
{
    private IScene? _current;

    public void Switch(IScene next)
    {
        _current?.Exit();
        _current = next;
        _current.Enter();
    }

    public void Update(float dt) => _current?.Update(dt);

    public void DrawFinePixels() => _current?.DrawFinePixels();

    public void DrawPixelArt() => _current?.DrawPixelArt();

    public void Shutdown()
    {
        _current?.Exit();
        _current = null;
    }
}
