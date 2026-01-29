using System.Text.Json.Serialization;

namespace Sparkl.Core.Greetd;

[JsonConverter( typeof( SnakeCaseEnumConverter<AuthMessageType> ) )]
public enum AuthMessageType
{
    Visible,
    Secret,
    Info,
    Error,
}
