# Archery

First-person bow-and-arrow target game. C# / .NET 10 / [Raylib-cs](https://github.com/ChrisDill/Raylib-cs).

## Layout

| Path | Purpose |
|---|---|
| `src/Archery.Game` | The game: window, main loop, scenes, asset loading. |
| `tools/Archery.AssetGen` | Procedurally draws every texture and saves it as PNG into `assets/`. |
| `assets/` | Generated assets (committed). The game only loads these; it never generates art at runtime. |

`src/Archery.Game/Assets/AssetIds.cs` is the single list of asset files; it is linked into the generator so both sides stay in sync.

## Run

```sh
dotnet run --project src/Archery.Game
```

## Regenerate assets

Only needed after changing a generator in `tools/Archery.AssetGen/Program.cs`:

```sh
dotnet run --project tools/Archery.AssetGen
```

Rebuild the game afterwards so the new files are copied to its output folder.
