# Archery

First-person bow-and-arrow target game in pixel art. C# / .NET 10 / [Raylib-cs](https://github.com/ChrisDill/Raylib-cs).

## Run

```sh
dotnet run --project src/Archery.Game
```

## Controls

| Input | Action |
|---|---|
| Hold left mouse | Draw the bow: the arrow slides back along its line and the string bends into a V. Full draw takes 0.7 s; at full draw the point sits just in front of the arrow rest. |
| Release left mouse | Shoot. The arrow flies to the crosshair and stays in the target; a new arrow is nocked. |
| Esc | Quit. |

## Layout

| Path | Purpose |
|---|---|
| `src/Archery.Game/Core` | Window, main loop, scene switching, config. `PixelScreen` is the native resolution; `PixelCamera` projects 3D points onto it. |
| `src/Archery.Game/Gameplay` | Game rules independent of drawing. `ArrowFlight` is the ballistic arrow model. |
| `src/Archery.Game/Scenes` | Screens. `RangeScene` composes the first-person view and runs shots. |
| `src/Archery.Game/Assets` | Image loading (`AssetStore`), file list (`AssetIds`), sprite attachment points (`SpriteAnchors`). |
| `assets/textures` | Finished art as PNG files, copied next to the executable on build. |

## Rendering

Everything is drawn at **320×180** into an off-screen texture, then scaled up by the largest whole number that fits
the window (4× → 1280×720), letterboxed if needed. Textures use nearest-neighbour sampling, so art pixels stay
square blocks. Draw sprites at whole-pixel positions.

## Arrow flight

The range has a simple 3D model behind the 2D art: the eye is at the origin looking along +Z (metres, +Y up), and
`PixelCamera` projects onto the 320×180 frame with the horizon on row 81. The target face is 30 m away; the focal
length is chosen so the 21-px face sprite matches a 122 cm face at that distance.

On release, `ArrowFlight` launches the arrow from the nock under gravity and solves the launch velocity so the tip
lands exactly on the crosshair after `FlightSeconds`. Each frame the arrow's tip and nock are projected and drawn as
a shaft with fletching, so it shrinks and arcs into the distance. The 3D arrow length and nock depth are derived from
the nocked sprite, so the drawn arrow starts exactly where the sprite was. Far away (seen almost end-on, e.g. stuck in
the target) it is drawn as its fletching from behind: a tiny red "Y".

## Art

Assets are plain PNG files drawn at native size (one image pixel = one game pixel). Edit or replace them in any
pixel-art editor (Aseprite, Piskel, Figma with pixel grid, …) and rebuild.

- **PNG only.** The raylib build bundled with Raylib-cs has no JPG decoder.
- Some sprites have attachment points (arrow rest and string nock point on the bow, the arrow's nock, the face
  centre on the target stand). They live in `SpriteAnchors.cs`; if you redraw one of those images, update its pixel
  coordinates there.
- The arrow is drawn pre-angled on a 2:1 slope (2 px left, 1 px up per step) and the bow leans the perpendicular way,
  so the arrow sits square to the string, on the arrow rest, with its nock on the string. The target sits on the
  arrow's line. Drawing the bow slides the arrow back along that line instead of rotating it (rotated pixel art turns
  jagged).
- The bowstring is not part of `bow.png`; the game draws it as 1-pixel lines from the bow's tips through the contact
  points to the nock, so it bends into a V when drawn.

| File | Size | Anchors |
|---|---|---|
| `sky.png` | 320×84 | Drawn at the top. |
| `hills.png` | 320×40 | Bottom edge sits on the horizon (y=81). |
| `ground.png` | 320×99 | Top edge is the horizon. |
| `target_face.png` | 21×21 | Centre (10,10), radius 10 (sets the camera scale, see Arrow flight). |
| `target_stand.png` | 32×40 | Face centre (16,14). |
| `bow.png` | 110×160 | Nearly upright, leaning on a 1:2 slope. Arrow rest (50,72), nock point on string (70,82), tips/contacts in `SpriteAnchors.Bow`. |
| `arrow.png` | 64×38 | 2:1 slope. Nock (58,32), tip (8,7). Fletching sits near the nock, clear of the bow. |
| `crosshair.png` | 13×13 | Centred; open centre so hits stay visible. |
