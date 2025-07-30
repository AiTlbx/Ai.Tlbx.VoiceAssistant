# Voice Assistant Tool System

The Voice Assistant tool system allows AI providers to execute custom functions during conversations. This system has been enhanced with full parameter schema support while maintaining backward compatibility.

## Quick Start

### Basic Tool (Backward Compatible)

For simple tools that don't need parameters:

```csharp
public class TimeTool : IVoiceTool
{
    public string Name => "get_current_time";
    public string Description => "Gets the current date and time";
    
    public Task<string> ExecuteAsync(string argumentsJson)
    {
        var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return Task.FromResult($"Current time: {currentTime}");
    }
}
```

### Tool with Parameter Schema

For tools that accept parameters with validation:

```csharp
public class WeatherTool : IVoiceToolWithSchema
{
    public string Name => "get_weather";
    public string Description => "Get weather for a location";
    
    public ToolParameterSchema GetParameterSchema() => new()
    {
        Properties = new Dictionary<string, ParameterProperty>
        {
            ["location"] = new ParameterProperty
            {
                Type = "string",
                Description = "City and state, e.g., San Francisco, CA",
                MinLength = 1
            },
            ["unit"] = new ParameterProperty
            {
                Type = "string",
                Description = "Temperature unit",
                Enum = new List<string> { "celsius", "fahrenheit" },
                Default = "celsius"
            }
        },
        Required = new List<string> { "location" }
    };
    
    public async Task<string> ExecuteAsync(string argumentsJson)
    {
        // Parse and use the arguments
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        var location = args.GetProperty("location").GetString();
        // ... implementation
    }
}
```

### Using Base Classes

For convenience, use the provided base classes:

#### VoiceToolBase - For tools with schema but manual parsing:

```csharp
public class MyTool : VoiceToolBase
{
    public override string Name => "my_tool";
    public override string Description => "Does something useful";
    
    public override ToolParameterSchema GetParameterSchema() => new()
    {
        Properties = new Dictionary<string, ParameterProperty>
        {
            ["param1"] = new ParameterProperty { Type = "string" }
        }
    };
    
    protected override Task<string> ExecuteInternalAsync(string argumentsJson)
    {
        // Manual parsing
        var args = JsonSerializer.Deserialize<JsonElement>(argumentsJson);
        // ... implementation
    }
}
```

#### ValidatedVoiceToolBase<T> - For type-safe tools with automatic validation:

```csharp
public class CalculatorTool : ValidatedVoiceToolBase<CalculatorTool.Args>
{
    public class Args
    {
        public string Operation { get; set; }
        public double A { get; set; }
        public double B { get; set; }
    }
    
    public override string Name => "calculate";
    public override string Description => "Perform calculations";
    
    public override ToolParameterSchema GetParameterSchema() => new()
    {
        Properties = new Dictionary<string, ParameterProperty>
        {
            ["operation"] = new ParameterProperty
            {
                Type = "string",
                Enum = new List<string> { "add", "subtract", "multiply", "divide" }
            },
            ["a"] = new ParameterProperty { Type = "number" },
            ["b"] = new ParameterProperty { Type = "number" }
        },
        Required = new List<string> { "operation", "a", "b" }
    };
    
    protected override Task<string> ExecuteValidatedAsync(Args args)
    {
        var result = args.Operation switch
        {
            "add" => args.A + args.B,
            "subtract" => args.A - args.B,
            "multiply" => args.A * args.B,
            "divide" => args.A / args.B,
            _ => 0
        };
        
        return Task.FromResult(CreateSuccessResult(new { result }));
    }
}
```

## Registering Tools

### Individual Tools

```csharp
services.AddVoiceAssistant()
    .WithHardware<WebAudioAccess>()
    .AddTool<TimeTool>()
    .AddTool<WeatherTool>()
    .AddTool<CalculatorTool>()
    .WithOpenAi();
```

### All Built-in Tools

```csharp
services.AddVoiceAssistant()
    .WithHardware<WebAudioAccess>()
    .AddBuiltInTools(includeAdvanced: true)
    .WithOpenAi();
```

### Multiple Tools at Once

```csharp
services.AddVoiceAssistant()
    .WithHardware<WebAudioAccess>()
    .AddTools(typeof(TimeTool), typeof(WeatherTool), typeof(CalculatorTool))
    .WithOpenAi();
```

## Parameter Schema Reference

### Supported Types

- `"string"` - Text values
- `"number"` - Numeric values (integers or decimals)
- `"boolean"` - True/false values
- `"object"` - Nested objects
- `"array"` - Lists of values

### Property Attributes

- `Type` - The JSON Schema type (required)
- `Description` - Helps AI understand the parameter
- `Enum` - List of allowed values
- `Default` - Default value if not provided
- `Minimum`/`Maximum` - For numeric types
- `MinLength`/`MaxLength` - For string types
- `Pattern` - Regex pattern for strings
- `Properties` - For nested objects
- `Items` - Schema for array elements

## Built-in Tools

### TimeTool
- **Name**: `get_current_time`
- **Parameters**: None
- **Description**: Gets current date and time

### TimeToolWithSchema
- **Name**: `get_current_time_advanced`
- **Parameters**: 
  - `timeZone` (optional): Target timezone
  - `format` (optional): Output format (full, date, time, iso8601, unix)

### WeatherTool
- **Name**: `get_weather`
- **Parameters**:
  - `location` (required): City and state/country
  - `unit` (optional): Temperature unit (celsius, fahrenheit)

### CalculatorTool
- **Name**: `calculate`
- **Parameters**:
  - `operation` (required): add, subtract, multiply, divide, power, modulo
  - `a` (required): First number
  - `b` (required): Second number

### WeatherLookupTool
- **Name**: `weather_lookup`
- **Parameters**:
  - `place` (required): Location (city, address, or coordinates)
  - `time` (optional): When to get weather - 'now', 'today', 'tomorrow', or specific date/time
  - `unit` (optional): Temperature unit (celsius, fahrenheit, kelvin)
  - `detailed` (optional): Include extended forecast information

## Best Practices

1. **Use Descriptive Names**: Tool names should clearly indicate their function
2. **Provide Clear Descriptions**: Help the AI understand when to use your tool
3. **Validate Input**: Use `ValidatedVoiceToolBase<T>` for automatic validation
4. **Handle Errors Gracefully**: Return error messages in a consistent format
5. **Keep Tools Focused**: Each tool should do one thing well
6. **Document Parameters**: Use descriptions to explain each parameter's purpose

## Migration Guide

Existing tools using `IVoiceTool` continue to work without changes. To add parameter schema support:

1. Change interface from `IVoiceTool` to `IVoiceToolWithSchema`
2. Implement `GetParameterSchema()` method
3. Optionally, inherit from base classes for additional features

The system maintains full backward compatibility - tools without schemas send empty parameter definitions to AI providers.