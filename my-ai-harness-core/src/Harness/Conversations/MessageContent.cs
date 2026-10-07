namespace Ablinger.MyAiHarness.Core.Harness.Conversations;

public struct MessageContent
{
    /// <summary>
    /// All content has to be representable by text.
    /// For images or other files, this should be a file URI to the content.
    /// </summary>
    public string Text { get; init; }
    
    public MessageContentType Type { get; init; }
    
    public enum MessageContentType
    {
        /// <summary>
        /// Standard Markdown text.
        /// </summary>
        Markdown,
        /// <summary>
        /// Local file. The <c>Text</c> should then be a MahFilePath or absolute file path. 
        /// </summary>
        LocalFile,
        /// <summary>
        /// URI to a file. Can be a https link, but also any other valid URI.
        /// </summary>
        FileLink,
    }
}