using Ai.Tlbx.VoiceAssistant.Interfaces;
using Ai.Tlbx.VoiceAssistant.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Ai.Tlbx.VoiceAssistant.Configuration
{
    /// <summary>
    /// Builder class for fluent configuration of voice assistant services.
    /// Provides a chainable API for setting up hardware access and AI providers.
    /// </summary>
    public sealed class VoiceAssistantBuilder
    {
        private readonly IServiceCollection _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceAssistantBuilder"/> class.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        internal VoiceAssistantBuilder(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// Configures the audio hardware access implementation.
        /// </summary>
        /// <typeparam name="THardware">The type of audio hardware access implementation.</typeparam>
        /// <returns>The builder instance for method chaining.</returns>
        public VoiceAssistantBuilder WithHardware<THardware>()
            where THardware : class, IAudioHardwareAccess
        {
            _services.AddScoped<IAudioHardwareAccess, THardware>();
            return this;
        }

        /// <summary>
        /// Configures the audio hardware access implementation with a factory function.
        /// </summary>
        /// <param name="factory">Factory function to create the hardware access instance.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public VoiceAssistantBuilder WithHardware(Func<IServiceProvider, IAudioHardwareAccess> factory)
        {
            _services.AddScoped(factory);
            return this;
        }

        /// <summary>
        /// Configures the audio hardware access implementation with a specific instance.
        /// </summary>
        /// <param name="hardware">The hardware access instance to use.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public VoiceAssistantBuilder WithHardware(IAudioHardwareAccess hardware)
        {
            _services.AddSingleton(hardware);
            return this;
        }

        /// <summary>
        /// Configures the AI voice provider implementation.
        /// </summary>
        /// <typeparam name="TProvider">The type of voice provider implementation.</typeparam>
        /// <returns>The builder instance for method chaining.</returns>
        public VoiceAssistantBuilder WithProvider<TProvider>()
            where TProvider : class, IVoiceProvider
        {
            _services.AddScoped<IVoiceProvider, TProvider>();
            return this;
        }

        /// <summary>
        /// Configures the AI voice provider implementation with a factory function.
        /// </summary>
        /// <param name="factory">Factory function to create the provider instance.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public VoiceAssistantBuilder WithProvider(Func<IServiceProvider, IVoiceProvider> factory)
        {
            _services.AddScoped(factory);
            return this;
        }

        /// <summary>
        /// Configures the AI voice provider implementation with a specific instance.
        /// </summary>
        /// <param name="provider">The provider instance to use.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public VoiceAssistantBuilder WithProvider(IVoiceProvider provider)
        {
            _services.AddSingleton(provider);
            return this;
        }

        /// <summary>
        /// Adds a voice tool that can be used by AI providers.
        /// </summary>
        /// <typeparam name="TTool">The type of voice tool implementation.</typeparam>
        /// <returns>The builder instance for method chaining.</returns>
        public VoiceAssistantBuilder AddTool<TTool>()
            where TTool : class, IVoiceTool
        {
            _services.AddTransient<IVoiceTool, TTool>();
            return this;
        }

        /// <summary>
        /// Adds a voice tool with a factory function.
        /// </summary>
        /// <param name="factory">Factory function to create the tool instance.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public VoiceAssistantBuilder AddTool(Func<IServiceProvider, IVoiceTool> factory)
        {
            _services.AddTransient(factory);
            return this;
        }

        /// <summary>
        /// Adds a voice tool with a specific instance.
        /// </summary>
        /// <param name="tool">The tool instance to add.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public VoiceAssistantBuilder AddTool(IVoiceTool tool)
        {
            _services.AddSingleton(tool);
            return this;
        }

        /// <summary>
        /// Configures logging for voice assistant operations.
        /// </summary>
        /// <param name="logAction">Action for logging voice assistant operations.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public VoiceAssistantBuilder WithLogging(Action<LogLevel, string> logAction)
        {
            _services.AddSingleton(logAction);
            return this;
        }

        /// <summary>
        /// Gets the configured service collection.
        /// This is the final step in the fluent configuration chain.
        /// </summary>
        /// <returns>The configured service collection.</returns>
        public IServiceCollection Services => _services;

        /// <summary>
        /// Builds and returns the service collection.
        /// This is an alternative final step in the fluent configuration chain.
        /// </summary>
        /// <returns>The configured service collection.</returns>
        public IServiceCollection Build() => _services;
    }
}