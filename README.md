# XnaFiddle

A browser-based C# game editor and runner powered by [KNI](https://github.com/kniEngine/kni). Write XNA-style game code in a Monaco editor, compile it with Roslyn in the browser, and see it run live on a WebGL canvas.

## Setup

```bash
git clone --recursive https://github.com/user/XnaFiddle.git
cd XnaFiddle
dotnet run --project XnaFiddle.BlazorGL/XnaFiddle.BlazorGL.csproj
```

If you already cloned without `--recursive`, run `git submodule update --init --recursive` before building.

Opens at **https://localhost:60440**.

## Upgrading Apos.Shapes

The `Apos.Shapes.KNI` NuGet package includes a pre-built content file (`apos-shapes.xnb`) that must be shipped with the app. When upgrading to a new version:

1. Update the package version in `XnaFiddle.BlazorGL/XnaFiddle.BlazorGL.csproj`.
2. Build the project so NuGet restores the new package.
3. Copy the new XNB from the NuGet cache to `wwwroot/`:
   ```
   cp ~/.nuget/packages/apos.shapes/<NEW_VERSION>/buildTransitive/Content/bin/DesktopGL/Content/apos-shapes.xnb XnaFiddle.BlazorGL/wwwroot/apos-shapes.xnb
   ```
4. Verify the Apos.Shapes example still runs.

## License

MIT
