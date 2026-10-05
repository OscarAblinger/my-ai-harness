using System.Collections.Generic;
using System.Threading;

namespace Ablinger.MyAiHarness.UI.Services;

/// <summary>
/// Produces an assistant reply for a given prompt. The mock implementation backs the UI until a
/// real backend (see <c>PromptProcessor</c> in my-ai-harness-core) is wired up.
/// </summary>
public interface IChatReplySource
{
    /// <summary>
    /// Streams the reply for <paramref name="prompt"/> as markdown chunks, in order.
    /// </summary>
    IAsyncEnumerable<string> StreamReplyAsync(string prompt, CancellationToken cancellationToken = default);
}
