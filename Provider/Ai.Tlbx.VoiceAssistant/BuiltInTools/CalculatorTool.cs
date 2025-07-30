using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ai.Tlbx.VoiceAssistant.Models;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    /// <summary>
    /// Calculator tool for performing mathematical operations.
    /// </summary>
    public class CalculatorTool : ValidatedVoiceToolBase<CalculatorTool.CalculatorArgs>
    {
        /// <summary>
        /// Arguments for the calculator tool.
        /// </summary>
        public class CalculatorArgs
        {
            /// <summary>
            /// The mathematical operation to perform.
            /// </summary>
            public string Operation { get; set; } = "add";

            /// <summary>
            /// The first operand.
            /// </summary>
            public double A { get; set; }

            /// <summary>
            /// The second operand.
            /// </summary>
            public double B { get; set; }
        }

        /// <inheritdoc/>
        public override string Name => "calculate";

        /// <inheritdoc/>
        public override string Description => "Perform mathematical calculations (add, subtract, multiply, divide, power, modulo)";

        /// <inheritdoc/>
        public override ToolParameterSchema GetParameterSchema() => new()
        {
            Properties = new Dictionary<string, ParameterProperty>
            {
                ["operation"] = new ParameterProperty
                {
                    Type = "string",
                    Description = "The mathematical operation to perform",
                    Enum = new List<string> { "add", "subtract", "multiply", "divide", "power", "modulo" }
                },
                ["a"] = new ParameterProperty
                {
                    Type = "number",
                    Description = "The first operand"
                },
                ["b"] = new ParameterProperty
                {
                    Type = "number",
                    Description = "The second operand"
                }
            },
            Required = new List<string> { "operation", "a", "b" }
        };

        /// <inheritdoc/>
        protected override Task<string> ExecuteValidatedAsync(CalculatorArgs args)
        {
            try
            {
                double result = args.Operation.ToLowerInvariant() switch
                {
                    "add" => args.A + args.B,
                    "subtract" => args.A - args.B,
                    "multiply" => args.A * args.B,
                    "divide" => args.B != 0 ? args.A / args.B : double.NaN,
                    "power" => Math.Pow(args.A, args.B),
                    "modulo" => args.B != 0 ? args.A % args.B : double.NaN,
                    _ => double.NaN
                };

                if (double.IsNaN(result))
                {
                    return Task.FromResult(CreateErrorResult(
                        args.Operation == "divide" && args.B == 0 ? "Division by zero" : 
                        args.Operation == "modulo" && args.B == 0 ? "Modulo by zero" :
                        $"Unknown operation: {args.Operation}"
                    ));
                }

                if (double.IsInfinity(result))
                {
                    return Task.FromResult(CreateErrorResult("Result is infinity"));
                }

                var response = new
                {
                    operation = args.Operation,
                    a = args.A,
                    b = args.B,
                    result = result,
                    expression = $"{args.A} {GetOperatorSymbol(args.Operation)} {args.B} = {result}"
                };

                return Task.FromResult(CreateSuccessResult(response));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CreateErrorResult($"Calculation error: {ex.Message}"));
            }
        }

        private string GetOperatorSymbol(string operation)
        {
            return operation.ToLowerInvariant() switch
            {
                "add" => "+",
                "subtract" => "-",
                "multiply" => "×",
                "divide" => "÷",
                "power" => "^",
                "modulo" => "%",
                _ => "?"
            };
        }
    }
}