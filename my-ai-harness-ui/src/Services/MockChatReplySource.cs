using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace my_ai_harness_ui.Services;

/// <summary>
/// Fake reply source used until the harness backend is connected. It streams a canned markdown
/// reply sentence by sentence (no quoting of the prompt) so the streaming path — and code-block
/// highlighting — is exercised end to end.
/// </summary>
public sealed class MockChatReplySource : IChatReplySource
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan ChunkDelay = TimeSpan.FromMilliseconds(140);

    private static readonly string[] Replies =
    [
        """
        I'm a mock reply — the real harness backend isn't wired up yet. Here's what the composer
        supports today:

        - Markdown is sent **as source text**; rendered markdown lives in this pane.
        - Fenced code blocks are highlighted per language.
        - `Ctrl + Enter` sends, plain `Enter` inserts a newline.

        ```csharp
        public sealed class Greeter(string greeting)
        {
            public string Greet(string name) => $"{greeting}, {name}!";
        }
        ```
        """,
        """
        Consider this a placeholder response. When the real backend lands, this pane will render
        the model output incrementally as it arrives.

        | Piece        | Status     |
        | ------------ | ---------- |
        | Composer     | done       |
        | Transcript   | done       |
        | Backend      | mock       |

        Nothing here is persisted — every session starts clean.
        """,
        """
        Still mock data, but with enough markdown to prove the renderer out:

        1. Headings, lists and **emphasis** render natively.
        2. Inline `code` gets its own styling.
        3. Blockquotes are indented:

        > The stream is chunked on purpose: it mimics token-by-token model output.

        ```python
        def greet(name: str) -> str:
            return f"Hello, {name}!"
        ```
        """,
    ];

    private int _replyIndex;

    public async IAsyncEnumerable<string> StreamReplyAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(InitialDelay, cancellationToken);

        var reply = Replies[_replyIndex++ % Replies.Length];
        foreach (var chunk in SplitIntoChunks(reply))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(ChunkDelay, cancellationToken);
            yield return chunk;
        }
    }

    /// <summary>Splits markdown into sentence-sized chunks without altering the final text.</summary>
    private static IEnumerable<string> SplitIntoChunks(string text)
    {
        var chunk = new StringBuilder();
        foreach (var character in text)
        {
            chunk.Append(character);

            var isBoundary = character switch
            {
                '\n' => true,
                '.' or '?' or '!' => chunk.Length > 40,
                _ => false,
            };

            if (isBoundary)
            {
                yield return chunk.ToString();
                chunk.Clear();
            }
        }

        if (chunk.Length > 0)
            yield return chunk.ToString();
    }
}
