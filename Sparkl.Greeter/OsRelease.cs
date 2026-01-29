using System.Globalization;
using Spectre.Console;

namespace Sparkl.Greeter;

internal sealed record OsRelease( string Name, Color Color )
{
    public static async ValueTask<OsRelease> LoadAsync( CancellationToken cancellationToken = default )
    {
        var dict  = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
        var lines = await File.ReadAllLinesAsync( "/etc/os-release", cancellationToken );
        foreach( var line in lines )
        {
            var parts = line.Split( '=', 2, StringSplitOptions.TrimEntries );
            if( parts.Length != 2 ) continue;

            var key   = parts[0].Trim();
            var value = parts[1].Trim().Trim( '"' );
            dict[key] = value;
        }

        var name = dict.TryGetValue( "PRETTY_NAME", out var prettyName )
                       ? prettyName
                       : dict.TryGetValue( "NAME", out var simpleName )
                           ? simpleName
                           : dict.GetValueOrDefault( "ID", "Unknown OS" );

        var color = dict.TryGetValue( "ANSI_COLOR", out var code )
                        ? ParseColor( code ) ?? Color.Cyan
                        : Color.Cyan;

        return new( name, color );

        static Color? ParseColor( string colorStr )
        {
            var parts = colorStr.Split( ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries );
            if( parts.Length < 5 ) return null;

            var rgbParts = parts[2..5];
            var r        = Byte.Parse( rgbParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture );
            var g        = Byte.Parse( rgbParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture );
            var b        = Byte.Parse( rgbParts[2], NumberStyles.Integer, CultureInfo.InvariantCulture );

            return new Color( r, g, b );
        }
    }
}
