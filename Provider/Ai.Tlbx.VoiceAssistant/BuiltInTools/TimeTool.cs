using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    [Description("Gets the current exact date and time. Just tell the user the time with seconds. If he asks also tell the date or convert the time to a given time zone, this server stands in berlin time)")]
    public class TimeTool : VoiceToolBase<TimeTool.Args>
    {
        public record Args();

        public override string Name => "get_current_time";

        public override Task<string> ExecuteAsync(Args args)
        {
            var currentTime = DateTime.Now.ToString("yyyy.MM.dd - HH:mm:ss");
            return Task.FromResult("The current time is: " + currentTime);
        }
    }
}
