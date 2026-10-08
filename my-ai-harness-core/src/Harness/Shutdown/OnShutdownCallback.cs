using System.Threading;

namespace Ablinger.MyAiHarness.Core.Harness.Shutdown;

public sealed class OnHarnessShutdownCallback
{
    public event ShutdownHandler? OnShutdown;

    internal void SignalShutdown(ShutdownHandlerArgs args)
    {
        OnShutdown?.Invoke(this, args);
    }
}

public delegate void ShutdownHandler(object sender, ShutdownHandlerArgs args);

public record struct ShutdownHandlerArgs(CancellationToken CancellationToken);
