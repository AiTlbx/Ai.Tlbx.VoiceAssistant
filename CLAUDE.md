# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Important Shell Commands

### Common Development Commands

Build the entire solution:
```bash
dotnet build TLBX.Ai.VoiceAssistant.slnx
```

Build and generate NuGet packages:
```bash
dotnet pack TLBX.Ai.VoiceAssistant.slnx
```

Run specific demo applications:
```bash
# Windows Demo
dotnet run --project Demo/Ai.Tlbx.VoiceAssistant.Demo.Windows/Ai.Tlbx.VoiceAssistant.Demo.Windows.csproj

# Linux Demo
dotnet run --project Demo/Ai.Tlbx.VoiceAssistant.Demo.Linux/Ai.Tlbx.VoiceAssistant.Demo.Linux.csproj

# Web Demo
dotnet run --project Demo/Ai.Tlbx.VoiceAssistant.Demo.Web/Ai.Tlbx.VoiceAssistant.Demo.Web.csproj
```

Clean build artifacts:
```bash
dotnet clean TLBX.Ai.VoiceAssistant.slnx
```

Publish NuGet packages (auto-increment version):
```bash
pwsh publish-nuget.ps1
```

Deep clean of bin/obj folders:
```bash
cleanBinObj.cmd
```

Publish without version increment:
```bash
pwsh publish-nuget-current-version.ps1
```

Sign NuGet packages (requires certificate):
```bash
pwsh sign-packages.ps1 -CertPath "path/to/cert.pfx" -CertPassword "password"
```

## High-Level Architecture (v4.0)

This is a voice assistant toolkit that provides a modular architecture for integrating with multiple AI providers. The v4.0 redesign introduces a composition-based architecture with clean separation between orchestration and provider implementations.

### Core Components

1. **Main Orchestrator** (`Provider/Ai.Tlbx.VoiceAssistant/`)
   - `VoiceAssistant`: Main orchestrator class that coordinates between hardware and AI providers
   - Provider-agnostic interfaces: `IVoiceProvider`, `IVoiceSettings`, `IVoiceTool`
   - Fluent DI configuration via `VoiceAssistantBuilder`
   - Thread-safe `ChatHistoryManager` for conversation state

2. **Provider Implementations** 
   - **OpenAI** (`Provider/Ai.Tlbx.VoiceAssistant.Provider.OpenAi/`): WebSocket-based real-time API
   - Future: Google, xAI, and custom providers
   - Each provider implements `IVoiceProvider` interface

3. **Hardware Abstraction** (`IAudioHardwareAccess` interface)
   - Defines contract for platform-specific audio operations
   - Key methods: `InitAudio()`, `StartRecordingAudio()`, `PlayAudio()`, `GetAvailableMicrophones()`
   - Each platform provides its own implementation

4. **Platform Implementations**
   - **Windows** (`Hardware/Ai.Tlbx.VoiceAssistant.Hardware.Windows/`): Uses NAudio library
   - **Linux** (`Hardware/Ai.Tlbx.VoiceAssistant.Hardware.Linux/`): Direct ALSA integration via P/Invoke
   - **Web** (`Hardware/Ai.Tlbx.VoiceAssistant.Hardware.Web/`): JavaScript interop with Web Audio API and AudioWorklet

5. **UI Components** (`WebUi/Ai.Tlbx.VoiceAssistant.WebUi/`)
   - Reusable Blazor components for audio controls and chat interfaces
   - Platform-agnostic UI elements

### Audio Processing Flow

1. Platform-specific hardware captures audio as PCM 16-bit format
2. Audio is base64 encoded and forwarded to the AI provider
3. AI responses (text and audio) are received and processed
4. Audio responses are decoded and played through platform hardware
5. Callbacks notify UI of status changes and new messages

### Key Design Patterns

- **Composition Pattern**: VoiceAssistant orchestrator composes with pluggable providers
- **Strategy Pattern**: Platform implementations via `IAudioHardwareAccess`
- **Dependency Injection**: Fluent builder pattern for DI configuration
- **Template Method**: `IVoiceTool` for custom AI tool extensions

### Package Distribution

The solution produces multiple NuGet packages (output to `nupkg/`):
- `Ai.Tlbx.VoiceAssistant`: Core orchestrator and interfaces
- `Ai.Tlbx.VoiceAssistant.Provider.OpenAi`: OpenAI provider implementation
- `Ai.Tlbx.VoiceAssistant.Hardware.Windows`: Windows audio support
- `Ai.Tlbx.VoiceAssistant.Hardware.Linux`: Linux audio support
- `Ai.Tlbx.VoiceAssistant.Hardware.Web`: Web/Blazor audio support
- `Ai.Tlbx.VoiceAssistant.WebUi`: Reusable UI components

Version is tracked in `version.txt` and auto-incremented by `publish-nuget.ps1`.

### Logging Strategy

This codebase uses a **centralized logging architecture** detailed in [`LoggingStrategy.md`](LoggingStrategy.md). 

**⚠️ CRITICAL: DO NOT USE ILogger<T> OR Microsoft.Extensions.Logging**

All logging flows from lower layers up to the orchestrator where users configure their preferred logging approach. This pattern:
- Maintains clean architecture boundaries
- Allows user choice of logging framework
- Simplifies testing and debugging
- Prevents tight coupling to Microsoft logging

Always use `Action<LogLevel, string>` for logging delegation and forward logs up through natural architectural layers.

### Important Notes

- The project targets .NET 9.0
- Version 4.0 introduces breaking changes from v3.x
- No automated tests are currently included - testing is done via demo applications
- Windows components require Windows 10 or later
- Linux components require libasound (ALSA) to be installed
- Web components require HTTPS or localhost for microphone access due to browser security
- JavaScript audio worklet processors handle real-time audio processing in web implementation
- Solution uses the new .slnx format (TLBX.Ai.VoiceAssistant.slnx)
- Publishing requires NUGET_API_KEY environment variable for NuGet.org uploads