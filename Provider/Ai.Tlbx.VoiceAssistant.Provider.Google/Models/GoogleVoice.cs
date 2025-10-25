namespace Ai.Tlbx.VoiceAssistant.Provider.Google.Models
{
    /// <summary>
    /// Available assistant voices for Google Gemini Live API.
    /// Half-cascade models support a subset of voices, while native audio supports an expanded list.
    /// </summary>
    public enum GoogleVoice
    {
        /// <summary>
        /// Puck voice (supported by both half-cascade and native audio).
        /// </summary>
        Puck,

        /// <summary>
        /// Charon voice (supported by both half-cascade and native audio).
        /// </summary>
        Charon,

        /// <summary>
        /// Kore voice (supported by both half-cascade and native audio).
        /// </summary>
        Kore,

        /// <summary>
        /// Fenrir voice (supported by both half-cascade and native audio).
        /// </summary>
        Fenrir,

        /// <summary>
        /// Aoede voice (supported by both half-cascade and native audio).
        /// </summary>
        Aoede,

        /// <summary>
        /// Leda voice (supported by both half-cascade and native audio).
        /// </summary>
        Leda,

        /// <summary>
        /// Orus voice (supported by both half-cascade and native audio).
        /// </summary>
        Orus,

        /// <summary>
        /// Zephyr voice (supported by both half-cascade and native audio).
        /// </summary>
        Zephyr
    }
}
