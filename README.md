# AI Voice Assistant Toolkit

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)

A comprehensive .NET 9 toolkit for building real-time AI voice assistants with support for multiple platforms and AI providers. This library provides a clean, modular architecture for integrating voice-based AI interactions into your applications.

## Features

- 🎙️ **Real-time voice interaction** with AI assistants
- 🌐 **Multi-platform support**: Windows, Linux, Web (Blazor)
- 🤖 **Provider-agnostic design** (currently supports OpenAI Realtime API)
- 🎵 **High-quality audio processing** with upsampling and EQ
- 💬 **Conversation history persistence** across sessions
- 🎛️ **Customizable voice settings** (speed, voice selection)
- 🛠️ **Extensible tool system** for custom AI capabilities
- 📊 **Built-in logging architecture** with user-controlled configuration
- 🎧 **Bluetooth-friendly** audio initialization

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Platform-Specific Guides](#platform-specific-guides)
  - [Windows Implementation](#windows-implementation)
  - [Linux Implementation](#linux-implementation)
  - [Web/Blazor Implementation](#web-blazor-implementation)
- [Architecture Overview](#architecture-overview)
- [API Reference](#api-reference)
- [Advanced Topics](#advanced-topics)
- [Troubleshooting](#troubleshooting)
- [Migration from v3.x](#migration-from-v3x)

## Installation

### NuGet Packages

Install the packages you need for your platform:

```bash
# Core package (required)
dotnet add package Ai.Tlbx.VoiceAssistant

# Provider packages (choose one or more)
dotnet add package Ai.Tlbx.VoiceAssistant.Provider.OpenAi

# Platform packages (choose based on your target)
dotnet add package Ai.Tlbx.VoiceAssistant.Hardware.Windows  # For Windows
dotnet add package Ai.Tlbx.VoiceAssistant.Hardware.Linux    # For Linux
dotnet add package Ai.Tlbx.VoiceAssistant.Hardware.Web      # For Blazor

# Optional UI components for Blazor
dotnet add package Ai.Tlbx.VoiceAssistant.WebUi
```

### Requirements

- .NET 9.0 or later
- OpenAI API key (for OpenAI provider)
- Platform-specific requirements:
  - **Windows**: Windows 10 or later
  - **Linux**: ALSA libraries (`sudo apt-get install libasound2-dev`)
  - **Web**: Modern browser with microphone permissions

## Quick Start

Here's a minimal example to get you started:

```csharp
using Ai.Tlbx.VoiceAssistant;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi.Models;
using Microsoft.Extensions.DependencyInjection;

// Configure services
var services = new ServiceCollection();
services.AddVoiceAssistant(builder =>
{
    builder.UseOpenAi(apiKey: "your-api-key-here")
           .UseWindowsHardware(); // or UseLinuxHardware() or UseWebHardware()
});

var serviceProvider = services.BuildServiceProvider();
var voiceAssistant = serviceProvider.GetRequiredService<VoiceAssistant>();

// Configure and start
var settings = new OpenAiVoiceSettings
{
    Voice = AssistantVoice.Alloy,
    Instructions = "You are a helpful assistant.",
    TalkingSpeed = 1.0,
    Model = OpenAiRealtimeModel.Gpt4oRealtimePreview20250603
};

await voiceAssistant.StartAsync(settings);

// The assistant is now listening...
// Stop when done
await voiceAssistant.StopAsync();
```

## Platform-Specific Guides

### Windows Implementation

#### Complete Windows Console Application

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ai.Tlbx.VoiceAssistant;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi.Models;

class Program
{
    static async Task Main(string[] args)
    {
        // Set up dependency injection
        var services = new ServiceCollection();
        
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information)
                   .AddConsole();
        });
        
        // Configure voice assistant
        services.AddVoiceAssistant(builder =>
        {
            builder.UseOpenAi(apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
                   .UseWindowsHardware();
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var voiceAssistant = serviceProvider.GetRequiredService<VoiceAssistant>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        
        // Set up event handlers
        voiceAssistant.OnMessageAdded = (message) =>
        {
            Console.WriteLine($"[{message.Role}]: {message.Content}");
        };
        
        voiceAssistant.OnConnectionStatusChanged = (status) =>
        {
            logger.LogInformation($"Status: {status}");
        };
        
        // Get available microphones
        var microphones = await voiceAssistant.GetAvailableMicrophonesAsync();
        Console.WriteLine("Available microphones:");
        for (int i = 0; i < microphones.Count; i++)
        {
            Console.WriteLine($"{i}: {microphones[i].Name} {(microphones[i].IsDefault ? "(Default)" : "")}");
        }
        
        // Configure settings
        var settings = new OpenAiVoiceSettings
        {
            Voice = AssistantVoice.Alloy,
            Instructions = "You are a helpful AI assistant. Be friendly and conversational.",
            TalkingSpeed = 1.0,
            Model = OpenAiRealtimeModel.Gpt4oRealtimePreview20250603
        };
        
        // Start the assistant
        Console.WriteLine("Starting voice assistant... Press any key to stop.");
        await voiceAssistant.StartAsync(settings);
        
        // Wait for user to stop
        Console.ReadKey();
        
        // Stop the assistant
        await voiceAssistant.StopAsync();
        Console.WriteLine("Voice assistant stopped.");
    }
}
```

#### Windows Forms Application

```csharp
using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Ai.Tlbx.VoiceAssistant;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi.Models;

public partial class MainForm : Form
{
    private readonly VoiceAssistant _voiceAssistant;
    private readonly IServiceProvider _serviceProvider;
    private Button _talkButton;
    private ListBox _chatHistory;
    private ComboBox _microphoneCombo;
    
    public MainForm()
    {
        InitializeComponent();
        
        // Set up DI
        var services = new ServiceCollection();
        services.AddVoiceAssistant(builder =>
        {
            builder.UseOpenAi(apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
                   .UseWindowsHardware();
        });
        
        _serviceProvider = services.BuildServiceProvider();
        _voiceAssistant = _serviceProvider.GetRequiredService<VoiceAssistant>();
        
        // Set up event handlers
        _voiceAssistant.OnMessageAdded = OnMessageAdded;
        _voiceAssistant.OnConnectionStatusChanged = OnStatusChanged;
        
        // Load microphones
        LoadMicrophones();
    }
    
    private void InitializeComponent()
    {
        // Set up UI controls
        _talkButton = new Button
        {
            Text = "Talk",
            Size = new Size(100, 50),
            Location = new Point(10, 10)
        };
        _talkButton.Click += TalkButton_Click;
        
        _microphoneCombo = new ComboBox
        {
            Location = new Point(120, 20),
            Size = new Size(200, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        
        _chatHistory = new ListBox
        {
            Location = new Point(10, 70),
            Size = new Size(400, 300)
        };
        
        Controls.AddRange(new Control[] { _talkButton, _microphoneCombo, _chatHistory });
        Text = "Voice Assistant";
        Size = new Size(450, 450);
    }
    
    private async void LoadMicrophones()
    {
        var mics = await _voiceAssistant.GetAvailableMicrophonesAsync();
        _microphoneCombo.Items.Clear();
        foreach (var mic in mics)
        {
            _microphoneCombo.Items.Add(new MicrophoneItem 
            { 
                Info = mic, 
                Display = $"{mic.Name} {(mic.IsDefault ? "(Default)" : "")}" 
            });
        }
        
        // Select default
        for (int i = 0; i < _microphoneCombo.Items.Count; i++)
        {
            if (((MicrophoneItem)_microphoneCombo.Items[i]).Info.IsDefault)
            {
                _microphoneCombo.SelectedIndex = i;
                break;
            }
        }
    }
    
    private async void TalkButton_Click(object sender, EventArgs e)
    {
        if (_voiceAssistant.IsRecording)
        {
            await _voiceAssistant.StopAsync();
            _talkButton.Text = "Talk";
        }
        else
        {
            var settings = new OpenAiVoiceSettings
            {
                Voice = AssistantVoice.Alloy,
                Instructions = "You are a helpful assistant.",
                TalkingSpeed = 1.0
            };
            
            await _voiceAssistant.StartAsync(settings);
            _talkButton.Text = "Stop";
        }
    }
    
    private void OnMessageAdded(ChatMessage message)
    {
        Invoke(new Action(() =>
        {
            _chatHistory.Items.Add($"[{message.Role}]: {message.Content}");
            _chatHistory.SelectedIndex = _chatHistory.Items.Count - 1;
        }));
    }
    
    private void OnStatusChanged(string status)
    {
        Invoke(new Action(() =>
        {
            Text = $"Voice Assistant - {status}";
        }));
    }
    
    private class MicrophoneItem
    {
        public AudioDeviceInfo Info { get; set; }
        public string Display { get; set; }
        public override string ToString() => Display;
    }
}
```

### Linux Implementation

#### Complete Linux Console Application

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ai.Tlbx.VoiceAssistant;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi.Models;

class Program
{
    static async Task Main(string[] args)
    {
        // Ensure ALSA is available
        if (!CheckAlsaAvailable())
        {
            Console.WriteLine("ALSA is not available. Install with: sudo apt-get install libasound2-dev");
            return;
        }
        
        // Set up dependency injection
        var services = new ServiceCollection();
        
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information)
                   .AddConsole();
        });
        
        // Configure voice assistant
        services.AddVoiceAssistant(builder =>
        {
            builder.UseOpenAi(apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
                   .UseLinuxHardware();
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var voiceAssistant = serviceProvider.GetRequiredService<VoiceAssistant>();
        
        // Set up event handlers
        voiceAssistant.OnMessageAdded = (message) =>
        {
            Console.WriteLine($"\033[1m[{message.Role}]\033[0m: {message.Content}");
        };
        
        voiceAssistant.OnConnectionStatusChanged = (status) =>
        {
            Console.WriteLine($"\033[33mStatus: {status}\033[0m");
        };
        
        // List available devices
        var microphones = await voiceAssistant.GetAvailableMicrophonesAsync();
        Console.WriteLine("\033[36mAvailable microphones:\033[0m");
        foreach (var mic in microphones)
        {
            Console.WriteLine($"  - {mic.Name} {(mic.IsDefault ? "\033[32m(Default)\033[0m" : "")}");
        }
        
        // Configure settings
        var settings = new OpenAiVoiceSettings
        {
            Voice = AssistantVoice.Alloy,
            Instructions = "You are a helpful Linux terminal assistant.",
            TalkingSpeed = 1.0
        };
        
        // Start the assistant
        Console.WriteLine("\n\033[32mStarting voice assistant... Press 'q' to quit.\033[0m");
        await voiceAssistant.StartAsync(settings);
        
        // Wait for quit command
        while (Console.ReadKey(true).KeyChar != 'q')
        {
            // Keep running
        }
        
        // Stop the assistant
        await voiceAssistant.StopAsync();
        Console.WriteLine("\n\033[31mVoice assistant stopped.\033[0m");
    }
    
    static bool CheckAlsaAvailable()
    {
        try
        {
            // Try to load ALSA library
            return System.IO.File.Exists("/usr/lib/x86_64-linux-gnu/libasound.so.2") ||
                   System.IO.File.Exists("/usr/lib/libasound.so.2");
        }
        catch
        {
            return false;
        }
    }
}
```

#### Linux Avalonia UI Application

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Ai.Tlbx.VoiceAssistant;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi.Models;

public class MainWindow : Window
{
    private readonly VoiceAssistant _voiceAssistant;
    private Button _talkButton;
    private ListBox _chatHistory;
    private ComboBox _microphoneCombo;
    private TextBlock _statusText;
    
    public MainWindow()
    {
        // Set up DI
        var services = new ServiceCollection();
        services.AddVoiceAssistant(builder =>
        {
            builder.UseOpenAi(apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
                   .UseLinuxHardware();
        });
        
        var serviceProvider = services.BuildServiceProvider();
        _voiceAssistant = serviceProvider.GetRequiredService<VoiceAssistant>();
        
        // Set up event handlers
        _voiceAssistant.OnMessageAdded = OnMessageAdded;
        _voiceAssistant.OnConnectionStatusChanged = OnStatusChanged;
        
        InitializeComponent();
        LoadMicrophones();
    }
    
    private void InitializeComponent()
    {
        Title = "Voice Assistant - Linux";
        Width = 600;
        Height = 500;
        
        var panel = new StackPanel { Margin = new Thickness(10) };
        
        // Controls row
        var controlsPanel = new StackPanel 
        { 
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(0, 0, 0, 10)
        };
        
        _talkButton = new Button 
        { 
            Content = "Talk",
            Width = 100,
            Height = 40
        };
        _talkButton.Click += TalkButton_Click;
        
        _microphoneCombo = new ComboBox 
        { 
            Width = 300,
            Height = 40
        };
        
        controlsPanel.Children.Add(_talkButton);
        controlsPanel.Children.Add(_microphoneCombo);
        
        // Status
        _statusText = new TextBlock 
        { 
            Text = "Ready",
            Margin = new Thickness(0, 0, 0, 10)
        };
        
        // Chat history
        _chatHistory = new ListBox 
        { 
            Height = 350
        };
        
        panel.Children.Add(controlsPanel);
        panel.Children.Add(_statusText);
        panel.Children.Add(_chatHistory);
        
        Content = panel;
    }
    
    private async void LoadMicrophones()
    {
        var mics = await _voiceAssistant.GetAvailableMicrophonesAsync();
        var items = new List<MicrophoneItem>();
        
        foreach (var mic in mics)
        {
            items.Add(new MicrophoneItem 
            { 
                Info = mic, 
                Display = $"{mic.Name} {(mic.IsDefault ? "(Default)" : "")}" 
            });
        }
        
        _microphoneCombo.Items = items;
        
        // Select default
        var defaultMic = items.FirstOrDefault(m => m.Info.IsDefault);
        if (defaultMic != null)
        {
            _microphoneCombo.SelectedItem = defaultMic;
        }
    }
    
    private async void TalkButton_Click(object sender, RoutedEventArgs e)
    {
        if (_voiceAssistant.IsRecording)
        {
            await _voiceAssistant.StopAsync();
            _talkButton.Content = "Talk";
        }
        else
        {
            var settings = new OpenAiVoiceSettings
            {
                Voice = AssistantVoice.Alloy,
                Instructions = "You are a helpful Linux assistant.",
                TalkingSpeed = 1.0
            };
            
            await _voiceAssistant.StartAsync(settings);
            _talkButton.Content = "Stop";
        }
    }
    
    private void OnMessageAdded(ChatMessage message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _chatHistory.Items.Add($"[{message.Role}]: {message.Content}");
            _chatHistory.ScrollIntoView(_chatHistory.Items[_chatHistory.Items.Count - 1]);
        });
    }
    
    private void OnStatusChanged(string status)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _statusText.Text = $"Status: {status}";
        });
    }
    
    private class MicrophoneItem
    {
        public AudioDeviceInfo Info { get; set; }
        public string Display { get; set; }
        public override string ToString() => Display;
    }
}
```

### Web/Blazor Implementation

#### Blazor Server Application

**Program.cs**
```csharp
using Ai.Tlbx.VoiceAssistant;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Configure Voice Assistant
builder.Services.AddVoiceAssistant(voiceBuilder =>
{
    voiceBuilder.UseOpenAi(apiKey: builder.Configuration["OpenAI:ApiKey"])
                .UseWebHardware();
});

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

**Pages/VoiceChat.razor**
```razor
@page "/voice-chat"
@using Ai.Tlbx.VoiceAssistant
@using Ai.Tlbx.VoiceAssistant.Provider.OpenAi
@using Ai.Tlbx.VoiceAssistant.Provider.OpenAi.Models
@using Ai.Tlbx.VoiceAssistant.WebUi.Components
@using Ai.Tlbx.VoiceAssistant.Models
@inject VoiceAssistant voiceAssistant
@implements IDisposable

<PageTitle>Voice Assistant</PageTitle>

<div class="container mt-4">
    <div class="row">
        <div class="col-md-4">
            <div class="card">
                <div class="card-header">
                    <h5>Controls</h5>
                </div>
                <div class="card-body">
                    <!-- Talk Button -->
                    <div class="mb-3">
                        <AiTalkControl OnStartTalking="StartSession" 
                                      OnStopTalking="StopSession" 
                                      IsTalking="@voiceAssistant.IsRecording" 
                                      Loading="@voiceAssistant.IsConnecting" />
                    </div>
                    
                    <!-- Voice Selection -->
                    <div class="mb-3">
                        <label class="form-label">Voice</label>
                        <VoiceSelect SelectedVoice="@selectedVoice" 
                                    SelectedVoiceChanged="OnVoiceChanged" 
                                    Disabled="@(voiceAssistant.IsConnecting || voiceAssistant.IsRecording)" />
                    </div>
                    
                    <!-- Speed Control -->
                    <div class="mb-3">
                        <VoiceSpeedSlider SelectedSpeed="@selectedSpeed" 
                                         SelectedSpeedChanged="OnSpeedChanged" 
                                         Disabled="@(voiceAssistant.IsConnecting || voiceAssistant.IsRecording)" />
                    </div>
                    
                    <!-- Microphone Selection -->
                    <div class="mb-3">
                        <label class="form-label">Microphone</label>
                        <MicrophoneSelect AvailableMicrophones="@availableMicrophones" 
                                         @bind-SelectedMicrophoneId="@selectedMicrophoneId" 
                                         MicPermissionGranted="@micPermissionGranted" 
                                         OnRequestPermission="RequestMicrophonePermission" 
                                         Disabled="@(voiceAssistant.IsConnecting || voiceAssistant.IsRecording)" />
                    </div>
                    
                    <!-- Status -->
                    <div class="mb-3">
                        <StatusWidget ConnectionStatus="@voiceAssistant.ConnectionStatus" 
                                     Error="@voiceAssistant.LastErrorMessage" 
                                     IsMicrophoneTesting="@voiceAssistant.IsMicrophoneTesting" />
                    </div>
                    
                    <!-- Clear Chat -->
                    <button class="btn btn-secondary w-100" 
                            @onclick="ClearChat" 
                            disabled="@voiceAssistant.IsConnecting">
                        Clear Chat
                    </button>
                </div>
            </div>
        </div>
        
        <div class="col-md-8">
            <div class="card">
                <div class="card-header">
                    <h5>Conversation</h5>
                </div>
                <div class="card-body" style="height: 500px; overflow-y: auto;">
                    <ChatWidget />
                </div>
            </div>
        </div>
    </div>
</div>

@code {
    private string selectedVoice = "alloy";
    private double selectedSpeed = 1.0;
    private string selectedMicrophoneId = string.Empty;
    private bool micPermissionGranted = false;
    private List<MicrophoneSelect.MicrophoneInfo> availableMicrophones = new();
    
    protected override async Task OnInitializedAsync()
    {
        voiceAssistant.OnConnectionStatusChanged = OnConnectionStatusChanged;
        voiceAssistant.OnMessageAdded = OnMessageAdded;
        voiceAssistant.OnMicrophoneDevicesChanged = OnMicrophoneDevicesChanged;
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await CheckMicrophonePermission();
        }
    }
    
    private async Task CheckMicrophonePermission()
    {
        try
        {
            var mics = await voiceAssistant.GetAvailableMicrophonesAsync();
            
            micPermissionGranted = mics.Count > 0 && 
                mics.Any(m => !string.IsNullOrEmpty(m.Name) && !m.Name.StartsWith("Microphone "));
            
            if (mics.Count > 0)
            {
                availableMicrophones = mics.Select(m => new MicrophoneSelect.MicrophoneInfo
                {
                    Id = m.Id,
                    Name = m.Name,
                    IsDefault = m.IsDefault
                }).ToList();
                
                var defaultMic = availableMicrophones.FirstOrDefault(m => m.IsDefault);
                if (defaultMic != null)
                {
                    selectedMicrophoneId = defaultMic.Id;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking microphone permission: {ex.Message}");
        }
        
        await InvokeAsync(StateHasChanged);
    }
    
    private async Task RequestMicrophonePermission()
    {
        try
        {
            var devices = await voiceAssistant.GetAvailableMicrophonesAsync();
            
            availableMicrophones = devices.Select(m => new MicrophoneSelect.MicrophoneInfo
            {
                Id = m.Id,
                Name = m.Name,
                IsDefault = m.IsDefault
            }).ToList();
            
            micPermissionGranted = devices.Count > 0 && 
                devices.Any(m => !string.IsNullOrEmpty(m.Name) && !m.Name.StartsWith("Microphone "));
            
            if (micPermissionGranted && availableMicrophones.Count > 0)
            {
                var defaultMic = availableMicrophones.FirstOrDefault(m => m.IsDefault);
                selectedMicrophoneId = defaultMic?.Id ?? availableMicrophones[0].Id;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error requesting microphone permission: {ex.Message}");
        }
        
        await InvokeAsync(StateHasChanged);
    }
    
    private async Task StartSession()
    {
        try
        {
            var settings = new OpenAiVoiceSettings
            {
                Instructions = "You are a helpful AI assistant. Be friendly and conversational.",
                Voice = Enum.Parse<AssistantVoice>(selectedVoice, true),
                TalkingSpeed = selectedSpeed,
                Model = OpenAiRealtimeModel.Gpt4oRealtimePreview20250603
            };
            
            await voiceAssistant.StartAsync(settings);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting session: {ex.Message}");
        }
    }
    
    private async Task StopSession()
    {
        try
        {
            await voiceAssistant.StopAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping session: {ex.Message}");
        }
    }
    
    private void ClearChat()
    {
        voiceAssistant.ClearChatHistory();
        InvokeAsync(StateHasChanged);
    }
    
    private async Task OnVoiceChanged(string newVoice)
    {
        selectedVoice = newVoice;
        await Task.CompletedTask;
    }
    
    private async Task OnSpeedChanged(double newSpeed)
    {
        selectedSpeed = newSpeed;
        await Task.CompletedTask;
    }
    
    private void OnConnectionStatusChanged(string status)
    {
        InvokeAsync(StateHasChanged);
    }
    
    private void OnMessageAdded(ChatMessage message)
    {
        InvokeAsync(StateHasChanged);
    }
    
    private void OnMicrophoneDevicesChanged(List<AudioDeviceInfo> devices)
    {
        InvokeAsync(async () => 
        {
            availableMicrophones = devices.Select(m => new MicrophoneSelect.MicrophoneInfo
            {
                Id = m.Id,
                Name = m.Name,
                IsDefault = m.IsDefault
            }).ToList();
            StateHasChanged();
        });
    }
    
    public void Dispose()
    {
        voiceAssistant.OnConnectionStatusChanged = null;
        voiceAssistant.OnMessageAdded = null;
        voiceAssistant.OnMicrophoneDevicesChanged = null;
    }
}
```

**Important Web-Specific Files**

The Web implementation requires these JavaScript files to be placed in `wwwroot/js/`:

1. **webAudioAccess.js** - Main audio handling module
2. **audio-processor.js** - Audio worklet processor for real-time audio

These are included in the `Ai.Tlbx.VoiceAssistant.Hardware.Web` package and will be automatically copied to your project.

## Architecture Overview

### Component Hierarchy

```
VoiceAssistant (Orchestrator)
├── IVoiceProvider (AI Provider Interface)
│   └── OpenAiVoiceProvider
├── IAudioHardwareAccess (Platform Interface)
│   ├── WindowsAudioAccess
│   ├── LinuxAudioAccess
│   └── WebAudioAccess
└── ChatHistoryManager (Conversation State)
```

### Key Interfaces

#### IVoiceProvider
```csharp
public interface IVoiceProvider : IAsyncDisposable
{
    bool IsConnected { get; }
    Task ConnectAsync(IVoiceSettings settings);
    Task DisconnectAsync();
    Task ProcessAudioAsync(string base64Audio);
    Task SendInterruptAsync();
    Task InjectConversationHistoryAsync(IEnumerable<ChatMessage> messages);
    
    // Callbacks
    Action<ChatMessage>? OnMessageReceived { get; set; }
    Action<string>? OnAudioReceived { get; set; }
    Action<string>? OnStatusChanged { get; set; }
    Action<string>? OnError { get; set; }
    Action? OnInterruptDetected { get; set; }
}
```

#### IAudioHardwareAccess
```csharp
public interface IAudioHardwareAccess : IAsyncDisposable
{
    Task InitAudio();
    Task<bool> StartRecordingAudio(MicrophoneAudioReceivedEventHandler audioDataReceivedHandler);
    Task<bool> StopRecordingAudio();
    bool PlayAudio(string base64EncodedPcm16Audio, int sampleRate = 24000);
    Task ClearAudioQueue();
    Task<List<AudioDeviceInfo>> GetAvailableMicrophones();
    Task<bool> SetMicrophoneDevice(string deviceId);
    void SetLogAction(Action<LogLevel, string> logAction);
}
```

## API Reference

### VoiceAssistant Class

Main orchestrator for voice interactions.

#### Properties

- `bool IsRecording` - Indicates if currently recording audio
- `bool IsConnecting` - Indicates if connecting to AI provider
- `bool IsMicrophoneTesting` - Indicates if microphone test is running
- `string ConnectionStatus` - Current connection status message
- `string LastErrorMessage` - Last error message if any

#### Methods

- `Task StartAsync(IVoiceSettings settings)` - Start voice assistant session
- `Task StopAsync()` - Stop current session
- `Task InterruptAsync()` - Interrupt current AI response
- `Task<List<AudioDeviceInfo>> GetAvailableMicrophonesAsync()` - Get available microphones
- `Task TestMicrophoneAsync()` - Test microphone with beep playback
- `void ClearChatHistory()` - Clear conversation history

#### Events

- `Action<ChatMessage> OnMessageAdded` - Fired when message is added to chat
- `Action<string> OnConnectionStatusChanged` - Fired when connection status changes
- `Action<List<AudioDeviceInfo>> OnMicrophoneDevicesChanged` - Fired when mic list changes

### OpenAiVoiceSettings

Configuration for OpenAI provider.

```csharp
public class OpenAiVoiceSettings : IVoiceSettings
{
    public string Instructions { get; set; }
    public AssistantVoice Voice { get; set; } = AssistantVoice.Alloy;
    public double TalkingSpeed { get; set; } = 1.0;
    public List<IVoiceTool> Tools { get; set; } = new();
    public OpenAiRealtimeModel Model { get; set; } = OpenAiRealtimeModel.Gpt4oRealtimePreview20250603;
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
}
```

### Voice Options

```csharp
public enum AssistantVoice
{
    Alloy,
    Echo,
    Fable,
    Onyx,
    Nova,
    Shimmer
}
```

### Model Options

```csharp
public enum OpenAiRealtimeModel
{
    Gpt4oRealtimePreview20250603 = 0,         // Latest (June 2025) - Recommended
    Gpt4oRealtimePreview20241217 = 1,         // December 2024 - Stable
    Gpt4oRealtimePreview20241001 = 2,         // October 2024 - Legacy
    Gpt4oMiniRealtimePreview20241217 = 3,     // Mini model - Lower latency
}
```

## Advanced Topics

### Custom Tools

Implement custom tools for AI capabilities:

```csharp
public class WeatherTool : IVoiceTool
{
    public string Name => "get_weather";
    public string Description => "Get current weather for a location";
    
    public ToolParameterSchema GetParameterSchema()
    {
        return new ToolParameterSchema
        {
            Type = "object",
            Properties = new Dictionary<string, ToolProperty>
            {
                ["location"] = new ToolProperty
                {
                    Type = "string",
                    Description = "City name"
                }
            },
            Required = new[] { "location" }
        };
    }
    
    public async Task<string> ExecuteAsync(string arguments)
    {
        var args = JsonSerializer.Deserialize<Dictionary<string, string>>(arguments);
        var location = args["location"];
        
        // Implement weather API call
        return $"The weather in {location} is sunny and 72°F";
    }
}

// Use in settings
settings.Tools.Add(new WeatherTool());
```

### Logging Configuration

The toolkit uses a centralized logging architecture. Configure logging at the orchestrator level:

```csharp
services.AddVoiceAssistant(builder =>
{
    builder.UseOpenAi(apiKey: "...")
           .UseWindowsHardware()
           .ConfigureLogging((provider, log) =>
           {
               // Custom logging logic
               var logger = provider.GetService<ILogger<VoiceAssistant>>();
               logger?.Log(log.Level.ToMicrosoftLogLevel(), log.Message);
           });
});
```

### Conversation History

The assistant maintains conversation history across sessions:

```csharp
// History is automatically injected when starting new sessions
// To manually manage history:
var messages = voiceAssistant.ChatHistory.GetMessages();

// Clear history
voiceAssistant.ClearChatHistory();
```

### Error Handling

```csharp
voiceAssistant.OnConnectionStatusChanged = (status) =>
{
    if (status.Contains("error", StringComparison.OrdinalIgnoreCase))
    {
        // Handle error
        var error = voiceAssistant.LastErrorMessage;
        Console.WriteLine($"Error occurred: {error}");
    }
};

// Also handle provider errors
try
{
    await voiceAssistant.StartAsync(settings);
}
catch (InvalidOperationException ex)
{
    // Handle initialization errors
    Console.WriteLine($"Failed to start: {ex.Message}");
}
```

## Troubleshooting

### Common Issues

#### Windows

**Issue**: "No microphones found"
- **Solution**: Check Windows privacy settings for microphone access
- Run as Administrator if needed
- Ensure audio drivers are installed

**Issue**: "NAudio initialization failed"
- **Solution**: Install Windows audio drivers
- Check Windows Audio service is running

#### Linux

**Issue**: "ALSA lib not found"
- **Solution**: Install ALSA libraries
  ```bash
  sudo apt-get update
  sudo apt-get install libasound2-dev
  ```

**Issue**: "Permission denied accessing audio device"
- **Solution**: Add user to audio group
  ```bash
  sudo usermod -a -G audio $USER
  # Log out and back in
  ```

#### Web/Blazor

**Issue**: "Microphone permission denied"
- **Solution**: 
  - Ensure HTTPS or localhost
  - Browser must support getUserMedia API
  - User must grant permission when prompted

**Issue**: "Audio worklet failed to load"
- **Solution**: 
  - Ensure JavaScript files are in wwwroot/js/
  - Check browser console for errors
  - Verify HTTPS is enabled

**Issue**: "Bluetooth headset switches to hands-free mode"
- **Solution**: The toolkit now prevents this by:
  - Using higher sample rates (48kHz)
  - Deferring AudioContext creation
  - Proper constraint configuration

### Debug Logging

Enable detailed logging for troubleshooting:

```csharp
services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Debug)
           .AddConsole()
           .AddDebug();
});

// For web hardware, enable JavaScript diagnostics
var hardware = serviceProvider.GetRequiredService<IAudioHardwareAccess>();
if (hardware is WebAudioAccess webAccess)
{
    await webAccess.SetDiagnosticLevel(DiagnosticLevel.Verbose);
}
```

## Migration from v3.x

Version 4.0 introduces breaking changes from v3.x:

### 1. Package Name Changes
- Old: `Ai.Tlbx.RealTimeAudio.*`
- New: `Ai.Tlbx.VoiceAssistant.*`

### 2. Architecture Changes
- `OpenAiRealTimeApiAccess` replaced by `VoiceAssistant` + `OpenAiVoiceProvider`
- Event-based callbacks replaced with `Action` properties
- New dependency injection pattern

### 3. Code Migration Example

**v3.x Code:**
```csharp
var openAiAccess = new OpenAiRealTimeApiAccess(apiKey);
openAiAccess.MessageReceived += OnMessageReceived;
await openAiAccess.ConnectAsync();
```

**v4.0 Code:**
```csharp
services.AddVoiceAssistant(builder =>
{
    builder.UseOpenAi(apiKey)
           .UseWindowsHardware();
});

var voiceAssistant = serviceProvider.GetRequiredService<VoiceAssistant>();
voiceAssistant.OnMessageAdded = OnMessageAdded;
await voiceAssistant.StartAsync(settings);
```

## GitHub Repository

[https://github.com/AiTlbx/Ai.Tlbx.VoiceAssistant](https://github.com/AiTlbx/Ai.Tlbx.VoiceAssistant)

## Contributing

Contributions are welcome! Please read our contributing guidelines and submit pull requests to our repository.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For issues, questions, or contributions, please visit our [GitHub repository](https://github.com/AiTlbx/Ai.Tlbx.VoiceAssistant).

## Acknowledgments

- Built on top of NAudio for Windows audio
- Uses ALSA for Linux audio support
- Leverages Web Audio API for browser-based audio