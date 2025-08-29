using System;
using System.Linq;
using Ai.Tlbx.VoiceAssistant.Configuration;
using Ai.Tlbx.VoiceAssistant.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Ai.Tlbx.VoiceAssistant.Extensions
{
    /// <summary>
    /// Extension methods for the VoiceAssistantBuilder to provide additional configuration options.
    /// </summary>
    public static class VoiceAssistantBuilderExtensions
    {
        /// <summary>
        /// Adds a voice tool with configuration options.
        /// </summary>
        /// <typeparam name="TTool">The type of voice tool implementation.</typeparam>
        /// <param name="builder">The voice assistant builder.</param>
        /// <param name="configure">Optional configuration action for the tool.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public static VoiceAssistantBuilder AddTool<TTool>(
            this VoiceAssistantBuilder builder,
            Action<TTool>? configure = null)
            where TTool : class, IVoiceTool, new()
        {
            builder.Services.AddTransient<IVoiceTool>(sp =>
            {
                var tool = new TTool();
                configure?.Invoke(tool);
                return tool;
            });
            return builder;
        }

        /// <summary>
        /// Adds multiple voice tools at once.
        /// </summary>
        /// <param name="builder">The voice assistant builder.</param>
        /// <param name="toolTypes">The types of voice tool implementations to add.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public static VoiceAssistantBuilder AddTools(
            this VoiceAssistantBuilder builder,
            params Type[] toolTypes)
        {
            foreach (var toolType in toolTypes)
            {
                if (!typeof(IVoiceTool).IsAssignableFrom(toolType))
                {
                    throw new ArgumentException($"Type {toolType.Name} must implement IVoiceTool", nameof(toolTypes));
                }

                builder.Services.AddTransient(typeof(IVoiceTool), toolType);
            }
            return builder;
        }

        /// <summary>
        /// Adds all built-in tools to the voice assistant.
        /// </summary>
        /// <param name="builder">The voice assistant builder.</param>
        /// <param name="includeAdvanced">Whether to include advanced versions of tools with schema support.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public static VoiceAssistantBuilder AddBuiltInTools(
            this VoiceAssistantBuilder builder,
            bool includeAdvanced = true)
        {
            // Always add basic tools
            builder.AddTool<BuiltInTools.TimeTool>();

            // Add advanced tools if requested
            if (includeAdvanced)
            {
                //builder.AddTool<BuiltInTools.TimeToolWithSchema>();
                builder.AddTool<BuiltInTools.WeatherTool>();
                //builder.AddTool<BuiltInTools.WeatherLookupTool>();
                builder.AddTool<BuiltInTools.CalculatorTool>();
            }

            return builder;
        }

        /// <summary>
        /// Removes all tools that match a specific type.
        /// </summary>
        /// <typeparam name="TTool">The type of tool to remove.</typeparam>
        /// <param name="builder">The voice assistant builder.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public static VoiceAssistantBuilder RemoveTool<TTool>(this VoiceAssistantBuilder builder)
            where TTool : class, IVoiceTool
        {
            var descriptorsToRemove = builder.Services
                .Where(d => d.ServiceType == typeof(IVoiceTool) && 
                           d.ImplementationType == typeof(TTool))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                builder.Services.Remove(descriptor);
            }

            return builder;
        }

        /// <summary>
        /// Clears all registered tools.
        /// </summary>
        /// <param name="builder">The voice assistant builder.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public static VoiceAssistantBuilder ClearTools(this VoiceAssistantBuilder builder)
        {
            var descriptorsToRemove = builder.Services
                .Where(d => d.ServiceType == typeof(IVoiceTool))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                builder.Services.Remove(descriptor);
            }

            return builder;
        }
    }
}