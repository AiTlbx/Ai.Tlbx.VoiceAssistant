using Ai.Tlbx.VoiceAssistant.Configuration;
using Ai.Tlbx.VoiceAssistant.Interfaces;
using Ai.Tlbx.VoiceAssistant.Managers;
using Ai.Tlbx.VoiceAssistant.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Ai.Tlbx.VoiceAssistant.Extensions
{
    /// <summary>
    /// Extension methods for configuring voice assistant services in dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds voice assistant services to the service collection with fluent configuration.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>A voice assistant builder for fluent configuration.</returns>
        public static VoiceAssistantBuilder AddVoiceAssistant(this IServiceCollection services)
        {
            // Register core services
            services.AddScoped<ChatHistoryManager>();
            services.AddScoped<VoiceAssistant>();
            
            return new VoiceAssistantBuilder(services);
        }
    }
}