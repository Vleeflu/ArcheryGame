using System.Numerics;
using Archery.Gameplay;

namespace Archery.Scenes;

/// <summary>
/// Chase camera for a released arrow. At release it is at the archer's eye; it then slides along the line from the
/// eye to the arrow's tip, closing in to just behind (and slightly above) the arrow while turning to look along the
/// arrow's heading, and follows it downrange. It stops short of the target on the arrow's line, so the arrow flies on
/// dead ahead into the target. The camera always looks along +Z, so the horizon stays put.
/// </summary>
public static class ArrowCam
{
    /// <summary>Metres behind the arrow's tip, along its heading, once the camera has caught up.</summary>
    private const float FollowDistance = 1.3f;

    /// <summary>Metres above the arrow's line, so the arrow is seen from above, pointing up the screen at the target.</summary>
    private const float Height = 0.08f;

    /// <summary>Flight seconds over which the camera closes in from the eye to the follow distance.</summary>
    private const float CatchUpSeconds = 0.25f;

    /// <summary>Camera position <paramref name="time"/> flight-seconds after release; never past <paramref name="stopZ"/>.</summary>
    public static Vector3 Position(ArrowFlight flight, float time, float stopZ)
    {
        var (tip, heading) = flight.At(time);
        float b = Blend(time);

        // Staying on the eye→tip line keeps the tip at a steady spot on screen (no sideways swoop) while the
        // distance to it shrinks smoothly, so the arrow only ever grows (no pop).
        float distance = float.Lerp(flight.At(0f).Tip.Length(), FollowDistance, b);
        var view = Vector3.Normalize(Vector3.Lerp(Vector3.Normalize(tip), heading, b));
        var camera = tip - view * distance + new Vector3(0f, Height * b, 0f);
        if (camera.Z <= stopZ)
        {
            return camera;
        }

        // Stopped: hold depth, stay on the arrow's line so it (and the target it is heading for) remain dead ahead.
        float along = (tip.Z - stopZ) / heading.Z;
        return new Vector3(tip.X - heading.X * along, tip.Y - heading.Y * along + Height, stopZ);
    }

    /// <summary>0 at release, 1 once the camera has caught up with the arrow (smoothstep). Also drives the bow leaving the view.</summary>
    public static float Blend(float time)
    {
        float t = Math.Clamp(time / CatchUpSeconds, 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
