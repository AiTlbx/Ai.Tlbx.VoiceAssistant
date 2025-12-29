using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Ai.Tlbx.VoiceAssistant.BuiltInTools;
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
        /// For AOT scenarios, use the overload with TArgs type parameter and call WithJsonTypeInfo.
        /// </summary>
        /// <typeparam name="TTool">The type of voice tool implementation.</typeparam>
        /// <param name="builder">The voice assistant builder.</param>
        /// <param name="configure">Optional configuration action for the tool.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public static VoiceAssistantBuilder AddTool<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTool>(
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
        /// Adds a voice tool with AOT-compatible configuration support.
        /// Returns a ToolRegistration that allows setting JsonTypeInfo for AOT scenarios.
        /// </summary>
        /// <typeparam name="TTool">The type of voice tool implementation.</typeparam>
        /// <typeparam name="TArgs">The tool's argument type.</typeparam>
        /// <param name="builder">The voice assistant builder.</param>
        /// <returns>A ToolRegistration for fluent configuration.</returns>
        /// <example>
        /// <code>
        /// // AOT usage:
        /// builder.AddTool&lt;MyTool, MyTool.Args&gt;()
        ///        .WithJsonTypeInfo(MyToolJsonContext.Default.Args);
        ///
        /// // Non-AOT usage (reflection fallback):
        /// builder.AddTool&lt;MyTool, MyTool.Args&gt;();
        /// </code>
        /// </example>
        public static ToolRegistration<TTool, TArgs> AddTool<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TTool,
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicConstructors |
                DynamicallyAccessedMemberTypes.PublicProperties)] TArgs>(
            this VoiceAssistantBuilder builder)
            where TTool : VoiceToolBase<TArgs>, new()
            where TArgs : notnull
        {
            var tool = new TTool();
            builder.Services.AddSingleton<IVoiceTool>(tool);
            return new ToolRegistration<TTool, TArgs>(tool, builder);
        }

        /// <summary>
        /// Adds multiple voice tools at once.
        /// Note: This method is not AOT-safe. Use the generic AddTool method for AOT scenarios.
        /// </summary>
        /// <param name="builder">The voice assistant builder.</param>
        /// <param name="toolTypes">The types of voice tool implementations to add.</param>
        /// <returns>The builder instance for method chaining.</returns>
        [RequiresUnreferencedCode("This method uses reflection to register tool types. For AOT scenarios, use the generic AddTool<T> method instead.")]
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
                builder.AddTool<BuiltInTools.WeatherTool>();
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