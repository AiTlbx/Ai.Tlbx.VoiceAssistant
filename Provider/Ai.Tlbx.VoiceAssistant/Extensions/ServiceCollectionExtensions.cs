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

        /// <summary>
        /// Adds voice assistant services to the service collection with fluent configuration and logging.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="logAction">Action for logging voice assistant operations.</param>
        /// <returns>A voice assistant builder for fluent configuration.</returns>
        public static VoiceAssistantBuilder AddVoiceAssistant(this IServiceCollection services, Action<LogLevel, string> logAction)
        {
            // Register logging action
            services.AddSingleton(logAction);
            
            // Register core services
            services.AddScoped<ChatHistoryManager>();
            services.AddScoped<VoiceAssistant>();
            
            return new VoiceAssistantBuilder(services);
        }
    }
}