# Ai.Tlbx.VoiceAssistant.Provider.Google

Google Gemini Live API provider for the AI Voice Assistant Toolkit.

[![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Provider.Google.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Provider.Google/)

## Installation

```bash
dotnet add package Ai.Tlbx.VoiceAssistant.Provider.Google
```

## Usage

```csharp
var provider = factory.CreateGoogle(apiKey);
var settings = new GoogleVoiceSettings
{
    Voice = GeminiVoice.Puck,
    Instructions = "You are a helpful assistant."
};

var assistant = new VoiceAssistant(provider, audioHardware);
await assistant.StartAsync(settings);
```

## Full Documentation

See the main package for complete documentation:

**[Ai.Tlbx.VoiceAssistant on NuGet](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant#readme-body-tab)**
