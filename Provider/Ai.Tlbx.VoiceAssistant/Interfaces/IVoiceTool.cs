namespace Ai.Tlbx.VoiceAssistant.Interfaces
{
    /// <summary>
    /// Interface for voice assistant tools that can be called by AI providers.
    /// Provides a common abstraction for tools across different providers.
    /// </summary>
    public interface IVoiceTool
    {
        /// <summary>
        /// Gets the name of the tool as it should be presented to the AI provider.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets a description of what the tool does for the AI provider.
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Executes the tool with the provided arguments.
        /// </summary>
        /// <param name="argumentsJson">JSON string containing tool arguments.</param>
        /// <returns>A task that resolves to the tool execution result as JSON.</returns>
        Task<string> ExecuteAsync(string argumentsJson);
    }
}