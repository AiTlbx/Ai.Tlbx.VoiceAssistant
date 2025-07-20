using System;
using System.Threading.Tasks;
using Ai.Tlbx.VoiceAssistant.Interfaces;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    /// <summary>
    /// A built-in tool for retrieving the current date and time.
    /// </summary>
    public class TimeTool : IVoiceTool
    {
        /// <summary>
        /// Gets the name of the tool as it should be presented to the AI provider.
        /// </summary>
        public string Name => "get_current_time";

        /// <summary>
        /// Gets a description of what the tool does for the AI provider.
        /// </summary>
        public string Description => "Gets the current exact date and time. Just tell the user the time with seconds. If he asks also tell the date or convert the time to a given time zone, this server stands in berlin time)";

        /// <summary>
        /// Executes the tool to get the current time.
        /// </summary>
        /// <param name="argumentsJson">Ignored for this tool as it takes no arguments.</param>
        /// <returns>The current date and time on the server as a formatted string.</returns>
        public Task<string> ExecuteAsync(string argumentsJson)
        {
            // Arguments are ignored.            
            var currentTime = DateTime.Now.ToString("yyyy.MM.dd - HH:mm:ss"); 
            return Task.FromResult("The current time is: " + currentTime);
        }
    }
}