using System.Numerics;
using Archery.Assets;
using Raylib_cs;

// Generates every texture listed in AssetIds and writes it as PNG into <repo>/assets.
// Run once (or after changing a generator); the game only ever loads the saved files.
//   dotnet run --project tools/Archery.AssetGen

Raylib.SetTraceLogLevel(TraceLogLevel.Warning);

var generators = new Dictionary<string, Func<Image>>
{
    [AssetIds.Sky] = Generators.Sky,
    [AssetIds.Ground] = Generators.Ground,
    [AssetIds.Target] = Generators.Target,
    [AssetIds.Bow] = Generators.Bow,
    [AssetIds.Arrow] = Generators.Arrow,
    [AssetIds.Crosshair] = Generators.Crosshair,
};

var missing = AssetIds.AllTextures.Where(id => !generators.ContainsKey(id)).ToArray();
if (missing.Length > 0)
{
    Console.Error.WriteLine($"No generator for: {string.Join(", ", missing)}");
    return 1;
}

var assetRoot = Path.Combine(FindRepoRoot(), "assets");

foreach (var id in AssetIds.AllTextures)
{
    var path = Path.Combine(assetRoot, id);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

    var image = generators[id]();
    bool ok = Raylib.ExportImage(image, path);
    Raylib.UnloadImage(image);

    if (!ok)
    {
        Console.Error.WriteLine($"Failed to write {path}");
        return 1;
    }

    Console.WriteLine($"wrote {id}");
}

return 0;

static string FindRepoRoot()
{
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Archery.slnx")))
        {
            return dir.FullName;
        }
    }

    throw new DirectoryNotFoundException("Could not locate Archery.slnx above the tool's output directory.");
}

static class Generators
{
    public static Image Sky()
    {
        return Raylib.GenImageGradientLinear(512, 256, 0, new Color(70, 130, 200, 255), new Color(195, 225, 245, 255));
    }

    public static Image Ground()
    {
        var image = Raylib.GenImagePerlinNoise(512, 256, 0, 0, 6f);
        Raylib.ImageColorTint(ref image, new Color(110, 170, 70, 255));
        return image;
    }

    /// <summary>Standard 10-ring archery face: white, black, blue, red, gold (outer to inner).</summary>
    public static Image Target()
    {
        const int size = 512;
        const int center = size / 2;
        const int outerRadius = 250;

        Color[] bands =
        [
            new(240, 240, 240, 255), // white
            new(30, 30, 30, 255),    // black
            new(40, 120, 210, 255),  // blue
            new(220, 50, 50, 255),   // red
            new(250, 210, 40, 255),  // gold
        ];

        var image = Raylib.GenImageColor(size, size, Color.Blank);
        for (int ring = 0; ring < 10; ring++)
        {
            int radius = outerRadius * (10 - ring) / 10;
            Raylib.ImageDrawCircle(ref image, center, center, radius, bands[ring / 2]);
        }

        for (int ring = 0; ring < 10; ring++)
        {
            int radius = outerRadius * (10 - ring) / 10;
            var line = ring / 2 == 1 ? new Color(200, 200, 200, 255) : new Color(20, 20, 20, 255);
            Raylib.ImageDrawCircleLines(ref image, center, center, radius, line);
        }

        // Inner "X" ring.
        Raylib.ImageDrawCircleLines(ref image, center, center, outerRadius / 20, new Color(20, 20, 20, 255));
        return image;
    }

    /// <summary>Recurve bow held vertically, seen from the archer's eye. String at the image's horizontal center.</summary>
    public static Image Bow()
    {
        const int width = 256;
        const int height = 768;
        const int segments = 48;
        const float stringX = width / 2f;
        const float tipY = 24f;
        const float bulge = 70f;
        const float recurveDepth = 18f;

        var limb = new Color(95, 60, 35, 255);
        var grip = new Color(45, 30, 20, 255);
        var bowString = new Color(235, 235, 220, 255);

        var image = Raylib.GenImageColor(width, height, Color.Blank);

        Vector2 PointAt(float t) // t in [-1, 1], top to bottom
        {
            float y = height / 2f + t * (height / 2f - tipY);
            float curve = bulge * (1f - t * t);
            float recurve = recurveDepth * (1f - MathF.Pow(MathF.Abs(t), 6f)); // tips flick back toward the archer
            return new Vector2(stringX - curve - recurve, y); // tips (t = ±1) land exactly on stringX
        }

        for (int i = 0; i < segments; i++)
        {
            float t0 = -1f + 2f * i / segments;
            float t1 = -1f + 2f * (i + 1) / segments;
            int thickness = (int)MathF.Round(8f + 10f * (1f - MathF.Abs((t0 + t1) / 2f)));
            Raylib.ImageDrawLineEx(ref image, PointAt(t0), PointAt(t1), thickness, limb);
        }

        var gripCenter = PointAt(0f);
        Raylib.ImageDrawRectangle(ref image, (int)gripCenter.X - 12, (int)gripCenter.Y - 45, 24, 90, grip);

        Raylib.ImageDrawLineEx(ref image, PointAt(-1f), PointAt(1f), 2, bowString);
        return image;
    }

    /// <summary>Arrow pointing up (away from the archer).</summary>
    public static Image Arrow()
    {
        const int width = 48;
        const int height = 512;
        const float cx = width / 2f;

        var shaft = new Color(160, 120, 70, 255);
        var head = new Color(150, 155, 160, 255);
        var fletch = new Color(210, 40, 40, 255);
        var nock = new Color(30, 30, 30, 255);

        var image = Raylib.GenImageColor(width, height, Color.Blank);

        Raylib.ImageDrawRectangle(ref image, (int)cx - 3, 50, 6, height - 62, shaft);

        // Triangles are wound counter-clockwise, as raylib expects.
        Raylib.ImageDrawTriangle(ref image, new Vector2(cx, 4), new Vector2(cx - 12, 56), new Vector2(cx + 12, 56), head);

        Raylib.ImageDrawTriangle(ref image, new Vector2(cx - 3, height - 120), new Vector2(cx - 20, height - 30), new Vector2(cx - 3, height - 40), fletch);
        Raylib.ImageDrawTriangle(ref image, new Vector2(cx + 3, height - 120), new Vector2(cx + 3, height - 40), new Vector2(cx + 20, height - 30), fletch);

        Raylib.ImageDrawRectangle(ref image, (int)cx - 4, height - 14, 8, 12, nock);
        return image;
    }

    public static Image Crosshair()
    {
        const int size = 64;
        const int c = size / 2;
        var color = new Color(255, 255, 255, 220);

        var image = Raylib.GenImageColor(size, size, Color.Blank);
        Raylib.ImageDrawCircleLines(ref image, c, c, 12, color);
        Raylib.ImageDrawCircle(ref image, c, c, 2, color);
        Raylib.ImageDrawLineEx(ref image, new Vector2(c, 4), new Vector2(c, c - 16), 2, color);
        Raylib.ImageDrawLineEx(ref image, new Vector2(c, c + 16), new Vector2(c, size - 4), 2, color);
        Raylib.ImageDrawLineEx(ref image, new Vector2(4, c), new Vector2(c - 16, c), 2, color);
        Raylib.ImageDrawLineEx(ref image, new Vector2(c + 16, c), new Vector2(size - 4, c), 2, color);
        return image;
    }
}
