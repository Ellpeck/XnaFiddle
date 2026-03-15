---
description: How file/asset loading works in XnaFiddle — embedded example assets, user drag-and-drop, InMemoryContentManager, and supported formats. Load when working on content loading, asset handling, drag-and-drop, or adding file format support.
---

# File Loading

XnaFiddle has no disk or traditional content pipeline. All assets live in memory and are loaded through `InMemoryContentManager`, a custom `ContentManager` subclass that serves files from a static `Dictionary<string, byte[]>`.

## Three entry paths, one destination

| Source | Mechanism | Ends up in | SourceUrl set? |
|---|---|---|---|
| **Example assets** | Embedded resources in the assembly, loaded by `ExampleGallery.LoadAssets()` | `InMemoryContentManager.AddFile()` | Yes — points to `wwwroot/examples/{ExampleName}/{file}` |
| **URL-fetched assets** | `FetchAndAddAssetUrl(url)` via `&assets=` share param or URL input | `InMemoryContentManager.AddFile()` | Yes — the fetched URL |
| **User drag-and-drop** | JS `fileDropInterop` -> `OnFileDropped` JSInvokable | `InMemoryContentManager.AddFile()` | No — cannot be re-fetched |

Both paths converge on `InMemoryContentManager.AddFile(fileName, bytes)`, which stores the data under the original filename AND the extension-stripped name. This lets user code call `Content.Load<Texture2D>("KniIcon")` without knowing the extension.

## Supported formats

Controlled by `SupportedAssetExtensions` in `Index.razor.cs`:

- **`.png`** — loaded as `Texture2D` via `Texture2D.FromStream()`
- **`.fnt`** — BMFont text format; stored as raw bytes. The UI parses `page` lines to show which texture files the font references (so users know what companion `.png` files to also drop)

Only `Texture2D` is handled by `InMemoryContentManager.Load<T>()` today. Any other type falls through to `base.Load<T>()`, which will fail (no disk content pipeline exists).

## Embedded example assets

Naming convention: `Examples/{ExampleName}.{AssetFile}` in the filesystem becomes embedded resource `XnaFiddle.Examples.{ExampleName}.{AssetFile}`.

Example: `Examples/TextureLoading.KniIcon.png` is the asset `KniIcon.png` for the `TextureLoading` example.

The `.csproj` has two wildcard `EmbeddedResource` includes — one for `*.cs` (example code) and one for everything else (assets). No manual `.csproj` edits are needed when adding a new asset file.

`ExampleGallery.LoadAssets(name)` finds all embedded resources that share the example's prefix but are not the `.cs` file, strips the prefix, and returns them as `ExampleAsset[]` (filename + byte array).

### Static copies for sharing

Every example asset is **also** served as a static file under `wwwroot/examples/{ExampleName}/{AssetFile}`. This duplicate is what makes share links work: `LoadExampleAssets()` sets `AssetInfo.SourceUrl` to `{Navigation.BaseUri}examples/{ExampleName}/{file}`, so `GetAssetUrlsFragment()` includes those URLs in the `&assets=` share fragment.

**When adding a new example asset:** place the file in `Examples/` (embedded resource, picked up by wildcard), **and** copy it to `wwwroot/examples/{ExampleName}/{AssetFile}` (static web asset). Both locations are required.

Current static copies: `AetherPhysics/CircleSprite.png`, `AetherPhysics/GroundSprite.png`, `FontStashSharp/DroidSans.ttf`, `TextureLoading/KniIcon.png`.

## Drag-and-drop flow

1. JS `fileDropInterop` listens on `#canvasHolder` for `drop` events
2. JS filters to `file.type.startsWith('image/')` — this means `.fnt` files only work if the browser assigns an image MIME type (potential gap; the C# side accepts `.fnt` but JS may reject it)
3. File is read as base64 via `FileReader`, sent to C# `OnFileDropped(fileName, base64)`
4. C# validates extension against `SupportedAssetExtensions`, enforces 10 MB limit
5. Calls `InMemoryContentManager.AddFile()` and updates the UI asset list

## Static persistence

`InMemoryContentManager._files` is static — assets survive across recompilations. The loaded-asset cache (`_loaded`) is per-instance and cleared on `Unload()`. Each compile run creates a fresh `InMemoryContentManager` instance, but the underlying file store persists.

## Adding a new file format

1. Add the extension to `SupportedAssetExtensions` in `Index.razor.cs`
2. Add a type check branch in `InMemoryContentManager.Load<T>()` (alongside the existing `Texture2D` branch)
3. If the format is not an image, update the JS `fileDropInterop` drop handler — it currently filters on `file.type.startsWith('image/')`, which will silently reject non-image files
4. For embedded example assets: place the file in `Examples/` with the `{ExampleName}.{filename}` naming convention; the `.csproj` wildcards will pick it up automatically

## Key files

| File | Role |
|---|---|
| `XnaFiddle.BlazorGL/InMemoryContentManager.cs` | Static file store + `ContentManager` that loads `Texture2D` from bytes |
| `XnaFiddle.BlazorGL/ExampleGallery.cs` | Reads embedded resources; `LoadAssets()` extracts non-code files for an example |
| `XnaFiddle.BlazorGL/Pages/Index.razor.cs` | `OnFileDropped` JSInvokable, `SupportedAssetExtensions`, `LoadExampleAssets()` |
| `XnaFiddle.BlazorGL/wwwroot/js/monaco-interop.js` | `fileDropInterop` — JS drag-and-drop handler on `#canvasHolder` |
| `XnaFiddle.BlazorGL/XnaFiddle.BlazorGL.csproj` | `EmbeddedResource` wildcards for `Examples/` |
| `XnaFiddle.BlazorGL/Examples/TextureLoading.cs` | Reference example showing `Content.Load<Texture2D>()` usage |
