using System;

namespace Ai.Tlbx.VoiceAssistant.Provider.Google.Models
{
    /// <summary>
    /// Supported Google Gemini Live API models that have been verified to work with this library.
    /// </summary>
    public enum GoogleModel
    {
        /// <summary>
        /// Gemini 2.5 Flash with native audio support (recommended for quality).
        /// Features the most natural and realistic-sounding speech with multilingual support.
        /// Model: gemini-2.5-flash-native-audio-preview-09-2025
        /// </summary>
        Gemini25FlashNativeAudio,

        /// <summary>
        /// Gemini Live 2.5 Flash (recommended for production reliability).
        /// Uses half-cascade architecture for improved stability.
        /// Model: gemini-live-2.5-flash-preview
        /// </summary>
        GeminiLive25Flash,

        /// <summary>
        /// Gemini 2.0 Flash Live (stable release).
        /// Earlier version maintained for compatibility.
        /// Model: gemini-2.0-flash-live-001
        /// </summary>
        Gemini20FlashLive001
    }

    /// <summary>
    /// Extension methods for GoogleModel enum.
    /// </summary>
    public static class GoogleModelExtensions
    {
        /// <summary>
        /// Gets the API model string for the specified model.
        /// </summary>
        /// <param name="model">The model enum value.</param>
        /// <returns>The API model string to use with Google Gemini.</returns>
        public static string ToApiString(this GoogleModel model)
        {
            return model switch
            {
                GoogleModel.Gemini25FlashNativeAudio => "models/gemini-2.5-flash-native-audio-preview-09-2025",
                GoogleModel.GeminiLive25Flash => "models/gemini-live-2.5-flash-preview",
                GoogleModel.Gemini20FlashLive001 => "models/gemini-2.0-flash-live-001",
                _ => throw new ArgumentOutOfRangeException(nameof(model), model, "Unsupported model")
            };
        }
    }
}
