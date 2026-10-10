# Archery

First-person bow-and-arrow target game in pixel art. C# / .NET 10 / [Raylib-cs](https://github.com/ChrisDill/Raylib-cs).

## Run

```sh
dotnet run --project src/Archery.Game
```

## Controls

| Input | Action |
|---|---|
| Hold left mouse | Raise and draw: the lowered bow drops away and the bow comes up upright in front of your face. You look down the arrow as it is drawn back toward you (0.7 s to full draw); its point settles on the arrow rest inside the crosshair. |
| Release left mouse | Shoot. The camera follows the arrow downrange in slow motion, stops short of the target and watches it hit, then cuts back to you with a new arrow. |
| Esc | Quit. |

## Layout

| Path | Purpose |
|---|---|
| `src/Archery.Game/Core` | Window, main loop, scene switching, config. `PixelScreen` is the native resolution; `PixelCamera` projects 3D points onto it. |
| `src/Archery.Game/Gameplay` | Game rules independent of drawing. `ArrowFlight` is the ballistic arrow model. |
| `src/Archery.Game/Scenes` | Screens. `RangeScene` composes the first-person view and runs shots; `ArrowCam` is the chase camera. |
| `src/Archery.Game/Assets` | Image loading (`AssetStore`), file list (`AssetIds`), sprite attachment points (`SpriteAnchors`). |
| `assets/textures` | Finished art as PNG files, copied next to the executable on build. |

## Rendering

Everything is drawn at **320×180** into an off-screen texture, then scaled up by the largest whole number that fits
the window (4× → 1280×720), letterboxed if needed. Textures use nearest-neighbour sampling, so art pixels stay
square blocks. Draw sprites at whole-pixel positions.

## 3D model and arrow flight

The range has a simple 3D model behind the 2D art: the eye is at the origin looking along +Z (metres, +Y up), and
`PixelCamera` projects onto the 320×180 frame with the horizon on row 81. The target face is 30 m away; the focal
length is chosen so the 21-px face sprite matches a 122 cm face at that distance. The target is drawn through the
camera, scaled by distance (exactly 1:1 from the eye).

In the raised aim the arrow is a 3D object pointing at the target, drawn back along its own line toward the eye; its
point projects onto the arrow rest of `bow_aim.png`. On release, `ArrowFlight` launches it from exactly that pose
under gravity, solving the launch velocity so the tip lands on the crosshair after `FlightSeconds`. Each frame the
arrow's tip and nock are projected and drawn as a shaft with fletching; seen nearly end-on (e.g. stuck in the target)
it is drawn as its fletching from behind, sized by distance.

`ArrowCam` moves the camera during the flight: from the eye it slides along the eye→tip line to just behind and above
the arrow, follows it, and stops 5 m before the target on the arrow's line so the arrow flies on into it. The sky,
hills and ground do not move with the camera yet.

## Art

Assets are plain PNG files drawn at native size (one image pixel = one game pixel). Edit or replace them in any
pixel-art editor (Aseprite, Piskel, Figma with pixel grid, …) and rebuild.

- **PNG only.** The raylib build bundled with Raylib-cs has no JPG decoder.
- Some sprites have attachment points (arrow rest and string nock point on the bow, the arrow's nock, the face
  centre on the target stand). They live in `SpriteAnchors.cs`; if you redraw one of those images, update its pixel
  coordinates there.
- The lowered arrow is drawn pre-angled on a 2:1 slope (2 px left, 1 px up per step) and the lowered bow leans the
  perpendicular way, so the arrow sits square to the string, on the arrow rest, with its nock on the string; the
  target sits on the arrow's line.
- Bowstrings are not part of the bow images; the game draws them as 1-pixel lines between the string anchors (and the
  nock in the aim), so they bend into a V when drawn.

| File | Size | Anchors |
|---|---|---|
| `sky.png` | 320×84 | Drawn at the top. |
| `hills.png` | 320×40 | Bottom edge sits on the horizon (y=81). |
| `ground.png` | 320×99 | Top edge is the horizon. |
| `target_face.png` | 21×21 | Centre (10,10), radius 10 (sets the camera scale, see Arrow flight). |
| `target_stand.png` | 32×40 | Face centre (16,14). |
| `bow.png` | 110×160 | Nearly upright, leaning on a 1:2 slope. Arrow rest (50,72), nock point on string (70,82), tips/contacts in `SpriteAnchors.Bow`. |
| `bow_aim.png` | 64×260 | Bow raised in front of the face, seen from behind, limbs running off-screen. Arrow-rest tip (24,127), string ends in `SpriteAnchors.BowAim`. |
| `arrow.png` | 64×38 | 2:1 slope. Nock (58,32), tip (8,7). Fletching sits near the nock, clear of the bow. |
| `crosshair.png` | 13×13 | Centred; open centre so hits stay visible. |
