namespace Ai.Tlbx.VoiceAssistant.Provider.Google.Models
{
    /// <summary>
    /// Configuration for audio transcription in Google Gemini Live API.
    /// </summary>
    public class AudioTranscriptionConfig
    {
        /// <summary>
        /// Whether to enable transcription of input audio (user speech).
        /// </summary>
        public bool EnableInputTranscription { get; set; } = true;

        /// <summary>
        /// Whether to enable transcription of output audio (model speech).
        /// </summary>
        public bool EnableOutputTranscription { get; set; } = true;
    }
}
