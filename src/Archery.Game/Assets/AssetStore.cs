using Raylib_cs;

namespace Archery.Assets;

/// <summary>
/// Loads pre-generated textures from disk. Requires an open window (GPU context).
/// Assets are never generated at runtime; run Archery.AssetGen to (re)create them.
/// </summary>
public sealed class AssetStore : IDisposable
{
    private readonly string _root;
    private readonly Dictionary<string, Texture2D> _textures = new();

    public AssetStore(string root)
    {
        _root = root;
    }

    public void LoadAll()
    {
        foreach (var id in AssetIds.AllTextures)
        {
            var path = Path.Combine(_root, id);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Missing asset '{id}'. Run: dotnet run --project tools/Archery.AssetGen", path);
            }

            var texture = Raylib.LoadTexture(path);
            if (!Raylib.IsTextureValid(texture))
            {
                throw new InvalidDataException($"Failed to load texture '{path}'.");
            }

            Raylib.SetTextureFilter(texture, TextureFilter.Bilinear);
            _textures[id] = texture;
        }
    }

    public Texture2D Texture(string id) => _textures[id];

    public void Dispose()
    {
        foreach (var texture in _textures.Values)
        {
            Raylib.UnloadTexture(texture);
        }

        _textures.Clear();
    }
}
