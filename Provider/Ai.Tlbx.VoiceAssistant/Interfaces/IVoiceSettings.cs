namespace Ai.Tlbx.VoiceAssistant.Interfaces
{
    /// <summary>
    /// Base interface for provider-specific voice assistant settings.
    /// Each provider (OpenAI, Google, xAI) implements this with their specific options.
    /// </summary>
    public interface IVoiceSettings
    {
        /// <summary>
        /// Instructions for the AI assistant's behavior and personality.
        /// </summary>
        string Instructions { get; set; }
        
        /// <summary>
        /// List of tools available to the AI assistant.
        /// </summary>
        List<IVoiceTool> Tools { get; set; }
    }
}