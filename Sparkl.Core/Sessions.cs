using System.Collections.Immutable;

namespace Sparkl.Core;

public static class Sessions
{
    public static async ValueTask<IEnumerable<Session>> LoadAvailableSessionsAsync(
        CancellationToken cancellationToken = default
    )
    {
        const string waylandDir = "/usr/share/wayland-sessions";
        const string x11Dir     = "/usr/share/xsessions";

        var waylandSessions = await Sessions.LoadSessionsFromDirectoryAsync(
                                  waylandDir,
                                  SessionKind.Wayland,
                                  cancellationToken
                              );
        var x11Sessions = await Sessions.LoadSessionsFromDirectoryAsync( x11Dir, SessionKind.X11, cancellationToken );

        return waylandSessions.Concat( x11Sessions );
    }

    private static async ValueTask<IEnumerable<Session>> LoadSessionsFromDirectoryAsync(
        string            directory,
        SessionKind       kind,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if( !Directory.Exists( directory ) )
                return [];

            var sessionFiles = Directory.EnumerateFiles( directory, "*.desktop", SearchOption.TopDirectoryOnly );
            var sessions     = new List<Session>();

            foreach( var file in sessionFiles )
            {
                if( cancellationToken.IsCancellationRequested ) break;

                var entries = await Sessions.ParseDesktopEntryFileAsync( file, cancellationToken );
                if( !entries.TryGetValue( "Desktop Entry", out var desktopEntry ) ) continue;

                if( desktopEntry.TryGetValue( "TryExec", out var tryExec ) )
                {
                    var execPath = Sessions.Which( tryExec );
                    if( execPath is null ) continue;
                }

                if( desktopEntry.TryGetValue( "Name", out var name )
                 && desktopEntry.TryGetValue( "Exec", out var command ) )
                    sessions.Add( new( kind, name, command, [] ) );
            }

            return sessions;
        }
        catch { return []; }
    }

    private static async ValueTask<ImmutableDictionary<string, ImmutableDictionary<string, string>>>
        ParseDesktopEntryFileAsync( string path, CancellationToken cancellationToken )
    {
        var groups = ImmutableDictionary.CreateBuilder<string, ImmutableDictionary<string, string>.Builder>();

        try
        {
            var     lines        = await File.ReadAllLinesAsync( path, cancellationToken );
            string? currentGroup = null;

            foreach( var line in from line in lines select line.Trim() )
            {
                if( line.StartsWith( '[' ) && line.EndsWith( ']' ) )
                {
                    currentGroup = line[1..^1].Trim();
                    if( !groups.ContainsKey( currentGroup ) )
                        groups[currentGroup] = ImmutableDictionary.CreateBuilder<string, string>();
                }
                else if( currentGroup is not null && line.Contains( '=' ) )
                {
                    var parts = line.Split( '=', 2, StringSplitOptions.TrimEntries );
                    if( parts.Length == 2 ) groups[currentGroup][parts[0]] = parts[1];
                }
            }

            var pairs =
                from g in groups
                select KeyValuePair.Create( g.Key, g.Value.ToImmutable() );

            return pairs.ToImmutableDictionary( kvp => kvp.Key, kvp => kvp.Value );
        }
        catch { return ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty; }
    }

    private static string? Which( string search )
    {
        if( Path.IsPathRooted( search ) && File.Exists( search ) )
            return search;

        var paths = Environment.GetEnvironmentVariable( "PATH" )?.Split( Path.PathSeparator ) ?? [];
        return ( from path in paths select Path.Combine( path, search ) ).FirstOrDefault( File.Exists );
    }
}
