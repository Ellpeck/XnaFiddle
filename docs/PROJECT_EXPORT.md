# Project Export -- Technical Implementation Plan

## 1. Feature Overview

The Export Project feature lets users download a complete, ready-to-build desktop project from
their current fiddle. When the user clicks "Export," XnaFiddle generates a `.zip` file containing
a solution, `.csproj`, entry point, and game source file. The user opens the project in
Visual Studio or Rider, restores NuGet packages, and builds/runs it locally -- no manual setup
required.

**User workflow:**

1. Write and test a fiddle in the browser.
2. Click the Export button in the toolbar.
3. Choose a target platform (KNI DesktopGL, KNI WindowsDX, or MonoGame DesktopGL).
4. Browser downloads `MyFiddle.zip`.
5. Extract, open in IDE, build, run.

## 2. Architecture

### New class: `ProjectExporter`

**Location:** `XnaFiddle.BlazorGL/ProjectExporter.cs`

**Responsibilities:**

- Accept a `SnippetModel`, expanded source code, target platform enum, and optional asset list
- Generate all project files (solution, csproj, Program.cs, Game1.cs)
- Determine which NuGet packages to include based on preset flags and using-directive scanning
- Package everything into an in-memory `ZipArchive`
- Return the zip bytes for download

**Dependencies:**

- `SnippetModel` -- to read preset flags
- `SnippetExpander` -- to get the expanded source (already called by Index.razor before export)
- `System.IO.Compression.ZipArchive` -- zip generation
- JS interop -- triggering the browser download

**Target platform enum:**

```csharp
public enum ExportTarget
{
    KniDesktopGL,
    KniWindowsDX,
    MonoGameDesktopGL
}
```

**Class skeleton:**

```csharp
public static class ProjectExporter
{
    public static byte[] Export(
        SnippetModel snippet,
        string expandedSource,
        ExportTarget target,
        List<ExampleAsset> assets = null)
    {
        // 1. Determine NuGet packages from preset flags + using scanning
        // 2. Generate file contents from templates
        // 3. Build zip archive in memory
        // 4. Return byte[]
    }
}
```

**Integration with Index.razor:**

The Export button sits in the editor toolbar (the `<div>` containing Run and Examples buttons).
Clicking it opens a small dropdown to select the target platform. On selection,
`Index.razor` calls `ProjectExporter.Export(...)`, then triggers a JS download.

## 3. Assembly-to-NuGet Mapping

The 30 entries in `KniAssemblyNames` (CompilationService.cs lines 37-70) map to NuGet packages
as follows. The exported project does not reference individual assemblies -- it references NuGet
packages, which bring in their assemblies transitively.

### Always included (base framework)

These assemblies all come from a single NuGet meta-package:

| Assemblies | KNI NuGet Package | MonoGame NuGet Package |
|---|---|---|
| `Xna.Framework`, `Xna.Framework.Graphics`, `Xna.Framework.Content`, `Xna.Framework.Audio`, `Xna.Framework.Media`, `Xna.Framework.Input`, `Xna.Framework.Game`, `Xna.Framework.Devices`, `Xna.Framework.Storage`, `Xna.Framework.XR`, `Kni.Platform`, `nkast.Wasm.JSInterop`, `nkast.Wasm.Dom`, `nkast.Wasm.Canvas`, `nkast.Wasm.Audio`, `nkast.Wasm.XHR`, `nkast.Wasm.XR`, `nkast.Wasm.Clipboard` | `nkast.Xna.Framework.DesktopGL` (or `.WindowsDX`) | `MonoGame.Framework.DesktopGL` |

Note: The `nkast.Wasm.*` assemblies are Blazor-specific and are NOT needed in desktop exports.
The desktop KNI meta-package provides its own platform backend.

### Conditional packages

Included only when the snippet uses the corresponding library.

| Assemblies | KNI NuGet Package (Version) | MonoGame NuGet Package | Detection Strategy |
|---|---|---|---|
| `KniGum`, `GumCommon`, `FlatRedBall.InterpolationCore` | `Gum.KNI` (2026.3.28.2) | TBD -- Gum supports MonoGame but package name may differ | Preset flag: `IsGum` |
| `Apos.Shapes.KNI` | `Apos.Shapes.KNI` (0.6.8) | `Apos.Shapes` | Preset flag: `IsAposShapes` |
| `KNI.Extended` | `KNI.Extended` (5.5.0) | `MonoGame.Extended` | Preset flag: `IsMonoGameExtended` |
| `FontStashSharp.Kni`, `FontStashSharp.Base`, `FontStashSharp.Rasterizers.StbTrueTypeSharp` | `FontStashSharp.Kni` (1.5.4) | `FontStashSharp` | Using-directive scan: `FontStashSharp` |
| `Aether.Physics2D` | `Aether.Physics2D.KNI` (2.2.0) | `Aether.Physics2D` | Using-directive scan: `Aether.Dynamics` or `Aether.Collision` or `tainicom.Aether.Physics2D` |
| `KernSmith`, `KernSmith.GumCommon`, `KernSmith.KniGum` | `KernSmith` (0.12.0) + `KernSmith.KniGum` (0.12.0) + `KernSmith.Rasterizers.StbTrueType` (0.12.0) | None -- KNI-only | Using-directive scan: `KernSmith` |
| `TextCopy` | `TextCopy` (transitive via Gum.KNI) | N/A (transitive) | Not directly referenced; comes in via Gum |

## 4. File Generation Templates

### Project name

The exported project uses the name `MyFiddle`. All generated files use this name consistently.

### Solution file (`MyFiddle.slnx`)

Use the `.slnx` format (simplified XML solution format introduced in VS 2022 17.10). It is
dramatically simpler than the legacy `.sln` format. VS Code ignores solution files entirely
(it works directly with `.csproj`), and modern VS versions support `.slnx` natively.

```xml
<Solution>
  <Project Path="MyFiddle\MyFiddle.csproj" />
</Solution>
```

That's it — no GUIDs, no configuration platforms, no boilerplate. The `dotnet` CLI and
VS infer everything from the `.csproj`.

### Project file (`MyFiddle/MyFiddle.csproj`)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>MyFiddle</RootNamespace>
    <AssemblyName>MyFiddle</AssemblyName>
    <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
  </PropertyGroup>

  <!-- KNI-specific: required for KNI targets -->
  <!-- <PropertyGroup>
    <KniPlatform>DesktopGL</KniPlatform>
  </PropertyGroup> -->

  <ItemGroup>
    <!-- Base framework package (varies by target) -->
    <PackageReference Include="{{FRAMEWORK_PACKAGE}}" Version="{{FRAMEWORK_VERSION}}" />

    <!-- Conditional library packages (only those the snippet uses) -->
    {{#each CONDITIONAL_PACKAGES}}
    <PackageReference Include="{{PackageId}}" Version="{{Version}}" />
    {{/each}}
  </ItemGroup>

  <!-- Include raw content files if any assets are present -->
  {{#if HAS_ASSETS}}
  <ItemGroup>
    <None Update="Content\**\*">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
  {{/if}}

</Project>
```

For KNI targets, uncomment the `KniPlatform` property and set it to `DesktopGL` or `WindowsDX`.

The template placeholders above are illustrative. In the actual implementation, `ProjectExporter`
builds the XML with string interpolation or `XDocument`.

### Entry point (`MyFiddle/Program.cs`)

```csharp
using System;

namespace MyFiddle
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using var game = new Game1();
            game.Run();
        }
    }
}
```

### Game class (`MyFiddle/Game1.cs`)

This is the expanded snippet source from `SnippetExpander.Expand()` with two modifications:

1. **Class name:** Replace `FiddleGame` with `Game1` (simple string replace on the class
   declaration line).
2. **Namespace wrapper:** Wrap the entire file in `namespace MyFiddle { ... }`.

The constructor reference `new FiddleGame()` does not appear in the expanded source (it is only
in `Program.cs`), so only the class declaration line needs the rename.

```csharp
// Output of SnippetExpander.Expand(), modified:
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
// ... additional usings ...

namespace MyFiddle
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        // ... rest of expanded source ...
    }
}
```

### Assets (`MyFiddle/Content/`)

If the user has loaded assets (via drag-and-drop or from an example), they are copied into
`MyFiddle/Content/` inside the zip. The `.csproj` includes a `<None>` item that copies them
to the output directory on build.

Asset handling:

- Each `ExampleAsset` (with `FileName` and `Data`) becomes a file at
  `MyFiddle/Content/{FileName}`.
- User-loaded files (from the drag-and-drop feature in Index.razor) follow the same pattern.
- The exported code references assets via `Content/filename.ext` relative paths, matching how
  XnaFiddle loads them at runtime.

## 5. Zip Generation and Download

### In-memory zip creation

```csharp
using var memoryStream = new MemoryStream();
using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
{
    AddEntry(archive, "MyFiddle.sln", slnContent);
    AddEntry(archive, "MyFiddle/MyFiddle.csproj", csprojContent);
    AddEntry(archive, "MyFiddle/Program.cs", programContent);
    AddEntry(archive, "MyFiddle/Game1.cs", gameContent);

    // Assets
    foreach (var asset in assets)
        AddBinaryEntry(archive, $"MyFiddle/Content/{asset.FileName}", asset.Data);
}
byte[] zipBytes = memoryStream.ToArray();
```

### Browser download via JS interop

Add a JS function in `wwwroot/js/monaco-interop.js` (or a new `export-interop.js`):

```javascript
window.downloadFile = function (fileName, contentBase64) {
    const bytes = Uint8Array.from(atob(contentBase64), c => c.charCodeAt(0));
    const blob = new Blob([bytes], { type: 'application/zip' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};
```

Call from C#:

```csharp
string base64 = Convert.ToBase64String(zipBytes);
await JsRuntime.InvokeVoidAsync("downloadFile", "MyFiddle.zip", base64);
```

### Estimated zip size

- `.sln` + `.csproj` + `Program.cs` + `Game1.cs`: ~3-5 KB uncompressed, ~1-2 KB compressed
- Assets: varies, but textures are already compressed (PNG), so zip compression adds little
- Total without assets: under 2 KB
- Total with typical assets: under 100 KB

## 6. UI Integration

### Export button placement

Add an "Export" button to the editor toolbar, after the Examples button:

```html
<button @onclick="ToggleExportDropdown"
        style="padding: 4px 8px; background: #3c3c3c; color: #d4d4d4;
               border: 1px solid #555; cursor: pointer; font-size: 13px;
               display: flex; align-items: center; gap: 5px;">
    <i class="codicon codicon-desktop-download"></i>
    Export
</button>
```

### Export dropdown

When clicked, show a small dropdown below the button with three options:

- KNI DesktopGL (Windows/Linux/macOS)
- KNI WindowsDX (Windows only)
- MonoGame DesktopGL (Windows/Linux/macOS)

Clicking an option triggers the export and download immediately. The dropdown closes after
selection. No modal dialog is needed -- this is a lightweight, fast operation.

### Disabled state

The Export button should be disabled (grayed out) while compilation is in progress, same as
the Run button behavior.

## 7. Detecting Library Usage

Two strategies determine which NuGet packages to include in the exported project.

### Strategy 1: Preset flags

The `SnippetModel` has three boolean flags that directly indicate library usage:

| Flag | Package(s) added |
|---|---|
| `IsGum` | `Gum.KNI` (KNI) / TBD (MonoGame) |
| `IsAposShapes` | `Apos.Shapes.KNI` (KNI) / `Apos.Shapes` (MonoGame) |
| `IsMonoGameExtended` | `KNI.Extended` (KNI) / `MonoGame.Extended` (MonoGame) |

These flags are authoritative. If the flag is set, the package is included regardless of what
appears in the source code.

### Strategy 2: Using-directive scanning

Libraries without preset flags require scanning the expanded source for their using directives.

| Library | Using prefix to scan for | Package(s) added |
|---|---|---|
| FontStashSharp | `using FontStashSharp` | `FontStashSharp.Kni` (KNI) / `FontStashSharp` (MonoGame) |
| Aether.Physics2D | `using tainicom.Aether.Physics2D` | `Aether.Physics2D.KNI` (KNI) / `Aether.Physics2D` (MonoGame) |
| KernSmith | `using KernSmith` | `KernSmith` + `KernSmith.KniGum` + `KernSmith.Rasterizers.StbTrueType` (KNI only) |

**Implementation:** A simple `source.Contains("using FontStashSharp")` check on the expanded
source string is sufficient. No need for Roslyn syntax tree parsing -- the using directives
are always at the top of the file in a predictable format generated by `SnippetExpander`.

**KernSmith note:** When KernSmith is detected, all three KernSmith packages must be included.
The user does not pick individual KernSmith sub-packages. If the export target is MonoGame,
KernSmith is unavailable -- show a warning in the export output or omit silently with a comment
in the generated `.csproj`.

## 8. Version Synchronization

The exported `.csproj` must reference the same NuGet package versions that XnaFiddle uses. If
versions drift, the exported project may behave differently than the fiddle.

### Option A: Hardcoded versions (simple)

Store version strings as constants in `ProjectExporter`:

```csharp
const string GumVersion = "2026.3.28.2";
const string AposShapesVersion = "0.6.8";
// ...
```

**Downside:** When updating a package in `XnaFiddle.BlazorGL.csproj`, the developer must also
update the constants in `ProjectExporter`. Easy to forget.

### Option B: MSBuild-generated version constants (recommended)

Add an MSBuild target to `XnaFiddle.BlazorGL.csproj` that reads package versions and generates
a constants file at build time:

```xml
<Target Name="GeneratePackageVersions" BeforeTargets="CoreCompile"
        DependsOnTargets="CollectPackageReferences">
  <PropertyGroup>
    <_GumVersion>@(PackageReference->WithMetadataValue('Identity','Gum.KNI')->'%(Version)')</_GumVersion>
    <_AposVersion>@(PackageReference->WithMetadataValue('Identity','Apos.Shapes.KNI')->'%(Version)')</_AposVersion>
    <_FontStashVersion>@(PackageReference->WithMetadataValue('Identity','FontStashSharp.Kni')->'%(Version)')</_FontStashVersion>
    <_AetherVersion>@(PackageReference->WithMetadataValue('Identity','Aether.Physics2D.KNI')->'%(Version)')</_AetherVersion>
    <_KniExtendedVersion>@(PackageReference->WithMetadataValue('Identity','KNI.Extended')->'%(Version)')</_KniExtendedVersion>
    <_KernSmithVersion>@(PackageReference->WithMetadataValue('Identity','KernSmith')->'%(Version)')</_KernSmithVersion>
  </PropertyGroup>
  <WriteLinesToFile
    File="$(IntermediateOutputPath)PackageVersions.g.cs"
    Overwrite="true"
    Lines="
namespace XnaFiddle {
    public static class PackageVersions {
        public const string Gum = &quot;$(_GumVersion)&quot;;
        public const string AposShapes = &quot;$(_AposVersion)&quot;;
        public const string FontStashSharp = &quot;$(_FontStashVersion)&quot;;
        public const string AetherPhysics = &quot;$(_AetherVersion)&quot;;
        public const string KniExtended = &quot;$(_KniExtendedVersion)&quot;;
        public const string KernSmith = &quot;$(_KernSmithVersion)&quot;;
    }
}" />
  <ItemGroup>
    <Compile Include="$(IntermediateOutputPath)PackageVersions.g.cs" />
  </ItemGroup>
</Target>
```

This follows the same pattern already used for `BuildInfo.g.cs` (csproj lines 48-56).
`ProjectExporter` then references `PackageVersions.Gum`, etc., and versions are always
in sync with the `.csproj`.

**The base KNI framework version** needs special handling since `nkast.Xna.Framework.DesktopGL`
is not referenced directly by XnaFiddle (it uses a project reference to the submodule). Options:

- Read the version from the `Kni.Platform` assembly at runtime via reflection (same approach
  used by `CompilationService.GetAssemblyVersion`).
- Hardcode it and update it manually when the KNI submodule is updated. This is acceptable
  because submodule updates are infrequent and deliberate.

**MonoGame framework version** must be hardcoded regardless, since XnaFiddle does not reference
MonoGame packages at all. Pick the latest stable MonoGame release (currently 3.8.2.1106) and
update it periodically.

## 9. MonoGame Considerations

### Namespace compatibility

Both KNI and MonoGame use `Microsoft.Xna.Framework` namespaces. The expanded source code from
`SnippetExpander` works with either framework without modification. No namespace rewriting
is needed.

### API compatibility

Most XNA APIs are identical between KNI and MonoGame. Known differences:

- **Content loading:** XnaFiddle uses raw file loading (e.g., `Texture2D.FromStream`), not the
  content pipeline. The exported MonoGame project should do the same -- load files from a
  `Content/` folder copied to the output directory, not from an MGCB-processed content
  pipeline. This matches XnaFiddle's behavior and avoids requiring MGCB tool installation.
- **GraphicsProfile:** XnaFiddle sets `GraphicsProfile.HiDef`. MonoGame supports this on
  DesktopGL, so no change is needed.

### Library availability by target

| Library | KNI | MonoGame |
|---|---|---|
| Base framework | `nkast.Xna.Framework.DesktopGL` / `.WindowsDX` | `MonoGame.Framework.DesktopGL` |
| Gum | `Gum.KNI` | TBD (Gum supports MonoGame but package naming may differ) |
| Apos.Shapes | `Apos.Shapes.KNI` | `Apos.Shapes` |
| MonoGame.Extended / KNI.Extended | `KNI.Extended` | `MonoGame.Extended` |
| FontStashSharp | `FontStashSharp.Kni` | `FontStashSharp` |
| Aether.Physics2D | `Aether.Physics2D.KNI` | `Aether.Physics2D` |
| KernSmith | `KernSmith` + `KernSmith.KniGum` + `KernSmith.Rasterizers.StbTrueType` | Not available (KNI-only) |

### MonoGame export restrictions

If the user's fiddle uses a KNI-only library (KernSmith, or Gum until the MonoGame package is
confirmed), the MonoGame export option should either:

1. Be disabled with a tooltip explaining why, or
2. Show a warning listing the incompatible libraries before proceeding

Option 1 is cleaner. The Export dropdown should gray out "MonoGame DesktopGL" and show
something like: "Requires KernSmith (KNI-only)" on hover.

## 10. Future Considerations

### Multi-file projects

The current architecture generates a single `Game1.cs`. Future versions may support multi-file
fiddles (e.g., separate classes). The `ProjectExporter.Export()` method should accept a list of
source files rather than a single string, even if V1 always passes a single file. This avoids
a breaking API change later.

### Content pipeline integration

For MonoGame exports, advanced users may want MGCB content pipeline integration (for shader
compilation, font processing, etc.). This is out of scope for V1 but could be added as a
checkbox option in the export dropdown. It would require generating an `.mgcb` file alongside
the project.

### .NET version targeting

The exported project targets `net8.0`. As .NET 9 and beyond become standard, the export
should offer a target framework selector or default to the latest LTS version. The `.csproj`
template already parameterizes `TargetFramework`, so this is straightforward.

### Project naming

V1 uses the fixed name "MyFiddle." A future version could prompt the user for a project name
or derive it from the example name if an example was loaded.
