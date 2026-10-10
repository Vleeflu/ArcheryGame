using Raylib_cs;

namespace Archery.Assets;

/// <summary>
/// Loads the image files in <c>assets/</c> into GPU textures. Requires an open window (GPU context).
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
                throw new FileNotFoundException($"Missing asset '{id}'.", path);
            }

            var texture = Raylib.LoadTexture(path);
            if (!Raylib.IsTextureValid(texture))
            {
                throw new InvalidDataException($"Failed to load texture '{path}'.");
            }

            // Pixel art: sprites are drawn 1:1 into the low-res frame, so sample nearest-neighbour only.
            Raylib.SetTextureFilter(texture, TextureFilter.Point);
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
