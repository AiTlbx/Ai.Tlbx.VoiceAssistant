using System.Threading.Tasks;
using Ai.Tlbx.VoiceAssistant.Interfaces;
using Ai.Tlbx.VoiceAssistant.Models;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    /// <summary>
    /// Base class for voice tools that support parameter schemas.
    /// </summary>
    public abstract class VoiceToolBase : IVoiceToolWithSchema
    {
        /// <summary>
        /// Gets the name of the tool.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the description of what this tool does.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Gets the parameter schema for this tool.
        /// </summary>
        /// <returns>The parameter schema defining the expected input structure.</returns>
        public abstract ToolParameterSchema GetParameterSchema();

        /// <summary>
        /// Executes the tool with the provided JSON arguments.
        /// </summary>
        /// <param name="argumentsJson">The arguments in JSON format.</param>
        /// <returns>The result as a JSON string.</returns>
        public virtual Task<string> ExecuteAsync(string argumentsJson)
        {
            return ExecuteInternalAsync(argumentsJson);
        }

        /// <summary>
        /// Internal execution method to be implemented by derived classes.
        /// </summary>
        /// <param name="argumentsJson">The arguments in JSON format.</param>
        /// <returns>The result as a JSON string.</returns>
        protected abstract Task<string> ExecuteInternalAsync(string argumentsJson);
    }
}