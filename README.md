# AI Voice Assistant Toolkit

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)

[![NuGet Core](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.svg?label=Core)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant/)
[![NuGet OpenAI](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Provider.OpenAi.svg?label=OpenAI%20Provider)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Provider.OpenAi/)
[![NuGet Windows](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Hardware.Windows.svg?label=Windows%20Hardware)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Hardware.Windows/)
[![NuGet Linux](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Hardware.Linux.svg?label=Linux%20Hardware)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Hardware.Linux/)
[![NuGet Web](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.Hardware.Web.svg?label=Web%20Hardware)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.Hardware.Web/)
[![NuGet WebUI](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.WebUi.svg?label=WebUI)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.WebUi/)

A simple .NET 9 toolkit for building real-time AI voice assistants. Talk to AI, get responses back with voice. Works on Windows, Linux, and Web.

## Install

```bash
# Core package (always needed)
dotnet add package Ai.Tlbx.VoiceAssistant

# AI provider
dotnet add package Ai.Tlbx.VoiceAssistant.Provider.OpenAi

# Platform (pick one)
dotnet add package Ai.Tlbx.VoiceAssistant.Hardware.Windows  # Windows
dotnet add package Ai.Tlbx.VoiceAssistant.Hardware.Linux    # Linux  
dotnet add package Ai.Tlbx.VoiceAssistant.Hardware.Web      # Blazor

# Optional UI components for web
dotnet add package Ai.Tlbx.VoiceAssistant.WebUi
```

## Simple Examples

### Console App (Windows/Linux)

```csharp
using Ai.Tlbx.VoiceAssistant;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi.Models;
using Ai.Tlbx.VoiceAssistant.Hardware.Windows; // or .Hardware.Linux
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddVoiceAssistant()
    .WithOpenAi(apiKey: "your-openai-key")
    .WithHardware<WindowsAudioDevice>(); // or LinuxAudioDevice

var voiceAssistant = services.BuildServiceProvider()
    .GetRequiredService<VoiceAssistant>();

voiceAssistant.OnMessageAdded = (message) =>
    Console.WriteLine($"[{message.Role}]: {message.Content}");

var settings = new OpenAiVoiceSettings
{
    Voice = AssistantVoice.Alloy,
    Instructions = "You are a helpful assistant."
};

await voiceAssistant.StartAsync(settings);
Console.ReadKey(); // Talk now, press any key to stop
await voiceAssistant.StopAsync();
```

### Web App (Blazor)

**Program.cs:**
```csharp
using Ai.Tlbx.VoiceAssistant;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi;
using Ai.Tlbx.VoiceAssistant.Hardware.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddVoiceAssistant()
    .WithOpenAi(apiKey: builder.Configuration["OpenAI:ApiKey"])
    .WithHardware<WebAudioAccess>();

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
```

**Voice.razor:**
```razor
@page "/voice"
@using Ai.Tlbx.VoiceAssistant
@using Ai.Tlbx.VoiceAssistant.Provider.OpenAi.Models
@inject VoiceAssistant voiceAssistant

<button @onclick="ToggleTalk" disabled="@voiceAssistant.IsConnecting">
    @(voiceAssistant.IsRecording ? "Stop" : "Talk")
</button>

<div>
    @foreach (var msg in voiceAssistant.ChatHistory.GetMessages())
    {
        <p><strong>@msg.Role:</strong> @msg.Content</p>
    }
</div>

@code {
    private async Task ToggleTalk()
    {
        if (voiceAssistant.IsRecording)
        {
            await voiceAssistant.StopAsync();
        }
        else
        {
            var settings = new OpenAiVoiceSettings
            {
                Voice = AssistantVoice.Alloy,
                Instructions = "You are a helpful assistant."
            };
            await voiceAssistant.StartAsync(settings);
        }
    }
    
    protected override Task OnInitializedAsync()
    {
        voiceAssistant.OnMessageAdded = _ => InvokeAsync(StateHasChanged);
        return base.OnInitializedAsync();
    }
}
```

## OpenAI Settings

```csharp
var settings = new OpenAiVoiceSettings
{
    // Required
    Instructions = "You are a helpful assistant",
    
    // Voice options: Alloy, Echo, Fable, Onyx, Nova, Shimmer
    Voice = AssistantVoice.Alloy,
    
    // Speed: 0.25 to 4.0 (1.0 = normal)
    TalkingSpeed = 1.0,
    
    // Model options (latest recommended)
    Model = OpenAiRealtimeModel.Gpt4oRealtimePreview20250603,
    
    // Optional
    Temperature = 0.7,
    MaxTokens = 4000,
    
    // Add tools for extra capabilities
    Tools = { new TimeToolWithSchema() }
};
```

## WebUI Components

The WebUI package provides ready-made Blazor components:

```razor
@using Ai.Tlbx.VoiceAssistant.WebUi.Components

<!-- Talk button with loading states -->
<AiTalkControl OnStartTalking="StartSession" 
               OnStopTalking="StopSession" 
               IsTalking="@voiceAssistant.IsRecording" />

<!-- Voice selection dropdown -->
<VoiceSelect @bind-SelectedVoice="selectedVoice" />

<!-- Speed slider -->
<VoiceSpeedSlider @bind-SelectedSpeed="talkingSpeed" />

<!-- Microphone picker -->
<MicrophoneSelect @bind-SelectedMicrophoneId="micId" />

<!-- Chat history display -->
<ChatWidget />

<!-- Connection status -->
<StatusWidget ConnectionStatus="@voiceAssistant.ConnectionStatus" />
```

## Built-in Tools

### Time Tool (included)
```csharp
settings.Tools.Add(new TimeToolWithSchema());
// AI can now tell you the current time in any timezone
```

### Custom Tools

Create tools for the AI to use:

```csharp
public class WeatherTool : ValidatedVoiceToolBase<WeatherArgs>
{
    public override string Name => "get_weather";
    public override string Description => "Get current weather for a location";

    public override ToolParameterSchema GetParameterSchema()
    {
        return new ToolParameterSchema
        {
            Properties = new Dictionary<string, ParameterProperty>
            {
                ["location"] = new ParameterProperty
                {
                    Type = "string",
                    Description = "City name"
                }
            },
            Required = new List<string> { "location" }
        };
    }

    protected override async Task<string> ExecuteValidatedAsync(WeatherArgs args)
    {
        // Call weather API here
        return CreateSuccessResult($"Weather in {args.Location}: Sunny, 72°F");
    }
}

public class WeatherArgs
{
    public string Location { get; set; }
}

// Use it
settings.Tools.Add(new WeatherTool());
```

That's it! The AI can now call your tool during conversations.

## Requirements

- .NET 9.0
- OpenAI API key
- **Windows**: Windows 10+
- **Linux**: `sudo apt-get install libasound2-dev`
- **Web**: Modern browser with mic permission

## GitHub

[https://github.com/AiTlbx/Ai.Tlbx.VoiceAssistant](https://github.com/AiTlbx/Ai.Tlbx.VoiceAssistant)

## License

MIT License