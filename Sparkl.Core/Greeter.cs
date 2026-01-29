using System.Diagnostics;
using Sparkl.Core.Greetd;

namespace Sparkl.Core;

public abstract class Greeter( GreetdClient client ) : IAsyncDisposable, IDisposable
{
    private bool         _disposed;
    private GreeterState _state = GreeterState.Connected;

    protected abstract ValueTask<User>    GetUserAsync( CancellationToken    cancellationToken );
    protected abstract ValueTask<Session> GetSessionAsync( CancellationToken cancellationToken );

    protected virtual ValueTask OnAuthenticatedAsync( CancellationToken cancellationToken ) => ValueTask.CompletedTask;
    protected virtual ValueTask OnSessionStartedAsync( CancellationToken cancellationToken ) => ValueTask.CompletedTask;
    protected abstract ValueTask OnFailureAsync( string message, CancellationToken cancellationToken );

    protected abstract ValueTask<string?> OnPromptForInputAsync(
        string            message,
        bool              isSecret,
        CancellationToken cancellationToken
    );

    protected abstract ValueTask OnErrorAsync( string message, CancellationToken cancellationToken );
    protected abstract ValueTask OnInfoAsync( string  message, CancellationToken cancellationToken );

    public async ValueTask<bool> StartAsync( CancellationToken cancellationToken = default )
    {
        ObjectDisposedException.ThrowIf( this._disposed, this );

        this._state = GreeterState.Authenticating;
        var user             = await this.GetUserAsync( cancellationToken );
        var createSessionReq = new GreetdRequest.CreateSession( user.Name );
        await client.SendAsync( createSessionReq, cancellationToken );

        var success    = true;
        var shouldExit = false;
        while( !cancellationToken.IsCancellationRequested )
        {
            if( shouldExit )
            {
                if( this._state is not GreeterState.Finished )
                {
                    var req = new GreetdRequest.CancelSession();
                    await client.SendAsync( req, cancellationToken );
                    success = false;
                }

                break;
            }

            var response = await client.ReceiveAsync<GreetdResponse>( cancellationToken );
            switch( response )
            {
                case GreetdResponse.Success:
                    if( this._state is GreeterState.Authenticating )
                    {
                        await this.OnAuthenticatedAsync( cancellationToken );
                        this._state = GreeterState.StartingSession;
                        var session  = await this.GetSessionAsync( cancellationToken );
                        var startReq = new GreetdRequest.StartSession( [session.Command], session.Environment );
                        await client.SendAsync( startReq, cancellationToken );
                        break;
                    }

                    if( this._state is GreeterState.StartingSession or GreeterState.Cancelling )
                    {
                        if( this._state is GreeterState.StartingSession )
                            await this.OnSessionStartedAsync( cancellationToken );

                        this._state = GreeterState.Finished;
                        shouldExit  = true;
                    }

                    break;

                case GreetdResponse.Error(var type, var message):
                    this._state = GreeterState.Cancelling;
                    success     = false;
                    var cancelReq = new GreetdRequest.CancelSession();
                    await client.SendAsync( cancelReq, cancellationToken );

                    switch( type )
                    {
                        case ErrorType.Error:
                            await this.OnErrorAsync( message, cancellationToken );
                            break;
                        case ErrorType.AuthError:
                            await this.OnFailureAsync( message, cancellationToken );
                            break;
                        default:
                            throw new UnreachableException();
                    }

                    break;

                case GreetdResponse.AuthMessage(var type, var message):
                    string? answer = null;
                    switch( type )
                    {
                        case AuthMessageType.Visible:
                            answer = await this.OnPromptForInputAsync( message, false, cancellationToken );
                            break;
                        case AuthMessageType.Secret:
                            answer = await this.OnPromptForInputAsync( message, true, cancellationToken );
                            break;
                        case AuthMessageType.Info:
                            await this.OnInfoAsync( message, cancellationToken );
                            break;
                        case AuthMessageType.Error:
                            await this.OnFailureAsync( message, cancellationToken );
                            break;
                        default:
                            throw new UnreachableException();
                    }

                    var req = new GreetdRequest.PostAuthMessageResponse( answer );
                    await client.SendAsync( req, cancellationToken );

                    break;

                default: throw new UnreachableException();
            }
        }

        return success;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if( this._disposed ) return;
        this._disposed = true;
        await client.DisposeAsync();
        GC.SuppressFinalize( this );
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if( this._disposed ) return;
        this._disposed = true;
        client.Dispose();
        GC.SuppressFinalize( this );
    }
}
