using System.Net.Sockets;

namespace Sparkl.Core.Greetd;

public sealed class GreetdClient : IDisposable, IAsyncDisposable
{
    private readonly Socket _socket;
    private readonly Stream _socketStream;

    private GreetdClient( Socket socket )
    {
        this._socket       = socket;
        this._socketStream = new NetworkStream( socket, true );
    }

    public async ValueTask SendAsync( GreetdRequest request, CancellationToken cancellationToken = default )
        => await GreetdCodec.SendAsync( this._socketStream, request, cancellationToken );

    public ValueTask<T> ReceiveAsync<T>( CancellationToken cancellationToken = default )
        => GreetdCodec.ReceiveAsync<T>( this._socketStream, cancellationToken );

    public static async ValueTask<GreetdClient> ConnectAsync( CancellationToken cancellationToken = default )
    {
        const string varName = "GREETD_SOCK";

        var socketPath = Environment.GetEnvironmentVariable( varName );

        if( String.IsNullOrWhiteSpace( socketPath ) )
            throw new InvalidOperationException( $"`{varName}` environment variable not set" );

        return await GreetdClient.ConnectAsync( socketPath, cancellationToken );
    }

    public static async ValueTask<GreetdClient> ConnectAsync(
        string            socketPath,
        CancellationToken cancellationToken = default
    )
    {
        var socket   = new Socket( AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified );
        var endpoint = new UnixDomainSocketEndPoint( socketPath );
        await socket.ConnectAsync( endpoint, cancellationToken );
        return new( socket );
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await this._socketStream.DisposeAsync();

    /// <inheritdoc />
    public void Dispose() => this._socketStream.Dispose();
}
