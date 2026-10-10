# Archery

First-person bow-and-arrow target game in pixel art. C# / .NET 10 / [Raylib-cs](https://github.com/ChrisDill/Raylib-cs).

## Run

```sh
dotnet run --project src/Archery.Game
```

## Layout

| Path | Purpose |
|---|---|
| `src/Archery.Game/Core` | Window, main loop, scene switching, config. `PixelScreen` is the native resolution. |
| `src/Archery.Game/Scenes` | Screens. `RangeScene` composes the first-person view. |
| `src/Archery.Game/Assets` | Image loading (`AssetStore`), file list (`AssetIds`), sprite attachment points (`SpriteAnchors`). |
| `assets/textures` | Finished art as PNG files, copied next to the executable on build. |

## Rendering

Everything is drawn at **320×180** into an off-screen texture, then scaled up by the largest whole number that fits
the window (4× → 1280×720), letterboxed if needed. Textures use nearest-neighbour sampling, so art pixels stay
square blocks. Draw sprites at whole-pixel positions.

## Art

Assets are plain PNG files drawn at native size (one image pixel = one game pixel). Edit or replace them in any
pixel-art editor (Aseprite, Piskel, Figma with pixel grid, …) and rebuild.

- **PNG only.** The raylib build bundled with Raylib-cs has no JPG decoder.
- Some sprites have attachment points (arrow rest and string nock point on the bow, the arrow's nock, the face
  centre on the target stand). They live in `SpriteAnchors.cs`; if you redraw one of those images, update its pixel
  coordinates there.
- The arrow is drawn pre-angled on a 2:1 slope (2 px left, 1 px up per step) and the bow leans the perpendicular way,
  so the arrow sits square to the string, on the arrow rest, with its nock on the string. The target sits on the
  arrow's line. Pulling later slides the arrow along that line instead of rotating it (rotated pixel art turns jagged).
- The bowstring is not part of `bow.png`; the game draws it as a 1-pixel line between the bow's tip/contact anchors
  so it can be pulled later.

| File | Size | Anchors |
|---|---|---|
| `sky.png` | 320×84 | Drawn at the top. |
| `hills.png` | 320×40 | Bottom edge sits on the horizon (y=81). |
| `ground.png` | 320×99 | Top edge is the horizon. |
| `target_face.png` | 41×41 | Centre (20,20), radius 20. |
| `target_stand.png` | 60×76 | Face centre (30,28). |
| `bow.png` | 110×160 | Nearly upright, leaning on a 1:2 slope. Arrow rest (50,72), nock point on string (70,82), tips/contacts in `SpriteAnchors.Bow`. |
| `arrow.png` | 64×38 | 2:1 slope. Nock (58,32), tip (8,7). Fletching sits near the nock, clear of the bow. |
| `crosshair.png` | 11×11 | Centred. |
