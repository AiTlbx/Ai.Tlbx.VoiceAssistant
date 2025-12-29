using System.Collections.Generic;
using System.Linq;
using Ai.Tlbx.VoiceAssistant.Interfaces;
using Ai.Tlbx.VoiceAssistant.Models;
using Ai.Tlbx.VoiceAssistant.Provider.Google.Protocol;
using Ai.Tlbx.VoiceAssistant.Reflection;
using Ai.Tlbx.VoiceAssistant.Translation;

namespace Ai.Tlbx.VoiceAssistant.Provider.Google.Translation
{
    /// <summary>
    /// Translates tool schemas to Google Gemini Live API format.
    /// Uses functionDeclarations wrapper structure.
    /// </summary>
    public class GoogleToolTranslator : IToolSchemaTranslator
    {
        public object TranslateToolDefinition(IVoiceTool tool, ToolSchema schema)
        {
            return new FunctionDeclaration
            {
                Name = tool.Name,
                Description = tool.Description,
                Parameters = BuildParametersObject(schema)
            };
        }

        public object TranslateTools(IEnumerable<(IVoiceTool Tool, ToolSchema Schema)> tools)
        {
            var functionDeclarations = tools
                .Select(t => (FunctionDeclaration)TranslateToolDefinition(t.Tool, t.Schema))
                .ToList();

            return new List<Tool>
            {
                new Tool { FunctionDeclarations = functionDeclarations }
            };
        }

        public object FormatToolResponse(string result, string callId, string toolName)
        {
            return new ToolResponseMessage
            {
                ToolResponse = new ToolResponse
                {
                    FunctionResponses = new List<FunctionResponse>
                    {
                        new FunctionResponse
                        {
                            Id = callId,
                            Name = toolName,
                            Response = new FunctionResponseData
                            {
                                Result = result
                            }
                        }
                    }
                }
            };
        }

        private GoogleToolParameters BuildParametersObject(ToolSchema schema)
        {
            var properties = new Dictionary<string, GoogleToolProperty>();

            foreach (var (name, param) in schema.Parameters)
            {
                properties[name] = BuildPropertyObject(param);
            }

            return new GoogleToolParameters
            {
                Type = "object",
                Properties = properties,
                Required = schema.Required.Count > 0 ? schema.Required.ToList() : null
            };
        }

        private GoogleToolProperty BuildPropertyObject(ToolParameter param)
        {
            var prop = new GoogleToolProperty
            {
                Type = TypeMapper.ToJsonSchemaType(param.Type)
            };

            if (!string.IsNullOrEmpty(param.Description))
            {
                prop.Description = param.Description;
            }

            if (param.Enum != null && param.Enum.Count > 0)
            {
                prop.Enum = param.Enum;
            }

            if (!string.IsNullOrEmpty(param.Format))
            {
                prop.Format = param.Format;
            }

            if (param.Type == ToolParameterType.Array && param.Items != null)
            {
                prop.Items = BuildPropertyObject(param.Items);
            }

            if (param.Type == ToolParameterType.Object && param.Properties != null)
            {
                var nestedProps = new Dictionary<string, GoogleToolProperty>();

                foreach (var (name, nestedParam) in param.Properties)
                {
                    nestedProps[name] = BuildPropertyObject(nestedParam);
                }

                prop.Properties = nestedProps;

                if (param.RequiredProperties?.Count > 0)
                {
                    prop.Required = param.RequiredProperties.ToList();
                }
            }

            return prop;
        }
    }
}
