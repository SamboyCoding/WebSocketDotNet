namespace WebSocketDotNet.Protocol;

public enum WebSocketOpcode : byte
{
    /// <summary>
    /// A continuation frame. Contains data carried over from the previous frame - the payloads should be concatenated.
    /// If the fin bit is not set, another continuation frame is expected. If it is set, the frame is complete and can be parsed.
    ///
    /// Can only be used for data frames - control frames must not be fragmented, but can be sent in between continuation frames.
    /// </summary>
    Continuation = 0x00,
    
    /// <summary>
    /// UTF8-encoded text data frame.
    /// </summary>
    Text = 0x01,
    /// <summary>
    /// Raw binary data frame, up to the application to interpret.
    /// </summary>
    Binary = 0x02,
    
    ReservedData3 = 0x03,
    ReservedData4 = 0x04,
    ReservedData5 = 0x05,
    ReservedData6 = 0x06,
    ReservedData7 = 0x07,
    
    /// <summary>
    /// Control frame which indicates the connection is to be closed. The receiving end should respond with a frame with this same opcode, and the
    /// end which receives that response should then close the connection.
    /// </summary>
    Close = 0x08,
    
    /// <summary>
    /// A ping control frame to ensure the connection is still alive. The receiving end should respond with a pong control frame.
    /// </summary>
    Ping = 0x09,
    
    /// <summary>
    /// A pong control frame, to be sent in response to a ping control frame.
    /// </summary>
    Pong = 0x0A,
    
    ReservedControlB = 0x0B,
    ReservedControlC = 0x0C,
    ReservedControlD = 0x0D,
    ReservedControlE = 0x0E,
}