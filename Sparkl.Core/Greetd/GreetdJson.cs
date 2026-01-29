using System.Text.Json;

namespace Sparkl.Core.Greetd;

internal static class GreetdJson
{
    public static JsonSerializerOptions Options { get; } = new( JsonSerializerDefaults.Strict )
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy  = JsonNamingPolicy.SnakeCaseLower,
    #if DEBUG
        WriteIndented = true,
    #else
        WriteIndented = false,
    #endif
    };
}
