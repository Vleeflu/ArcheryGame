using Raylib_cs;

namespace Archery.Assets;

/// <summary>
/// Loads the image files in <c>assets/</c> into GPU textures. Requires an open window (GPU context).
/// </summary>
public sealed class AssetStore : IDisposable
{
    // OpenGL texture parameter and value (raylib exposes the call, not the constants).
    private const int GlTextureMagFilter = 0x2800, GlNearest = 0x2600;

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

            if (AssetIds.ScaledTextures.Contains(id))
            {
                // Shrunk: mipmapped and filtered, so the rings are averaged into the grid instead of breaking up.
                // Full size or larger: nearest-neighbour, so art pixels stay square blocks. Clamping stops filtering
                // from bleeding the opposite edge into the borders.
                Raylib.GenTextureMipmaps(ref texture);
                Raylib.SetTextureFilter(texture, TextureFilter.Trilinear);
                Rlgl.TextureParameters(texture.Id, GlTextureMagFilter, GlNearest);
                Raylib.SetTextureWrap(texture, TextureWrap.Clamp);
            }
            else
            {
                // Pixel art: sample nearest-neighbour only, so art pixels stay square blocks.
                Raylib.SetTextureFilter(texture, TextureFilter.Point);
            }
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
