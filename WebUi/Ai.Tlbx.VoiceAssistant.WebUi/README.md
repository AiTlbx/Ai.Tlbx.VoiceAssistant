# Ai.Tlbx.VoiceAssistant.WebUi

Pre-built Blazor UI components for the AI Voice Assistant Toolkit.

[![NuGet](https://img.shields.io/nuget/v/Ai.Tlbx.VoiceAssistant.WebUi.svg)](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant.WebUi/)

## Installation

```bash
dotnet add package Ai.Tlbx.VoiceAssistant.WebUi
```

## Components

- `AiTalkControl` — Talk/Stop button with visual feedback
- `VoiceSelect` — Voice selection dropdown
- `VoiceSpeedSlider` — Talking speed control
- `MicrophoneSelect` — Microphone device picker
- `StatusWidget` — Connection status display
- `ChatWidget` — Conversation history view

## Usage

```razor
@using Ai.Tlbx.VoiceAssistant.WebUi.Components

<AiTalkControl OnStartTalking="StartSession"
               OnStopTalking="StopSession"
               IsTalking="@assistant.IsRecording" />

<VoiceSelect SelectedVoice="@selectedVoice"
             SelectedVoiceChanged="OnVoiceChanged" />

<ChatWidget />
```

## Full Documentation

See the main package for complete documentation:

**[Ai.Tlbx.VoiceAssistant on NuGet](https://www.nuget.org/packages/Ai.Tlbx.VoiceAssistant#readme-body-tab)**
