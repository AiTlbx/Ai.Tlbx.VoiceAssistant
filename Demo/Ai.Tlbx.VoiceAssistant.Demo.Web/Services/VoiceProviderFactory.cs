using System;
using System.Linq;
using Ai.Tlbx.VoiceAssistant.Interfaces;
using Ai.Tlbx.VoiceAssistant.Models;
using Ai.Tlbx.VoiceAssistant.Provider.OpenAi;
using Ai.Tlbx.VoiceAssistant.Provider.Google;
using Ai.Tlbx.VoiceAssistant.Provider.XAi;
using Microsoft.Extensions.DependencyInjection;

namespace Ai.Tlbx.VoiceAssistant.Demo.Web.Services
{
    public enum VoiceProviderType
    {
        OpenAI,
        Google,
        XAi
    }

    public interface IVoiceProviderFactory
    {
        IVoiceProvider CreateProvider(VoiceProviderType providerType);
    }

    public class VoiceProviderFactory : IVoiceProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public VoiceProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IVoiceProvider CreateProvider(VoiceProviderType providerType)
        {
            var logAction = _serviceProvider.GetService<Action<LogLevel, string>>();
            var tools = _serviceProvider.GetServices<IVoiceTool>().ToList();

            IVoiceProvider provider = providerType switch
            {
                VoiceProviderType.OpenAI => CreateOpenAIProvider(logAction, tools),
                VoiceProviderType.Google => CreateGoogleProvider(logAction, tools),
                VoiceProviderType.XAi => CreateXAiProvider(logAction, tools),
                _ => throw new ArgumentException($"Unknown provider type: {providerType}", nameof(providerType))
            };

            return provider;
        }

        private IVoiceProvider CreateOpenAIProvider(Action<LogLevel, string>? logAction, System.Collections.Generic.List<IVoiceTool> tools)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            return new OpenAiVoiceProvider(apiKey, logAction);
        }

        private IVoiceProvider CreateGoogleProvider(Action<LogLevel, string>? logAction, System.Collections.Generic.List<IVoiceTool> tools)
        {
            var apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
            return new GoogleVoiceProvider(apiKey, logAction);
        }

        private IVoiceProvider CreateXAiProvider(Action<LogLevel, string>? logAction, System.Collections.Generic.List<IVoiceTool> tools)
        {
            var apiKey = Environment.GetEnvironmentVariable("XAI_API_KEY");
            return new XaiVoiceProvider(apiKey, logAction);
        }
    }
}
