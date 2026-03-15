# Add a New Example to XnaFiddle

This skill walks through adding a new built-in example to the XnaFiddle example gallery.

## Steps

### 1. Create the example file

Add a new `.cs` file under `XnaFiddle.BlazorGL/Examples/`:

```csharp
// XnaFiddle.BlazorGL/Examples/MyExample.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class MyExampleGame : Game
{
    GraphicsDeviceManager _graphics;

    public MyExampleGame()
    {
        _graphics = new GraphicsDeviceManager(this);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        base.Draw(gameTime);
    }
}
```

**Rules for example code:**
- Must have exactly one class extending `Game` (or `Microsoft.Xna.Framework.Game`)
- No `namespace` declaration (or use a top-level-friendly one) — the runner scans all types
- No file I/O or disk access — use `InMemoryContentManager` for assets (they are stored in `InMemoryContentManager.Files`)
- Keep `using` statements minimal; Roslyn resolves from the hardcoded `KniAssemblyNames` list

### 2. Mark it as an embedded resource

In `XnaFiddle.BlazorGL.csproj`, examples are included as `EmbeddedResource` with `ExcludeFromCompile`:

```xml
<EmbeddedResource Include="Examples\MyExample.cs">
  <ExcludeFromCompile>true</ExcludeFromCompile>
</EmbeddedResource>
```

Check the existing pattern in the `.csproj` and follow it exactly.

### 3. Verify the gallery picks it up

`ExampleGallery.cs` reads embedded resources matching `*.Examples.*.cs` and exposes them by filename stem. The new example will automatically appear in the dropdown on the page — no code change needed in `ExampleGallery.cs` or `Index.razor`.

### 4. Copy assets to wwwroot (if the example has assets)

If the example has non-code asset files (`.png`, `.fnt`, `.ttf`), they must exist in **two** places:

1. `Examples/{ExampleName}.{AssetFile}` — embedded resource, loaded at runtime by `ExampleGallery.LoadAssets()`
2. `wwwroot/examples/{ExampleName}/{AssetFile}` — static web asset, served over HTTP so share links can re-fetch them

```bash
mkdir -p XnaFiddle.BlazorGL/wwwroot/examples/MyExample
cp XnaFiddle.BlazorGL/Examples/MyExample.MyAsset.png \
   XnaFiddle.BlazorGL/wwwroot/examples/MyExample/MyAsset.png
```

`LoadExampleAssets()` automatically sets `AssetInfo.SourceUrl` to `{baseUri}examples/{ExampleName}/{file}`, which `GetAssetUrlsFragment()` includes in share URLs. Without the wwwroot copy, the share link will have the URL but fetching it will 404.

### 6. Update third-party notices (if bundling external assets)

If the example bundles third-party assets (fonts, images, etc.) from external projects, check whether their license requires attribution (e.g. Apache 2.0, CC-BY). If so, add a row to the table in `THIRD-PARTY-NOTICES.md` at the repo root:

```markdown
| `Examples/MyExample.AssetName.ext` | License Name | Copyright Holder | [Source](https://...) |
```

Assets under licenses that don't require attribution (MIT, CC0, Unlicense, public domain) are covered by the file's general intro paragraph and don't need an explicit entry.

### 7. Test

```bash
dotnet build XnaFiddle.BlazorGL/XnaFiddle.BlazorGL.csproj
```

Open the app, select the new example from the gallery dropdown, and click **Compile & Run**.

## Key files

- `XnaFiddle.BlazorGL/Examples/` — example source files
- `XnaFiddle.BlazorGL/ExampleGallery.cs` — loads embedded resources, returns source by name
- `XnaFiddle.BlazorGL/XnaFiddle.BlazorGL.csproj` — must include new file as EmbeddedResource
- `XnaFiddle.BlazorGL/Pages/Index.razor` — gallery dropdown (no change needed)
- `XnaFiddle.BlazorGL/InMemoryContentManager.cs` — use for asset loading in examples
