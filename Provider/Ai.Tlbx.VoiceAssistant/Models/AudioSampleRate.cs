namespace Ai.Tlbx.VoiceAssistant.Models
{
    /// <summary>
    /// Standard audio sample rates supported by voice providers.
    /// Hardware captures at 48kHz and downsamples to the provider's required rate.
    /// </summary>
    public enum AudioSampleRate
    {
        /// <summary>16 kHz - Used by Google Gemini Live API</summary>
        Rate16000 = 16000,

        /// <summary>24 kHz - Used by OpenAI Realtime API</summary>
        Rate24000 = 24000,

        /// <summary>44.1 kHz - CD quality, some providers may use this</summary>
        Rate44100 = 44100,

        /// <summary>48 kHz - Professional audio, native capture rate</summary>
        Rate48000 = 48000
    }
}
