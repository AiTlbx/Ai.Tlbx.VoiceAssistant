using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Ai.Tlbx.VoiceAssistant.BuiltInTools;

namespace Ai.Tlbx.VoiceAssistant.Configuration
{
    /// <summary>
    /// Fluent configuration for a registered voice tool.
    /// Allows AOT-compatible JsonTypeInfo configuration.
    /// </summary>
    /// <typeparam name="TTool">The tool type.</typeparam>
    /// <typeparam name="TArgs">The tool's argument type.</typeparam>
    public class ToolRegistration<TTool, [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.PublicProperties)] TArgs>
        where TTool : VoiceToolBase<TArgs>
        where TArgs : notnull
    {
        private readonly TTool _tool;
        private readonly VoiceAssistantBuilder _builder;

        internal ToolRegistration(TTool tool, VoiceAssistantBuilder builder)
        {
            _tool = tool;
            _builder = builder;
        }

        /// <summary>
        /// Sets the JsonTypeInfo for AOT-compatible argument deserialization.
        /// Call this with a source-generated JsonTypeInfo for AOT scenarios.
        /// </summary>
        /// <param name="typeInfo">The source-generated JsonTypeInfo for TArgs.</param>
        /// <returns>This registration for further chaining.</returns>
        public ToolRegistration<TTool, TArgs> WithJsonTypeInfo(JsonTypeInfo<TArgs> typeInfo)
        {
            _tool.SetJsonTypeInfo(typeInfo);
            return this;
        }

        /// <summary>
        /// Returns the builder to continue adding more tools.
        /// </summary>
        public VoiceAssistantBuilder And => _builder;

        /// <summary>
        /// Implicit conversion to VoiceAssistantBuilder for seamless chaining.
        /// </summary>
        public static implicit operator VoiceAssistantBuilder(ToolRegistration<TTool, TArgs> registration)
            => registration._builder;
    }
}
