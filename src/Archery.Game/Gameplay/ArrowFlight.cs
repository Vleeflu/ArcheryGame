using System.Numerics;

namespace Archery.Gameplay;

/// <summary>
/// Ballistic flight of one arrow under gravity, solved so the tip lands exactly on the aim point after
/// <see cref="Duration"/> seconds. World units are metres, +Y up.
/// </summary>
public sealed class ArrowFlight
{
    public static readonly Vector3 Gravity = new(0f, -9.81f, 0f);

    /// <summary>Seconds over which the arrow turns from its on-bow orientation to its flight heading.</summary>
    private const float SettleSeconds = 0.06f;

    private readonly Vector3 _onBowDirection;
    private readonly Vector3 _tipStart;
    private readonly Vector3 _launchVelocity;

    public ArrowFlight(Vector3 nock, Vector3 aimPoint, float length, float duration)
    {
        Length = length;
        Duration = duration;
        _onBowDirection = Vector3.Normalize(aimPoint - nock);
        _tipStart = nock + _onBowDirection * length;
        // p(T) = p0 + v0*T + g*T²/2 = aim  →  v0 = (aim - p0 - g*T²/2) / T
        _launchVelocity = (aimPoint - _tipStart - 0.5f * Gravity * duration * duration) / duration;
    }

    public float Length { get; }
    public float Duration { get; }

    /// <summary>Tip position and pointing direction <paramref name="time"/> seconds after release (clamped to landing).</summary>
    public (Vector3 Tip, Vector3 Direction) At(float time)
    {
        float t = Math.Clamp(time, 0f, Duration);
        var tip = _tipStart + _launchVelocity * t + 0.5f * Gravity * t * t;
        var heading = Vector3.Normalize(_launchVelocity + Gravity * t);
        // The arrow leaves the bow pointing at the aim point; ease into the true heading so release doesn't snap.
        var direction = Vector3.Normalize(Vector3.Lerp(_onBowDirection, heading, Math.Clamp(t / SettleSeconds, 0f, 1f)));
        return (tip, direction);
    }
}
