using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Sparkl.Core.Greetd;

[JsonPolymorphic( TypeDiscriminatorPropertyName = "type" )]
[JsonDerivedType( typeof( CreateSession ),           "create_session" )]
[JsonDerivedType( typeof( PostAuthMessageResponse ), "post_auth_message_response" )]
[JsonDerivedType( typeof( StartSession ),            "start_session" )]
[JsonDerivedType( typeof( CancelSession ),           "cancel_session" )]
public abstract record GreetdRequest
{
    public sealed record CreateSession( string Username ) : GreetdRequest;

    public sealed record PostAuthMessageResponse( string? Response ) : GreetdRequest;

    public sealed record StartSession(
        [property: JsonPropertyName( "cmd" )] ImmutableArray<string> Command,
        [property: JsonPropertyName( "env" )] ImmutableArray<string> Environment
    ) : GreetdRequest;

    public sealed record CancelSession : GreetdRequest;

    private GreetdRequest() { }
}
