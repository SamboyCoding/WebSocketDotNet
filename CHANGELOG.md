v1.0.0
- Moved configuration of Websocket options to a new WebSocketConfiguration struct and added a new constructor which takes it.
  - The old constructor is still available but marked as Obsolete.
- Added the ability to specify the websocket chunking mode (default, and previous behavior, is to chunk so that the length of a packet is always less than 65536 bytes)
- Fixed invalid data being generated, causing a disconnect, when messages > 65535 bytes were sent.

v0.5.5
- Hard reset the underlying TCP client when reconnecting.

v0.5.4
- Attempt to fix reconnection behavior

v0.5.3
- Fixed some edge cases in error handling

v0.5.2
- Better and unified error handling, especially when closing the connection 
- Code cleanup

v0.5.1
- Build nuget package deterministically

v0.5.0
- Initial release