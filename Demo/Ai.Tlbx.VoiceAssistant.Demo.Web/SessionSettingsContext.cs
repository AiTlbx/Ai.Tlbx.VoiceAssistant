using System.Collections.Generic;

namespace Ai.Tlbx.VoiceAssistant.Demo.Web
{
    public record SessionSettingsContext
    {
        public string SelectedVoice { get; init; } = "alloy";
        public double SelectedSpeed { get; init; } = 1.0;
        public List<string> EnabledTools { get; init; } = new();
        public string SelectedMicrophoneId { get; init; } = string.Empty;
    }
}

