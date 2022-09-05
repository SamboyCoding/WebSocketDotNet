namespace WebSocketDotNet;

public enum MessageChunkingMode
{
    /// <summary>
    /// Don't chunk messages unless they go over the max integer limit (very unlikely). This limit is imposed by the .NET framework.
    /// </summary>
    AlwaysUseExtendedLength,
    
    /// <summary>
    /// Avoid using the 64-bit length field in outgoing packets - chunk messages if they are larger than 65535 bytes. The default behavior.
    /// </summary>
    LimitTo16BitExtendedLength,
    
    /// <summary>
    /// Avoid using the extended length field at all - chunk messages if they are larger than 125 bytes
    /// </summary>
    NeverUseExtendedLength,
}