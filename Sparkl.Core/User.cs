namespace Sparkl.Core;

public sealed record User( string Name, string HomeDirectory )
{
    public static User FromName( string name ) => new( name, Path.Combine( "/home", name ) );
}
