using System.Collections.Immutable;

namespace Sparkl.Core;

public sealed record Session(
    SessionKind            Kind,
    string                 Name,
    string                 Command,
    ImmutableArray<string> Environment
)
{
    public static Session FromCommand( string command, IEnumerable<string>? environment = null )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace( command );
        ImmutableArray<string> env = environment is null ? [] : [..environment];

        return new( SessionKind.Unknown, command, command, env );
    }
}
