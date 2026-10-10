using System.Numerics;

namespace Archery.Assets;

/// <summary>
/// Attachment points inside each image (origin top-left). Sprites drawn 1:1 use whole pixel indices; the scaled
/// target images use continuous coordinates (pixel edges at whole numbers). If an image is redrawn, update these.
/// </summary>
public static class SpriteAnchors
{
    /// <summary>
    /// target_face.png (241x241): 122 cm ten-ring face. Rings are tenths of <see cref="Radius"/> (10 at the centre,
    /// 1 at the edge), plus the inner X ring at half the 10 ring. Drawn 1:1 into the fine layer at full zoom.
    /// </summary>
    public static class TargetFace
    {
        public static readonly Vector2 Center = new(120.5f, 120.5f);
        /// <summary>Pixels from <see cref="Center"/> to the outer edge of the 1 ring (0.61 m).</summary>
        public const float Radius = 120f;
    }

    /// <summary>target_stand.png (369x484): coiled straw boss on a wooden A-frame, same scale as the face.</summary>
    public static class TargetStand
    {
        public static readonly Vector2 FaceCenter = new(184.5f, 166.5f);
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

    /// <summary>
    /// bow_aim.png (64x260): the bow raised in front of the face at full draw, seen from behind. The riser stands
    /// upright with the limbs running off the top and bottom of the screen; the arrow rest reaches left into the sight
    /// window. No string in the image.
    /// </summary>
    public static class BowAim
    {
        /// <summary>Tip of the arrow rest, where the arrow's point sits at full draw.</summary>
        public static readonly Vector2 Rest = new(24, 127);
        public static readonly Vector2 TopString = new(46, 0);
        public static readonly Vector2 BottomString = new(34, 259);
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
