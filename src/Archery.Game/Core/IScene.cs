namespace Archery.Core;

/// <summary>One screen of the game (range, menu, results, ...).</summary>
public interface IScene
{
    void Enter();
    void Update(float dt);
    void Draw();
    void Exit();
}
