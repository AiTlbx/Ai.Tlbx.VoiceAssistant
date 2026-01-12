# Ai.Tlbx.VoiceAssistant.Provider.OpenAi

OpenAI Realtime API provider for the AI Voice Assistant Toolkit.

[![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Provider.OpenAi.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Provider.OpenAi/)

## Installation

```bash
dotnet add package Ai.Tlbx.VoiceAssistant.Provider.OpenAi
```

## Usage

```csharp
var provider = factory.CreateOpenAi(apiKey);
var settings = new OpenAiVoiceSettings
{
    Voice = AssistantVoice.Alloy,
    Instructions = "You are a helpful assistant."
};

var assistant = new VoiceAssistant(provider, audioHardware);
await assistant.StartAsync(settings);
```

## Full Documentation

See the main package for complete documentation:

**[Ai.Tlbx.VoiceAssistant on NuGet](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant#readme-body-tab)**
