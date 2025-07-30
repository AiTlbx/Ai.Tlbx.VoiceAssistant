using Ai.Tlbx.VoiceAssistant.Models;

namespace Ai.Tlbx.VoiceAssistant.Interfaces
{
    /// <summary>
    /// Extended interface for voice tools that support parameter schema definitions.
    /// This interface extends IVoiceTool to provide OpenAI-compatible parameter schemas.
    /// </summary>
    public interface IVoiceToolWithSchema : IVoiceTool
    {
        /// <summary>
        /// Gets the parameter schema for this tool.
        /// </summary>
        /// <returns>The parameter schema defining the expected input structure.</returns>
        ToolParameterSchema GetParameterSchema();
    }
}