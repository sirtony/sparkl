using Sparkl.Core;
using Sparkl.Core.Greetd;
using Sparkl.Greeter;
using Spectre.Console;

const int maxRetries = 5;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += ( _, e ) => {
    e.Cancel = true;
    cts.Cancel();
};

try
{
    var os       = await OsRelease.LoadAsync();
    var osName   = new Text( os.Name,                 new( os.Color, decoration: Decoration.Bold ) );
    var hostName = new Text( Environment.MachineName, new( Color.Yellow, decoration: Decoration.Bold ) );

    AnsiConsole.WriteLine();
    AnsiConsole.Write( "Authenticate into " );
    AnsiConsole.Write( hostName );
    AnsiConsole.Write( " (" );
    AnsiConsole.Write( osName );
    AnsiConsole.WriteLine( ")." );
    AnsiConsole.WriteLine();
}
catch
{
    // ignored
}

var users    = await Users.LoadSystemUsersAsync( cts.Token );
var sessions = await Sessions.LoadAvailableSessionsAsync( cts.Token );
var client   = await GreetdClient.ConnectAsync( cts.Token );
var greeter  = new SparklGreeter( client, [..users], [..sessions] );

for( var retryCount = 0; retryCount < maxRetries; retryCount++ )
{
    try
    {
        if( await greeter.StartAsync( cts.Token ) ) break;
    }
    catch( OperationCanceledException ) { break; }
    catch( Exception ex )
    {
        var delay = TimeSpan.FromSeconds( Math.Pow( 2, retryCount ) );
        AnsiConsole.MarkupLine( "[red]\u2716[/] {0}", Markup.Escape( ex.Message ) );
        AnsiConsole.WriteLine();

        if( !( retryCount >= maxRetries - 1 ) )
            AnsiConsole.MarkupLine( "Retrying in [dim yellow]{0}[/] seconds...", delay.TotalSeconds );

        await Task.Delay( delay );
        AnsiConsole.Clear();
    }
}
