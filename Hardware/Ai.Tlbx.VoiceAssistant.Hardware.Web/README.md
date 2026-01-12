# Ai.Tlbx.VoiceAssistant.Hardware.Web

Browser audio hardware support for Blazor applications in the AI Voice Assistant Toolkit.

[![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Hardware.Web.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Hardware.Web/)

## Installation

```bash
dotnet add package Ai.Tlbx.VoiceAssistant.Hardware.Web
```

## Features

- **48kHz capture** with browser echo cancellation, noise suppression, and auto gain
- **De-esser** (high-shelf EQ) to tame sibilance before amplification
- **Compressor** (8:1 ratio) for consistent loudness across whispers and shouts
- **Anti-aliasing filter** (Butterworth LPF) before downsampling
- **Provider-specific sample rates**: 16kHz for Google, 24kHz for OpenAI/xAI

## Requirements

- Modern browser with Web Audio API support
- HTTPS or localhost (required for microphone access)
- .NET 9.0 or .NET 10.0

## Usage

```csharp
// In Program.cs
builder.Services.AddScoped<IAudioHardwareAccess, WebAudioAccess>();

// In your Blazor component
@inject IAudioHardwareAccess AudioHardware

var provider = factory.CreateOpenAi(apiKey);
var assistant = new VoiceAssistant(provider, AudioHardware);
await assistant.StartAsync(settings);
```

## Full Documentation

See the main package for complete documentation:

**[Ai.Tlbx.VoiceAssistant on NuGet](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant#readme-body-tab)**
