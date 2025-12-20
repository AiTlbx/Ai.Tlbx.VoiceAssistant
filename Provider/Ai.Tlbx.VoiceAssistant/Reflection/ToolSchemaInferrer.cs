using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Ai.Tlbx.VoiceAssistant.Models;

namespace Ai.Tlbx.VoiceAssistant.Reflection
{
    /// <summary>
    /// Infers ToolSchema from C# types using reflection.
    /// Schemas are cached for performance.
    /// </summary>
    public static class ToolSchemaInferrer
    {
        private static readonly ConcurrentDictionary<Type, ToolSchema> _cache = new();

        /// <summary>
        /// Infers a ToolSchema from the given type.
        /// Results are cached.
        /// </summary>
        /// <typeparam name="T">The type to infer schema from.</typeparam>
        /// <returns>The inferred ToolSchema.</returns>
        public static ToolSchema InferSchema<T>() where T : notnull
        {
            return InferSchema(typeof(T));
        }

        /// <summary>
        /// Infers a ToolSchema from the given type.
        /// Results are cached.
        /// </summary>
        /// <param name="type">The type to infer schema from.</param>
        /// <returns>The inferred ToolSchema.</returns>
        public static ToolSchema InferSchema(Type type)
        {
            return _cache.GetOrAdd(type, BuildSchema);
        }

        private static ToolSchema BuildSchema(Type type)
        {
            var schema = new ToolSchema();

            // Check if it's a record with a primary constructor
            var constructor = type.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (constructor != null && constructor.GetParameters().Length > 0)
            {
                // Use constructor parameters (common for records)
                foreach (var param in constructor.GetParameters())
                {
                    var paramName = ToSnakeCase(param.Name ?? "unknown");
                    var toolParam = BuildParameter(param.ParameterType, param);

                    schema.Parameters[paramName] = toolParam;

                    if (IsRequired(param))
                    {
                        schema.Required.Add(paramName);
                    }
                }
            }
            else
            {
                // Fall back to public properties with init/set
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite);

                foreach (var prop in properties)
                {
                    var propName = ToSnakeCase(prop.Name);
                    var toolParam = BuildParameter(prop.PropertyType, prop);

                    schema.Parameters[propName] = toolParam;

                    if (IsRequired(prop))
                    {
                        schema.Required.Add(propName);
                    }
                }
            }

            return schema;
        }

        private static ToolParameter BuildParameter(Type type, object? memberInfo)
        {
            var param = new ToolParameter
            {
                Description = GetDescription(memberInfo)
            };

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                param.Nullable = true;
                type = underlyingType;
            }

            // Handle nullable reference types via NullabilityInfo
            if (!type.IsValueType && IsNullableReferenceType(memberInfo))
            {
                param.Nullable = true;
            }

            // Map type
            param.Type = TypeMapper.MapType(type);

            // Handle enums
            if (type.IsEnum)
            {
                param.Type = ToolParameterType.String;
                param.Enum = Enum.GetNames(type).Select(ToSnakeCase).ToList();
            }

            // Handle arrays and lists
            if (type.IsArray)
            {
                param.Type = ToolParameterType.Array;
                var elementType = type.GetElementType();
                if (elementType != null)
                {
                    param.Items = BuildParameter(elementType, null);
                }
            }
            else if (type.IsGenericType && IsListType(type))
            {
                param.Type = ToolParameterType.Array;
                param.Items = BuildParameter(type.GetGenericArguments()[0], null);
            }

            // Handle nested objects
            if (param.Type == ToolParameterType.Object && !type.IsEnum && type != typeof(object))
            {
                param.Properties = new Dictionary<string, ToolParameter>();
                param.RequiredProperties = new List<string>();

                var nestedProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite);

                foreach (var prop in nestedProps)
                {
                    var propName = ToSnakeCase(prop.Name);
                    param.Properties[propName] = BuildParameter(prop.PropertyType, prop);

                    if (IsRequired(prop))
                    {
                        param.RequiredProperties.Add(propName);
                    }
                }
            }

            // Handle format hints
            param.Format = TypeMapper.GetFormat(type);

            // Handle default values from parameter info
            if (memberInfo is ParameterInfo pi && pi.HasDefaultValue && pi.DefaultValue != null)
            {
                param.Default = ConvertDefaultValue(pi.DefaultValue, type);
            }

            return param;
        }

        private static bool IsRequired(ParameterInfo param)
        {
            // Required if:
            // 1. Not nullable value type
            // 2. Not nullable reference type
            // 3. No default value

            if (param.HasDefaultValue)
                return false;

            if (Nullable.GetUnderlyingType(param.ParameterType) != null)
                return false;

            if (IsNullableReferenceType(param))
                return false;

            return true;
        }

        private static bool IsRequired(PropertyInfo prop)
        {
            // Required if non-nullable and has 'required' keyword
            // In practice, we check for nullability

            if (Nullable.GetUnderlyingType(prop.PropertyType) != null)
                return false;

            if (IsNullableReferenceType(prop))
                return false;

            // Check for required keyword (via attribute)
            var requiredAttr = prop.GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>();
            if (requiredAttr != null)
                return true;

            // For non-nullable reference types without required, we still consider them required
            return !prop.PropertyType.IsValueType;
        }

        private static bool IsNullableReferenceType(object? memberInfo)
        {
            if (memberInfo == null)
                return false;

            NullabilityInfoContext context = new();

            try
            {
                NullabilityInfo? nullabilityInfo = memberInfo switch
                {
                    ParameterInfo pi => context.Create(pi),
                    PropertyInfo prop => context.Create(prop),
                    _ => null
                };

                return nullabilityInfo?.WriteState == NullabilityState.Nullable ||
                       nullabilityInfo?.ReadState == NullabilityState.Nullable;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsListType(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var genericDef = type.GetGenericTypeDefinition();
            return genericDef == typeof(List<>) ||
                   genericDef == typeof(IList<>) ||
                   genericDef == typeof(ICollection<>) ||
                   genericDef == typeof(IEnumerable<>);
        }

        private static string? GetDescription(object? memberInfo)
        {
            if (memberInfo == null)
                return null;

            DescriptionAttribute? attr = memberInfo switch
            {
                ParameterInfo pi => pi.GetCustomAttribute<DescriptionAttribute>(),
                PropertyInfo prop => prop.GetCustomAttribute<DescriptionAttribute>(),
                _ => null
            };

            return attr?.Description;
        }

        private static object? ConvertDefaultValue(object defaultValue, Type type)
        {
            // For enums, convert to snake_case string
            if (type.IsEnum)
            {
                return ToSnakeCase(defaultValue.ToString() ?? "");
            }

            return defaultValue;
        }

        private static string ToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var result = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2");
            return result.ToLowerInvariant();
        }
    }
}
