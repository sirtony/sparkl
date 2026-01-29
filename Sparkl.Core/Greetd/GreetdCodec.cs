using System.Buffers.Binary;
using System.Text.Json;

namespace Sparkl.Core.Greetd;

internal static class GreetdCodec
{
    public static async ValueTask SendAsync<T>(
        Stream            destination,
        T                 obj,
        CancellationToken cancellationToken = default
    )
    {
        await using var memoryStream = new MemoryStream();
        await JsonSerializer.SerializeAsync( memoryStream, obj, GreetdJson.Options, cancellationToken );
        await memoryStream.FlushAsync( cancellationToken );
        memoryStream.Seek( 0, SeekOrigin.Begin );

        var lenBuf = new byte[sizeof( uint )];
        if( BitConverter.IsLittleEndian )
            BinaryPrimitives.WriteUInt32LittleEndian( lenBuf, (uint)memoryStream.Length );
        else
            BinaryPrimitives.WriteUInt32BigEndian( lenBuf, (uint)memoryStream.Length );

        await destination.WriteAsync( lenBuf, cancellationToken );
        await memoryStream.CopyToAsync( destination, cancellationToken );
        await destination.FlushAsync( cancellationToken );
    }

    public static async ValueTask<T> ReceiveAsync<T>( Stream source, CancellationToken cancellationToken = default )
    {
        const int maxPayloadLength = 4 * 1024; // greetd's responses are small, more than enough

        var lenBuf = new byte[sizeof( uint )];
        await source.ReadExactlyAsync( lenBuf, cancellationToken );

        var length = BitConverter.IsLittleEndian
                         ? BinaryPrimitives.ReadUInt32LittleEndian( lenBuf )
                         : BinaryPrimitives.ReadUInt32BigEndian( lenBuf );

        if( length > maxPayloadLength )
            throw new InvalidOperationException( "message payload exceeds maximum permissible length" );

        var msgBuf = new byte[length];
        await source.ReadExactlyAsync( msgBuf, cancellationToken );

        return JsonSerializer.Deserialize<T>( msgBuf, GreetdJson.Options )!;
    }
}
