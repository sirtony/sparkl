using System.Text.Json.Serialization;

namespace Sparkl.Core.Greetd;

[JsonConverter( typeof( SnakeCaseEnumConverter<ErrorType> ) )]
public enum ErrorType
{
    Error,
    AuthError,
}
