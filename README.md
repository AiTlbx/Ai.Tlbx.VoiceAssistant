# Ai.Tlbx.VoiceAssistant

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)

A modular toolkit for building voice assistant applications in .NET with support for multiple AI providers.

## Version 4.0.0

This major release introduces a complete architectural redesign:
- **Multi-Provider Support**: Pluggable architecture supporting OpenAI, Google, xAI, and custom providers
- **Composition-Based Design**: Clean separation between orchestration and provider implementations
- **Dependency Injection First**: Fluent DI pattern for seamless integration with modern .NET applications
- **Provider Agnostic**: Core interfaces and models work with any AI provider

## GitHub Repository

[https://github.com/AiTlbx/Ai.Tlbx.VoiceAssistant](https://github.com/AiTlbx/Ai.Tlbx.VoiceAssistant)

## Packages

### Core Package

#### Ai.Tlbx.VoiceAssistant

[![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant/)

The core orchestrator package containing interfaces and base implementations for voice assistant functionality.

```bash
dotnet add package Ai.Tlbx.VoiceAssistant
```

### Provider Packages

#### Ai.Tlbx.VoiceAssistant.Provider.OpenAi

[![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Provider.OpenAi.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Provider.OpenAi/)

OpenAI provider implementation supporting real-time conversation via WebSocket API.

```bash
dotnet add package Ai.Tlbx.VoiceAssistant.Provider.OpenAi
```

### Hardware Packages

Platform-specific audio hardware implementations:

#### Ai.Tlbx.VoiceAssistant.Hardware.Windows

[![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Hardware.Windows.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Hardware.Windows/)

Windows audio hardware integration using NAudio.

```bash
dotnet add package Ai.Tlbx.VoiceAssistant.Hardware.Windows
```

#### Ai.Tlbx.VoiceAssistant.Hardware.Linux

[![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Hardware.Linux.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Hardware.Linux/)

Linux audio hardware integration using ALSA.

```bash
dotnet add package Ai.Tlbx.VoiceAssistant.Hardware.Linux
```

#### Ai.Tlbx.VoiceAssistant.Hardware.Web

[![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Hardware.Web.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Hardware.Web/)

Web audio hardware integration for Blazor applications using Web Audio API.

```bash
dotnet add package Ai.Tlbx.VoiceAssistant.Hardware.Web
```

### UI Components

#### Ai.Tlbx.VoiceAssistant.WebUi

[![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.WebUi.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.WebUi/)

Reusable Blazor UI components for voice assistant applications.

```bash
dotnet add package Ai.Tlbx.VoiceAssistant.WebUi
```

## Quick Start

### 1. Configure Dependency Injection

```csharp
using Ai.Tlbx.VoiceAssistant.Extensions;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi.Extensions;

// In your Program.cs or Startup.cs
services.AddVoiceAssistant()
    .WithHardware<WindowsAudioHardware>()  // Or LinuxAudioDevice, WebAudioAccess
    .WithOpenAi();  // Or future providers like WithGoogle(), WithXAi()
```

### 2. Use in Your Application

```csharp
public class VoiceService
{
    private readonly VoiceAssistant _voiceAssistant;
    
    public VoiceService(VoiceAssistant voiceAssistant)
    {
        _voiceAssistant = voiceAssistant;
    }
    
    public async Task StartConversation()
    {
        var settings = new OpenAiVoiceSettings
        {
            Instructions = "You are a helpful assistant.",
            Voice = AssistantVoice.Alloy,
            Model = "gpt-4o-realtime-preview-2025-06-03"
        };
        
        await _voiceAssistant.StartAsync(settings);
    }
}
```

### 3. Handle Events

```csharp
_voiceAssistant.OnMessageAdded = (message) =>
{
    Console.WriteLine($"{message.Role}: {message.Content}");
};

_voiceAssistant.OnConnectionStatusChanged = (status) =>
{
    Console.WriteLine($"Status: {status}");
};
```

## Architecture

The toolkit follows a composition-based architecture:

```
VoiceAssistant (orchestrator)
├── IVoiceProvider (AI provider interface)
│   ├── OpenAiVoiceProvider
│   ├── GoogleVoiceProvider (future)
│   └── XAiVoiceProvider (future)
├── IAudioHardwareAccess (hardware interface)
│   ├── WindowsAudioHardware
│   ├── LinuxAudioDevice
│   └── WebAudioAccess
└── ChatHistoryManager (conversation state)
```

## Key Features

- **Multi-Provider Support**: Easily switch between AI providers
- **Cross-Platform Audio**: Works on Windows, Linux, and Web
- **Real-Time Processing**: Low-latency audio streaming
- **Dependency Injection**: First-class DI support
- **Type-Safe Configuration**: Strongly-typed settings per provider
- **Extensible Tool System**: Add custom tools for AI assistants
- **Thread-Safe Chat History**: Built-in conversation management

## Requirements

- .NET 9.0 or higher
- Platform-specific requirements:
  - **Windows**: Windows 10 or later
  - **Linux**: ALSA libraries (`libasound2`)
  - **Web**: Modern browser with Web Audio API support

## Environment Variables

- `OPENAI_API_KEY`: Required for OpenAI provider

## Migration from v3.x

Version 4.0 introduces breaking changes:

1. Package names have changed from `Ai.Tlbx.RealTimeAudio.*` to `Ai.Tlbx.VoiceAssistant.*`
2. `OpenAiRealTimeApiAccess` is replaced by `VoiceAssistant` + `OpenAiVoiceProvider`
3. Event-based callbacks replaced with simpler `Action` properties
4. New fluent DI configuration pattern

See the migration guide in the documentation for detailed instructions.

## Contributing

Contributions are welcome! Please see our contributing guidelines.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

- Built on top of NAudio for Windows audio
- Uses ALSA for Linux audio support
- Leverages Web Audio API for browser-based audio