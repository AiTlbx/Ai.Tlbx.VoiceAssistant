using Ai.Tlbx.VoiceAssistant;
using Microsoft.AspNetCore.Components;

namespace Ai.Tlbx.VoiceAssistant.WebUi.Services
{
    /// <summary>
    /// Interface for accessing the VoiceAssistant service in components.
    /// To use this in a component, inject the CascadingParameter:
    /// [CascadingParameter] private VoiceAssistant VoiceAssistant { get; set; }
    /// </summary>
    public static class VoiceAssistantHelper
    {
        /// <summary>
        /// Render fragment that provides the VoiceAssistant as a cascading value.
        /// Use in your layout or page like:
        /// <code>
        /// @inject VoiceAssistant voiceAssistant
        /// <CascadingValue Value="voiceAssistant">
        ///     @Body
        /// </CascadingValue>
        /// </code>
        /// </summary>
        public static RenderFragment ProvideCascadingVoiceAssistant(VoiceAssistant voiceAssistant, RenderFragment childContent) =>
            builder =>
            {
                builder.OpenComponent<CascadingValue<VoiceAssistant>>(0);
                builder.AddAttribute(1, "Value", voiceAssistant);
                builder.AddAttribute(2, "ChildContent", childContent);
                builder.CloseComponent();
            };
    }
} 