using System.Text.Json.Serialization;
using Ai.Tlbx.VoiceAssistant.Models;

namespace Ai.Tlbx.VoiceAssistant.Hardware.Web
{
    [JsonSerializable(typeof(AudioDeviceInfo))]
    [JsonSerializable(typeof(List<AudioDeviceInfo>))]
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    public partial class WebAudioJsonContext : JsonSerializerContext
    {
    }
}
