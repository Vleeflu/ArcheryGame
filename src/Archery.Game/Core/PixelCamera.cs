using System.Numerics;

namespace Archery.Core;

/// <summary>
/// Pinhole camera for the first-person view, always looking along +Z (+X right, +Y up, metres).
/// <paramref name="Center"/> is the screen pixel straight ahead (on the horizon row of the backdrop);
/// <paramref name="Focal"/> is the focal length in screen pixels; <paramref name="Position"/> is where the camera is.
/// </summary>
public readonly record struct PixelCamera(Vector2 Center, float Focal, Vector3 Position)
{
    /// <summary>Points closer than this in front of the camera are not drawn.</summary>
    public const float NearPlane = 0.05f;

    public Vector2 Project(Vector3 p)
    {
        var r = p - Position;
        return new(Center.X + Focal * r.X / r.Z, Center.Y - Focal * r.Y / r.Z);
    }

    /// <summary>The world point <paramref name="depth"/> metres in front of the camera that projects onto <paramref name="screen"/>.</summary>
    public Vector3 Unproject(Vector2 screen, float depth) =>
        Position + new Vector3((screen.X - Center.X) * depth / Focal, (Center.Y - screen.Y) * depth / Focal, depth);

    /// <summary>Distance in front of the camera (along its view direction) of world point <paramref name="p"/>.</summary>
    public float DepthOf(Vector3 p) => p.Z - Position.Z;
}
