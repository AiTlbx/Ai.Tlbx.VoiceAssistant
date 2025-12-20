# AI Voice Assistant Toolkit

**Real-time voice conversations with AI in .NET — OpenAI, Google Gemini, and xAI Grok in one unified API.**

[![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.svg?label=nuget&color=blue)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant/)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![.NET 9 | 10](https://img.shields.io/badge/.NET-9.0_|_10.0-purple.svg)](https://dotnet.microsoft.com/)

---

## Quick Start (Blazor Server)

**1. Install packages:**
```bash
dotnet add package Ai.Tlbx.VoiceAssistant
dotnet add package Ai.Tlbx.VoiceAssistant.Provider.OpenAi   # and/or .Google, .XAi
dotnet add package Ai.Tlbx.VoiceAssistant.Hardware.Web
```

**2. Add API keys** (`appsettings.json`):
```json
{
  "VoiceProviders": {
    "OpenAI": "sk-...",
    "Google": "AIza...",
    "xAI": "xai-..."
  }
}
```

**3. Configure services** (`Program.cs`):
```csharp
// Register provider factory and audio hardware
builder.Services.AddSingleton<IVoiceProviderFactory, VoiceProviderFactory>();
builder.Services.AddScoped<IAudioHardwareAccess, WebAudioAccess>();
```

**4. Create a voice page** (`Voice.razor`):
```razor
@page "/voice"
@inject IVoiceProviderFactory ProviderFactory
@inject IAudioHardwareAccess AudioHardware
@inject IConfiguration Config

<select @bind="_selectedProvider">
    <option value="openai">OpenAI</option>
    <option value="google">Google Gemini</option>
    <option value="xai">xAI Grok</option>
</select>

<button @onclick="Toggle">@(_assistant?.IsRecording == true ? "Stop" : "Talk")</button>

@foreach (var msg in _messages)
{
    <p><b>@msg.Role:</b> @msg.Content</p>
}

@code {
    private VoiceAssistant? _assistant;
    private List<ChatMessage> _messages = new();
    private string _selectedProvider = "openai";

    private async Task Toggle()
    {
        if (_assistant?.IsRecording == true)
        {
            await _assistant.StopAsync();
            return;
        }

        // Create provider based on selection
        var (provider, settings) = _selectedProvider switch
        {
            "openai" => (
                ProviderFactory.CreateOpenAi(Config["VoiceProviders:OpenAI"]!),
                (IVoiceSettings)new OpenAiVoiceSettings { Instructions = "You are helpful." }
            ),
            "google" => (
                ProviderFactory.CreateGoogle(Config["VoiceProviders:Google"]!),
                (IVoiceSettings)new GoogleVoiceSettings { Instructions = "You are helpful." }
            ),
            "xai" => (
                ProviderFactory.CreateXai(Config["VoiceProviders:xAI"]!),
                (IVoiceSettings)new XaiVoiceSettings { Instructions = "You are helpful." }
            ),
            _ => throw new InvalidOperationException()
        };

        _assistant = new VoiceAssistant(provider, AudioHardware);
        _assistant.OnMessageReceived = msg => InvokeAsync(() => { _messages.Add(msg); StateHasChanged(); });

        await _assistant.StartAsync(settings);
    }
}
```

**That's it.** Select a provider, talk to the AI, get voice responses back.

---

## All Packages

| Package | Purpose | NuGet |
|---------|---------|-------|
| `Ai.Tlbx.VoiceAssistant` | Core orchestrator | [![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant/) |
| `...Provider.OpenAi` | OpenAI Realtime API | [![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Provider.OpenAi.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Provider.OpenAi/) |
| `...Provider.Google` | Google Gemini Live API | [![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Provider.Google.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Provider.Google/) |
| `...Provider.XAi` | xAI Grok Voice Agent API | [![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Provider.XAi.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Provider.XAi/) |
| `...Hardware.Web` | Browser audio (Blazor) | [![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Hardware.Web.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Hardware.Web/) |
| `...Hardware.Windows` | Native Windows audio | [![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Hardware.Windows.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Hardware.Windows/) |
| `...Hardware.Linux` | Native Linux audio (ALSA) | [![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Hardware.Linux.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Hardware.Linux/) |
| `...WebUi` | Pre-built Blazor components | [![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.WebUi.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.WebUi/) |

---

## Switch Providers in One Line

```csharp
// OpenAI
var provider = factory.CreateOpenAi(apiKey);
var settings = new OpenAiVoiceSettings { Voice = AssistantVoice.Alloy };

// Google Gemini
var provider = factory.CreateGoogle(apiKey);
var settings = new GoogleVoiceSettings { Voice = GeminiVoice.Puck };

// xAI Grok
var provider = factory.CreateXai(apiKey);
var settings = new XaiVoiceSettings { Voice = XaiVoice.Sage };
```

Same `VoiceAssistant` API, same tool definitions — just swap the provider.

---

## Tools: Just Write C#

Define tools with plain C# records. Schema is **auto-inferred** — no JSON, no manual mapping:

```csharp
[Description("Get weather for a location")]
public class WeatherTool : VoiceToolBase<WeatherTool.Args>
{
    public record Args(
        [property: Description("City name")] string Location,
        [property: Description("Temperature unit")] TemperatureUnit Unit = TemperatureUnit.Celsius
    );

    public override string Name => "get_weather";

    public override Task<string> ExecuteAsync(Args args)
    {
        return Task.FromResult(CreateSuccessResult(new { temp = 22, location = args.Location }));
    }
}

public enum TemperatureUnit { Celsius, Fahrenheit }
```

**Universal translation:** The same tool works on OpenAI, Google, and xAI. Required/optional parameters, enums, nested objects — all inferred from C# types.

Register in DI:
```csharp
builder.Services.AddTransient<IVoiceTool, WeatherTool>();
```

---

## Key Features

### Noise-Cancelling WebAudio
The `Hardware.Web` package includes an AudioWorklet-based noise gate that filters background noise before sending to the AI — cleaner input without external dependencies.

### Provider-Agnostic Architecture
Write once, run on any provider. The orchestrator handles:
- Audio format conversion (PCM 16-bit @ 24kHz)
- Tool schema translation per provider
- Streaming audio playback with interruption support
- Chat history management

### Built-in Tools
- `TimeTool` — Current time in any timezone
- `WeatherTool` — Mock weather (demo)
- `CalculatorTool` — Basic math operations

---

## Native Apps (Windows/Linux)

> **Note:** Native desktop support works but is less polished than the web implementation. Good for experiments and prototypes.

```csharp
// Windows (requires Windows 10+)
dotnet add package Ai.Tlbx.VoiceAssistant.Hardware.Windows

// Linux (requires libasound2-dev)
dotnet add package Ai.Tlbx.VoiceAssistant.Hardware.Linux
```

```csharp
var provider = new OpenAiVoiceProvider(apiKey, logger);
var hardware = new WindowsAudioAccess(logger); // or LinuxAudioAccess

var assistant = new VoiceAssistant(provider, hardware);
await assistant.StartAsync(settings);

Console.ReadKey(); // Talk now
await assistant.StopAsync();
```

---

## Requirements

- **.NET 9.0 or 10.0**
- **API Key:** [OpenAI](https://platform.openai.com/api-keys), [Google AI Studio](https://aistudio.google.com/apikey), or [xAI](https://console.x.ai/)
- **Web:** Modern browser with microphone permission (HTTPS or localhost)
- **Windows:** Windows 10+
- **Linux:** `sudo apt-get install libasound2-dev`

---

## License

MIT — do whatever you want.

---

<p align="center">
  <a href="https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant/">NuGet</a> •
  <a href="https://github.com/AiTlbx/Ai.Tlbx.VoiceAssistant/issues">Issues</a> •
  <a href="https://github.com/AiTlbx/Ai.Tlbx.VoiceAssistant">GitHub</a>
</p>
