using System.Text.Json.Serialization;

namespace Sparkl.Core.Greetd;

[JsonPolymorphic( TypeDiscriminatorPropertyName = "type" )]
[JsonDerivedType( typeof( Success ),     "success" )]
[JsonDerivedType( typeof( Error ),       "error" )]
[JsonDerivedType( typeof( AuthMessage ), "auth_message" )]
public abstract record GreetdResponse
{
    public sealed record Success : GreetdResponse;

    public sealed record Error(
        [property: JsonPropertyName( "error_type" )]
        ErrorType Type,
        [property: JsonPropertyName( "description" )]
        string Message
    ) : GreetdResponse;

    public sealed record AuthMessage(
        [property: JsonPropertyName( "auth_message_type" )]
        AuthMessageType Type,
        [property: JsonPropertyName( "auth_message" )]
        string Message
    ) : GreetdResponse;

    private GreetdResponse() { }
}
