using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sparkl.Core;

public sealed class SnakeCaseEnumConverter<T>
    : JsonConverter<T>
    where T : struct, Enum
{
    public override T Read(
        ref Utf8JsonReader    reader,
        Type                  typeToConvert,
        JsonSerializerOptions options
    )
    {
        if( reader.TokenType != JsonTokenType.String )
            throw new JsonException( $"Expected string for enum {typeof( T ).Name} value." );

        var text = reader.GetString();
        if( String.IsNullOrEmpty( text ) )
            throw new JsonException( $"Invalid {typeof( T ).Name} value: {text}" );

        var pascal = SnakeCaseEnumConverter<T>.SnakeToPascal( text );

        if( Enum.TryParse( pascal, false, out T value ) )
            return value;

        throw new JsonException(
            $"Invalid {typeof( T ).Name} value: {text}"
        );
    }

    public override void Write(
        Utf8JsonWriter        writer,
        T                     value,
        JsonSerializerOptions options
    )
    {
        var snake = SnakeCaseEnumConverter<T>.PascalToSnake( value.ToString() );
        writer.WriteStringValue( snake );
    }

    private static string SnakeToPascal( string snake )
    {
        var parts = snake.Split( '_', StringSplitOptions.RemoveEmptyEntries );
        return String.Concat(
            parts.Select( p => {
                    return p.Length switch
                    {
                        0 => String.Empty,
                        1 => Char.ToUpperInvariant( p[0] ).ToString(),
                        _ => Char.ToUpperInvariant( p[0] ) + p[1..].ToLowerInvariant(),
                    };
                }
            )
        );
    }

    private static string PascalToSnake( string pascal )
    {
        var sb = new StringBuilder();
        for( var i = 0; i < pascal.Length; i++ )
        {
            var c = pascal[i];
            if( Char.IsUpper( c ) && i > 0 )
                sb.Append( '_' );

            sb.Append( Char.ToLowerInvariant( c ) );
        }

        return sb.ToString();
    }
}
