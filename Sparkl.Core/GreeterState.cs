namespace Sparkl.Core;

internal enum GreeterState : byte
{
    Connected,
    Authenticating,
    StartingSession,
    Cancelling,
    Finished,
}
