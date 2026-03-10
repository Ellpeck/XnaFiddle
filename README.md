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

## License

MIT
