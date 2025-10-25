namespace Ai.Tlbx.VoiceAssistant.Provider.Google.Models
{
    /// <summary>
    /// Voice Activity Detection configuration for turn detection and interruption handling.
    /// </summary>
    public class VoiceActivityDetection
    {
        /// <summary>
        /// Start-of-speech detection sensitivity. HIGH for reliable detection, LOW to reduce false positives.
        /// </summary>
        public SpeechSensitivity StartOfSpeechSensitivity { get; set; } = SpeechSensitivity.HIGH;

        /// <summary>
        /// End-of-speech detection sensitivity. LOW enables easier interruption, HIGH for faster turn-taking.
        /// </summary>
        public SpeechSensitivity EndOfSpeechSensitivity { get; set; } = SpeechSensitivity.LOW;

        /// <summary>
        /// Audio buffering duration before confirming speech start (milliseconds).
        /// </summary>
        public int PrefixPaddingMs { get; set; } = 100;

        /// <summary>
        /// Required silence duration before ending speech detection (milliseconds).
        /// </summary>
        public int SilenceDurationMs { get; set; } = 200;

        /// <summary>
        /// How user activity affects ongoing AI generation (interruption behavior).
        /// </summary>
        public ActivityHandling ActivityHandling { get; set; } = ActivityHandling.START_OF_ACTIVITY_INTERRUPTS;

        /// <summary>
        /// Enable automatic VAD. When false, client must manually signal activity.
        /// </summary>
        public bool AutomaticDetection { get; set; } = true;
    }

    public enum SpeechSensitivity
    {
        HIGH,
        LOW
    }

    public enum ActivityHandling
    {
        /// <summary>
        /// Enable barge-in: user can interrupt AI responses.
        /// </summary>
        START_OF_ACTIVITY_INTERRUPTS,

        /// <summary>
        /// Disable barge-in: AI completes responses without interruption.
        /// </summary>
        NO_INTERRUPTION
    }

    public static class VoiceActivityDetectionExtensions
    {
        public static string ToApiString(this SpeechSensitivity sensitivity, bool isStartOfSpeech)
        {
            return $"{(isStartOfSpeech ? "START" : "END")}_SENSITIVITY_{sensitivity}";
        }

        public static string ToApiString(this ActivityHandling handling)
        {
            return handling.ToString();
        }
    }
}
