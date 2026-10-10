using System.Numerics;

namespace Archery.Assets;

/// <summary>
/// Pixel coordinates of attachment points inside each sprite image (origin top-left, whole pixels).
/// If an image is redrawn, these must be updated to match it.
/// </summary>
public static class SpriteAnchors
{
    /// <summary>target_face.png (21x21): ten-ring face as seen from the shooting line (~30 m).</summary>
    public static class TargetFace
    {
        public static readonly Vector2 Center = new(10, 10);
        public const int Radius = 10;
    }

    /// <summary>target_stand.png (32x40): straw boss on an A-frame.</summary>
    public static class TargetStand
    {
        public static readonly Vector2 FaceCenter = new(16, 14);
    }

    /// <summary>
    /// bow.png (110x160): recurve standing nearly upright, leaning along a 1:2 slope (top tip up-right) so it is
    /// perpendicular to the arrow. Back of the bow faces the target, string side faces down-right. No string in the image.
    /// </summary>
    public static class Bow
    {
        /// <summary>Pixel the arrow shaft passes over when resting on the arrow rest.</summary>
        public static readonly Vector2 Shelf = new(50, 72);
        /// <summary>Where the arrow's nock sits on the braced string (10 arrow steps behind <see cref="Shelf"/>).</summary>
        public static readonly Vector2 Nock = new(70, 82);
        public static readonly Vector2 TopTip = new(96, 20);
        public static readonly Vector2 TopContact = new(96, 30);
        public static readonly Vector2 BottomContact = new(40, 142);
        public static readonly Vector2 BottomTip = new(32, 148);
    }

    /// <summary>arrow.png (64x38): drawn on a 2:1 slope, tip up-left. Moves only along that line.</summary>
    public static class Arrow
    {
        public static readonly Vector2 Nock = new(58, 32);
        public static readonly Vector2 Tip = new(8, 7);
        /// <summary>One pixel-exact step from nock toward tip: 2 px left, 1 px up.</summary>
        public static readonly Vector2 Direction = new(-2, -1);
    }
}
