using System.Numerics;

namespace Archery.Assets;

/// <summary>
/// Pixel coordinates of attachment points inside each sprite image (origin top-left, whole pixels).
/// If an image is redrawn, these must be updated to match it.
/// </summary>
public static class SpriteAnchors
{
    /// <summary>target_face.png (41x41).</summary>
    public static class TargetFace
    {
        public static readonly Vector2 Center = new(20, 20);
        public const int Radius = 20;
    }

    /// <summary>target_stand.png (60x76): straw boss on an A-frame.</summary>
    public static class TargetStand
    {
        public static readonly Vector2 FaceCenter = new(30, 28);
    }

    /// <summary>
    /// bow.png (132x132): recurve tilted 45° (lower-left to upper-right) so it is perpendicular to the 45° arrow.
    /// Back of the bow faces up-left (toward the target), string side faces down-right. The string is not in the image.
    /// </summary>
    public static class Bow
    {
        /// <summary>Pixel the arrow shaft passes over when resting on the arrow rest.</summary>
        public static readonly Vector2 Shelf = new(63, 57);
        /// <summary>Where the arrow's nock sits on the braced string (15 steps from <see cref="Shelf"/> along +x+y).</summary>
        public static readonly Vector2 Nock = new(78, 72);
        public static readonly Vector2 TopTip = new(123, 22);
        public static readonly Vector2 TopContact = new(120, 30);
        public static readonly Vector2 BottomContact = new(30, 120);
        public static readonly Vector2 BottomTip = new(22, 123);
    }

    /// <summary>arrow.png (54x54): drawn at 45°, tip up-left. Moves only along that diagonal.</summary>
    public static class Arrow
    {
        public static readonly Vector2 Nock = new(48, 48);
        public static readonly Vector2 Tip = new(4, 4);
        /// <summary>Unit step from nock toward tip, one pixel on each axis.</summary>
        public static readonly Vector2 Direction = new(-1, -1);
    }
}
