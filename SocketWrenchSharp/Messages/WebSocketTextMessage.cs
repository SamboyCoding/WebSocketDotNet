﻿using System.Text;
using SocketWrenchSharp.Protocol;

namespace SocketWrenchSharp.Messages;

/// <summary>
/// Represents a websocket message with opcode 0x1 (text).
/// Contains a UTF-8 encoded string payload. 
/// </summary>
public class WebSocketTextMessage : WebSocketMessage
{
    public string Text { get; private set; }

    public WebSocketTextMessage(string text)
    {
        Text = text;
    }

    public override WebSocketOpcode OpcodeToSend => WebSocketOpcode.Text;
    public override void ReadData(byte[] payload) => Text = Encoding.UTF8.GetString(payload);
    public override byte[] GetPayload() => Encoding.UTF8.GetBytes(Text);
}