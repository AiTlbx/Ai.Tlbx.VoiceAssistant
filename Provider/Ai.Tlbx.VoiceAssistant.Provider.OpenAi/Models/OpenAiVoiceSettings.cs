using System.Collections.Generic;
using Ai.Tlbx.VoiceAssistant.Interfaces;

namespace Ai.Tlbx.VoiceAssistant.Provider.OpenAi.Models
{
    /// <summary>
    /// OpenAI-specific voice assistant settings that control model behavior and configuration.
    /// </summary>
    public class OpenAiVoiceSettings : IVoiceSettings
    {
        /// <summary>
        /// Instructions for the AI assistant's behavior and personality.
        /// </summary>
        public string Instructions { get; set; } = "You are a helpful assistant.";
        
        /// <summary>
        /// List of tools available to the AI assistant.
        /// </summary>
        public List<IVoiceTool> Tools { get; set; } = new();

        /// <summary>
        /// The OpenAI model to use for the conversation.
        /// </summary>
        public string Model { get; set; } = "gpt-4o-realtime-preview-2025-06-03";

        /// <summary>
        /// The voice to use for AI responses.
        /// </summary>
        public AssistantVoice Voice { get; set; } = AssistantVoice.Alloy;

        /// <summary>
        /// The temperature setting for response generation (0.0 to 1.0).
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Maximum number of tokens for the response.
        /// </summary>
        public int? MaxTokens { get; set; }

        /// <summary>
        /// Turn detection settings for conversation flow.
        /// </summary>
        public TurnDetection TurnDetection { get; set; } = new();

        /// <summary>
        /// Input audio transcription settings.
        /// </summary>
        public InputAudioTranscription InputAudioTranscription { get; set; } = new();

        /// <summary>
        /// Output audio format settings.
        /// </summary>
        public string OutputAudioFormat { get; set; } = "pcm16";
    }

    /// <summary>
    /// Available assistant voices for OpenAI real-time API.
    /// </summary>
    public enum AssistantVoice
    {
        Alloy,
        Echo,
        Fable,
        Onyx,
        Nova,
        Shimmer
    }

    /// <summary>
    /// Turn detection configuration for conversation management.
    /// </summary>
    public class TurnDetection
    {
        /// <summary>
        /// Type of turn detection (server_vad for server-side voice activity detection).
        /// </summary>
        public string Type { get; set; } = "server_vad";

        /// <summary>
        /// Threshold for voice activity detection (0.0 to 1.0).
        /// </summary>
        public double Threshold { get; set; } = 0.5;

        /// <summary>
        /// Prefix padding in milliseconds.
        /// </summary>
        public int PrefixPaddingMs { get; set; } = 300;

        /// <summary>
        /// Silence duration in milliseconds before considering turn complete.
        /// </summary>
        public int SilenceDurationMs { get; set; } = 200;
    }

    /// <summary>
    /// Input audio transcription configuration.
    /// </summary>
    public class InputAudioTranscription
    {
        /// <summary>
        /// Model to use for transcription.
        /// </summary>
        public string Model { get; set; } = "whisper-1";

        /// <summary>
        /// Whether to enable transcription.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}