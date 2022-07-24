namespace SocketWrenchSharp;

public enum WebSocketCloseCode : ushort
{
    /// <summary>
    /// (Out-of-spec) When sending a close frame, indicates that no close code should be sent.
    /// </summary>
    Unspecified = 0,
    
    /// <summary>
    /// The connection was closed normally by both endpoints sending a close message.
    /// </summary>
    ClosedOk = 1000,
    
    /// <summary>
    /// The endpoint is "going away", e.g. server is shutting down.
    /// </summary>
    GoingAway = 1001,
    
    /// <summary>
    /// A protocol error occurred.
    /// </summary>
    ProtocolError = 1002,
    
    /// <summary>
    /// The endpoint is terminating the connection because it has received a type of data it cannot accept.
    /// </summary>
    UnsupportedData = 1003,
    
    /// <summary>
    /// Reserved for future use.
    /// </summary>
    Reserved = 1004,
    
    /// <summary>
    /// Not sent on the wire. Indicates that a close message was received without a status.
    /// </summary>
    NoStatus = 1005,
    /// <summary>
    /// Not sent on the wire. Indicates that the connection was closed unexpectedly (i.e. without a close message)
    /// </summary>
    AbnormalClosure = 1006,
    
    /// <summary>
    /// The endpoint is terminating the connection because the type of data specified by the opcode did not match the type of data in the payload.
    /// </summary>
    MismatchTypeAndPayload = 1007,
    
    /// <summary>
    /// The endpoint is terminating the connection because the data sent violated its policy.
    /// </summary>
    PolicyViolation = 1008,
    
    /// <summary>
    /// The endpoint is terminating the connection because it received a message that was too large.
    /// </summary>
    MessageTooBig = 1009,
    
    /// <summary>
    /// The endpoint is terminating the connection because it requires the use of an extension which was not negotiated.
    /// </summary>
    MissingMandatoryExtension = 1010,
    
    /// <summary>
    /// The endpoint is terminating the connection because it encountered an error.
    /// </summary>
    InternalError = 1011,
    
    /// <summary>
    /// The connection failed due to a failure to perform a TLS handshake (e.g. the server certificate is invalid).
    /// </summary>
    TlsHandshakeFailure = 1015,
}