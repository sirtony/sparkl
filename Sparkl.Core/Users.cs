namespace Sparkl.Core;

public static class Users
{
    private const ushort MinSystemUserId = 1000;
    private const ushort MaxSystemUserId = 60000;

    public static async ValueTask<IEnumerable<User>> LoadSystemUsersAsync(
        CancellationToken cancellationToken = default
    ) =>
        from line in await Users.ParsePasswdFileLinesAsync( cancellationToken )
        let parts = line.Split( ':', StringSplitOptions.TrimEntries )
        where parts.Length >= 7
        let username = parts[0]
        where UInt16.TryParse( parts[2], out var uid ) && uid is >= Users.MinSystemUserId and <= Users.MaxSystemUserId
        let homeDir = parts[5]
        let shell = parts[6]
        where !shell.EndsWith( "/nologin" ) && !shell.EndsWith( "/false" )
        select new User( username, homeDir );

    private static async ValueTask<IEnumerable<string>> ParsePasswdFileLinesAsync( CancellationToken cancellationToken )
    {
        try
        {
            var lines = await File.ReadAllLinesAsync( "/etc/passwd", cancellationToken );
            return
                from line in lines
                where !String.IsNullOrWhiteSpace( line ) && !line.StartsWith( '#' )
                select line.Trim();
        }
        catch { return []; }
    }
}
