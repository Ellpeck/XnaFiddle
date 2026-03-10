using System.IO.Compression;
using System.Text;

// xnafiddle-encode snippet '{"IsGum":true,"initialize":"..."}'
// xnafiddle-encode snippet --file mysnippet.json
// xnafiddle-encode code 'using System; public class MyGame : Game { ... }'
// xnafiddle-encode code --file MyGame.cs
//
// Always outputs a single line: the full xnafiddle.net URL, ready to paste.

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  xnafiddle-encode snippet '<json>'");
    Console.Error.WriteLine("  xnafiddle-encode snippet --file <path>");
    Console.Error.WriteLine("  xnafiddle-encode code '<csharp>'");
    Console.Error.WriteLine("  xnafiddle-encode code --file <path>");
    return 1;
}

string mode = args[0].ToLowerInvariant();
if (mode != "snippet" && mode != "code")
{
    Console.Error.WriteLine($"Unknown mode '{args[0]}'. Use 'snippet' or 'code'.");
    return 1;
}

string input;
if (args[1] == "--file")
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("--file requires a path argument.");
        return 1;
    }
    string path = args[2];
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"File not found: {path}");
        return 1;
    }
    input = File.ReadAllText(path, Encoding.UTF8);
}
else
{
    input = args[1];
}

string encoded = Encode(input);
string param   = mode == "snippet" ? "snippet" : "code";
Console.WriteLine($"https://xnafiddle.net/#{param}={encoded}");
return 0;

static string Encode(string text)
{
    byte[] bytes = Encoding.UTF8.GetBytes(text);
    using var ms = new MemoryStream();
    using (var gz = new GZipStream(ms, CompressionLevel.Optimal))
        gz.Write(bytes);
    return Convert.ToBase64String(ms.ToArray())
        .Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
