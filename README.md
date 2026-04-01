# Remote Desktop System (.NET 8)

A modular, secure, and high-performance remote desktop system built with .NET 8 WPF.

## Architecture

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│  HostAgent  │────▶│  Signaling   │◀────│  ClientView │
│   (WPF)     │     │   Server     │     │    (WPF)    │
└─────────────┘     └──────────────┘     └─────────────┘
       │                                        │
       └──────────── Direct TCP ────────────────┘
```

## Projects

- **Core** - Core functionality: screen capture, image encoding, input injection, networking, protocol
- **Shared** - Shared protocol messages and contracts
- **HostAgent** - WPF app for host (captures and shares desktop)
- **ClientViewer** - WPF app for client (views and controls remote desktop)
- **SignalingServer** - ASP.NET Core SignalR server for session coordination

## Features

- ✅ Screen capture using Win32 API (BitBlt)
- ✅ JPEG image encoding with quality control
- ✅ Mouse and keyboard input injection
- ✅ TCP networking with custom protocol
- ✅ SignalR-based signaling for session management
- ✅ Real-time desktop streaming (~10 FPS)
- ✅ Remote control with mouse/keyboard forwarding
- ✅ Self-contained .exe files (no .NET installation required)

## Quick Start (Pre-built .exe Files)

### For PC that wants to share screen (Host):
1. Go to `build/publish/HostAgent/`
2. Run `HostAgent.exe`
3. Enter a session ID and port (default: 5000)
4. Click "Start Sharing"

### For PC that wants to view (Client):
1. Go to `build/publish/ClientViewer/`
2. Run `ClientViewer.exe`
3. Enter the host's IP address
4. Click "Connect"

**Important:** Copy the entire folder (HostAgent or ClientViewer), not just the .exe file!

## Build & Run (From Source)

### Prerequisites
- .NET 8 SDK
- Windows 10/11

### Build
```bash
dotnet build
```

### Run

1. **Start the Signaling Server** (in one terminal):
```bash
cd SignalingServer
dotnet run
```

2. **Start the Host Agent** (on the host computer):
```bash
cd HostAgent
dotnet run
```
- Enter a session ID (e.g., "my-desktop")
- Click "Start Sharing"
- The host will wait for clients

3. **Start the Client Viewer** (on the client computer):
```bash
cd ClientViewer
dotnet run
```
- Enter the host's IP address and port
- Click "Connect"
- Enable "Remote Control" to control the host's computer

## Direct Connection Mode

You can also connect directly without the signaling server:

1. Start HostAgent, enter session ID, click "Start Sharing"
2. Note the port shown in status
3. In ClientViewer, enter the host's IP address and the port
4. Click "Connect"

## Protocol

Messages are JSON-based with length-prefix framing:

| Type | Description |
|------|-------------|
| 1 | ScreenFrame - JPEG image data |
| 2 | MouseMove - Cursor position |
| 3 | MouseClick - Button events |
| 4 | MouseScroll - Wheel events |
| 5 | KeyPress - Keyboard events |
| 6 | TextInput - Unicode text |
| 7 | SessionInfo - Session metadata |

## Security Notes

- This is a demonstration project
- For production use, add encryption (TLS/wire encryption)
- Add authentication and authorization
- Consider rate limiting and input validation
- Add consent mechanism for host

## License

MIT