using System.Collections.Generic;
using System.Linq;
using Ai.Tlbx.VoiceAssistant.Interfaces;
using Ai.Tlbx.VoiceAssistant.Models;
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
            var parameters = BuildParametersObject(schema);

            return new Dictionary<string, object>
            {
                ["type"] = "function",
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["parameters"] = parameters
            };
        }

        public object TranslateTools(IEnumerable<(IVoiceTool Tool, ToolSchema Schema)> tools)
        {
            return tools.Select(t => TranslateToolDefinition(t.Tool, t.Schema)).ToList();
        }

        public object FormatToolResponse(string result, string callId, string toolName)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "conversation.item.create",
                ["item"] = new Dictionary<string, object>
                {
                    ["type"] = "function_call_output",
                    ["call_id"] = callId,
                    ["output"] = result
                }
            };
        }

        /// <summary>
        /// Creates a web_search built-in tool definition.
        /// </summary>
        public static object CreateWebSearchTool()
        {
            return new Dictionary<string, object>
            {
                ["type"] = "web_search"
            };
        }

        /// <summary>
        /// Creates an x_search built-in tool definition.
        /// </summary>
        /// <param name="allowedXHandles">Optional list of X handles to filter results.</param>
        public static object CreateXSearchTool(IEnumerable<string>? allowedXHandles = null)
        {
            var tool = new Dictionary<string, object>
            {
                ["type"] = "x_search"
            };

            if (allowedXHandles != null)
            {
                tool["allowed_x_handles"] = allowedXHandles.ToList();
            }

            return tool;
        }

        /// <summary>
        /// Creates a file_search built-in tool definition.
        /// </summary>
        /// <param name="vectorStoreIds">Collection IDs to search.</param>
        /// <param name="maxNumResults">Maximum number of results to return.</param>
        public static object CreateFileSearchTool(IEnumerable<string> vectorStoreIds, int? maxNumResults = null)
        {
            var tool = new Dictionary<string, object>
            {
                ["type"] = "file_search",
                ["vector_store_ids"] = vectorStoreIds.ToList()
            };

            if (maxNumResults.HasValue)
            {
                tool["max_num_results"] = maxNumResults.Value;
            }

            return tool;
        }

        private Dictionary<string, object> BuildParametersObject(ToolSchema schema)
        {
            var properties = new Dictionary<string, object>();

            foreach (var (name, param) in schema.Parameters)
            {
                properties[name] = BuildPropertyObject(param);
            }

            var result = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties
            };

            if (schema.Required.Count > 0)
            {
                result["required"] = schema.Required.ToList();
            }

            return result;
        }

        private object BuildPropertyObject(ToolParameter param)
        {
            var prop = new Dictionary<string, object>
            {
                ["type"] = TypeMapper.ToJsonSchemaType(param.Type)
            };

            if (!string.IsNullOrEmpty(param.Description))
            {
                prop["description"] = param.Description;
            }

            if (param.Enum != null && param.Enum.Count > 0)
            {
                prop["enum"] = param.Enum;
            }

            if (!string.IsNullOrEmpty(param.Format))
            {
                prop["format"] = param.Format;
            }

            if (param.Default != null)
            {
                prop["default"] = param.Default;
            }

            if (param.Type == ToolParameterType.Array && param.Items != null)
            {
                prop["items"] = BuildPropertyObject(param.Items);
            }

            if (param.Type == ToolParameterType.Object && param.Properties != null)
            {
                var nestedProps = new Dictionary<string, object>();

                foreach (var (name, nestedParam) in param.Properties)
                {
                    nestedProps[name] = BuildPropertyObject(nestedParam);
                }

                prop["properties"] = nestedProps;

                if (param.RequiredProperties?.Count > 0)
                {
                    prop["required"] = param.RequiredProperties.ToList();
                }
            }

            if (param.Minimum.HasValue)
            {
                prop["minimum"] = param.Minimum.Value;
            }

            if (param.Maximum.HasValue)
            {
                prop["maximum"] = param.Maximum.Value;
            }

            if (param.MinLength.HasValue)
            {
                prop["minLength"] = param.MinLength.Value;
            }

            if (param.MaxLength.HasValue)
            {
                prop["maxLength"] = param.MaxLength.Value;
            }

            if (!string.IsNullOrEmpty(param.Pattern))
            {
                prop["pattern"] = param.Pattern;
            }

            return prop;
        }
    }
}
