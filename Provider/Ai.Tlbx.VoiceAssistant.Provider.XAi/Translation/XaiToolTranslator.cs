using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Ai.Tlbx.VoiceAssistant.Interfaces;
using Ai.Tlbx.VoiceAssistant.Models;
using Ai.Tlbx.VoiceAssistant.Provider.XAi.Protocol;
using Ai.Tlbx.VoiceAssistant.Reflection;
using Ai.Tlbx.VoiceAssistant.Translation;

namespace Ai.Tlbx.VoiceAssistant.Provider.XAi.Translation
{
    /// <summary>
    /// Translates tool schemas to xAI Voice Agent API format.
    /// Also handles xAI built-in tools (web_search, x_search, file_search).
    /// </summary>
    public class XaiToolTranslator : IToolSchemaTranslator
    {
        public object TranslateToolDefinition(IVoiceTool tool, ToolSchema schema)
        {
            return new XaiToolDefinition
            {
                Type = "function",
                Name = tool.Name,
                Description = tool.Description,
                Parameters = BuildParametersObject(schema)
            };
        }

        public object TranslateTools(IEnumerable<(IVoiceTool Tool, ToolSchema Schema)> tools)
        {
            return tools.Select(t => TranslateToolDefinition(t.Tool, t.Schema)).ToList();
        }

        public object FormatToolResponse(string result, string callId, string toolName)
        {
            return new XaiConversationItemCreateMessage
            {
                Item = new XaiConversationItem
                {
                    Type = "function_call_output",
                    CallId = callId,
                    Output = result
                }
            };
        }

        /// <summary>
        /// Creates a web_search built-in tool definition.
        /// </summary>
        public static XaiToolDefinition CreateWebSearchTool()
        {
            return new XaiToolDefinition { Type = "web_search" };
        }

        /// <summary>
        /// Creates an x_search built-in tool definition.
        /// </summary>
        /// <param name="allowedXHandles">Optional list of X handles to filter results.</param>
        public static XaiToolDefinition CreateXSearchTool(IEnumerable<string>? allowedXHandles = null)
        {
            return new XaiToolDefinition { Type = "x_search" };
        }

        /// <summary>
        /// Creates a file_search built-in tool definition.
        /// </summary>
        /// <param name="vectorStoreIds">Collection IDs to search.</param>
        /// <param name="maxNumResults">Maximum number of results to return.</param>
        public static XaiToolDefinition CreateFileSearchTool(IEnumerable<string> vectorStoreIds, int? maxNumResults = null)
        {
            return new XaiToolDefinition { Type = "file_search" };
        }

        private XaiToolParameters BuildParametersObject(ToolSchema schema)
        {
            var properties = new Dictionary<string, XaiToolProperty>();

            foreach (var (name, param) in schema.Parameters)
            {
                properties[name] = BuildPropertyObject(param);
            }

            return new XaiToolParameters
            {
                Type = "object",
                Properties = properties,
                Required = schema.Required.Count > 0 ? schema.Required.ToList() : null
            };
        }

        private XaiToolProperty BuildPropertyObject(ToolParameter param)
        {
            var prop = new XaiToolProperty();

            var typeName = TypeMapper.ToJsonSchemaType(param.Type);
            using var doc = JsonDocument.Parse($"\"{typeName}\"");
            prop.Type = doc.RootElement.Clone();

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

            if (param.Default != null)
            {
                prop.Default = SerializeDefaultValue(param.Default);
            }

            if (param.Type == ToolParameterType.Array && param.Items != null)
            {
                prop.Items = BuildPropertyObject(param.Items);
            }

            if (param.Type == ToolParameterType.Object && param.Properties != null)
            {
                var nestedProps = new Dictionary<string, XaiToolProperty>();

                foreach (var (name, nestedParam) in param.Properties)
                {
                    nestedProps[name] = BuildPropertyObject(nestedParam);
                }

                prop.Properties = nestedProps;
                prop.Required = param.RequiredProperties?.Count > 0 ? param.RequiredProperties.ToList() : null;
            }

            if (param.Minimum.HasValue)
            {
                prop.Minimum = param.Minimum.Value;
            }

            if (param.Maximum.HasValue)
            {
                prop.Maximum = param.Maximum.Value;
            }

            if (param.MinLength.HasValue)
            {
                prop.MinLength = param.MinLength.Value;
            }

            if (param.MaxLength.HasValue)
            {
                prop.MaxLength = param.MaxLength.Value;
            }

            if (!string.IsNullOrEmpty(param.Pattern))
            {
                prop.Pattern = param.Pattern;
            }

            return prop;
        }

        private static JsonElement SerializeDefaultValue(object value)
        {
            var json = value switch
            {
                string s => $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"",
                bool b => b ? "true" : "false",
                int i => i.ToString(),
                long l => l.ToString(),
                double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
                decimal m => m.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => $"\"{value}\""
            };

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
    }
}
