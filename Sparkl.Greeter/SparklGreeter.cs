using System.Collections.Immutable;
using Sparkl.Core;
using Sparkl.Core.Greetd;
using Spectre.Console;

namespace Sparkl.Greeter;

internal sealed class SparklGreeter( GreetdClient client, ImmutableArray<User> users, ImmutableArray<Session> sessions )
    : Core.Greeter( client )
{
    private const int      MaxPageSize = 10;
    private       Session? _selectedSession;

    private User? _selectedUser;

    /// <inheritdoc />
    protected override ValueTask OnAuthenticatedAsync( CancellationToken cancellationToken )
    {
        AnsiConsole.MarkupLine(
            "Welcome [green]{0}[/]!",
            Markup.Escape( this._selectedUser?.Name ?? "unknown" )
        );
        AnsiConsole.WriteLine();

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override ValueTask OnSessionStartedAsync( CancellationToken cancellationToken )
    {
        AnsiConsole.MarkupLine(
            "Entering [green]{0}[/], enjoy!",
            Markup.Escape( this._selectedSession?.Name ?? "unknown" )
        );
        AnsiConsole.WriteLine();

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override async ValueTask<User> GetUserAsync( CancellationToken cancellationToken )
    {
        User user;
        switch( users )
        {
            case []:
                var userInput = new TextPrompt<string>( "[cyan]Enter user[/] [yellow]\u203A[/]" )
                {
                    AllowEmpty  = false,
                    PromptStyle = new( Color.LightGreen ),
                };

                var enteredUsername = await AnsiConsole.PromptAsync( userInput, cancellationToken );
                user               = User.FromName( enteredUsername );
                this._selectedUser = user;
                break;

            case [var onlyUser]:
                this._selectedUser = onlyUser;
                user               = onlyUser;
                break;

            default:
                var userSelect = new SelectionPrompt<User>()
                                .Title( "[cyan]Select user[/] [yellow]\u203A[/]" )
                                .PageSize( SparklGreeter.MaxPageSize )
                                .AddChoices( users )
                                .WrapAround()
                                .UseConverter( u => u.Name );

                user               = await AnsiConsole.PromptAsync( userSelect, cancellationToken );
                this._selectedUser = user;
                break;
        }

        AnsiConsole.MarkupLine( "[yellow]▶[/] Logging in as [green]{0}[/]...", Markup.Escape( user.Name ) );
        AnsiConsole.WriteLine();

        return user;
    }

    /// <inheritdoc />
    protected override async ValueTask<Session> GetSessionAsync( CancellationToken cancellationToken )
    {
        Session session;
        switch( sessions )
        {
            case []:
                var sessionInput = new TextPrompt<string>( "[cyan]Enter command[/] [yellow]\u203A[/]" )
                {
                    AllowEmpty  = false,
                    PromptStyle = new( Color.LightGreen ),
                };

                var command = await AnsiConsole.PromptAsync( sessionInput, cancellationToken );
                session               = Session.FromCommand( command );
                this._selectedSession = session;
                break;

            case [var onlySession]:
                this._selectedSession = onlySession;
                session               = onlySession;
                break;

            default:
                var sessionPrompt = new SelectionPrompt<Session>()
                                   .Title( "[cyan]Select session[/] [yellow]\u203A[/]" )
                                   .PageSize( SparklGreeter.MaxPageSize )
                                   .HighlightStyle( new( Color.LightGreen ) )
                                   .AddChoices( sessions )
                                   .UseConverter( s => s.Name );

                session               = await AnsiConsole.PromptAsync( sessionPrompt, cancellationToken );
                this._selectedSession = session;
                break;
        }

        AnsiConsole.MarkupLine( "[yellow]▶[/] Selected [green]{0}[/].", Markup.Escape( session.Name ) );
        AnsiConsole.WriteLine();

        return session;
    }

    /// <inheritdoc />
    protected override ValueTask OnFailureAsync( string message, CancellationToken cancellationToken )
    {
        AnsiConsole.MarkupLine( "[red]\u2716[/] {0}", Markup.Escape( message ) );
        AnsiConsole.WriteLine();

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override async ValueTask<string?> OnPromptForInputAsync(
        string            message,
        bool              isSecret,
        CancellationToken cancellationToken
    )
    {
        var trimmed = Markup.Escape( message.Trim().TrimEnd( ':' ).Trim() );
        var prompt = new TextPrompt<string>( $"[cyan]{trimmed}[/] [yellow]\u203A[/]" )
        {
            AllowEmpty = isSecret,
            IsSecret   = isSecret,
            Mask       = isSecret ? '\u25CF' : null,
        };

        var result = await AnsiConsole.PromptAsync( prompt, cancellationToken );
        AnsiConsole.WriteLine();
        return String.IsNullOrEmpty( result ) ? null : result;
    }

    /// <inheritdoc />
    protected override ValueTask OnErrorAsync( string message, CancellationToken cancellationToken )
    {
        AnsiConsole.WriteLine( $"[red]\u2716[/] {Markup.Escape( message )}" );
        AnsiConsole.WriteLine();

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override ValueTask OnInfoAsync( string message, CancellationToken cancellationToken )
    {
        AnsiConsole.WriteLine( $"[blue]\u2139[/] {Markup.Escape( message )}" );
        AnsiConsole.WriteLine();

        return ValueTask.CompletedTask;
    }
}
