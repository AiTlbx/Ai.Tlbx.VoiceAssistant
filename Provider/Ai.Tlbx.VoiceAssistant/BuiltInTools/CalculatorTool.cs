using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ai.Tlbx.VoiceAssistant.BuiltInTools
{
    public enum CalculatorOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Power,
        Modulo
    }

    [Description("Perform mathematical calculations (add, subtract, multiply, divide, power, modulo)")]
    public class CalculatorTool : VoiceToolBase<CalculatorTool.Args>
    {
        public record Args(
            [property: Description("The mathematical operation to perform")] CalculatorOperation Operation,
            [property: Description("The first operand")] double A,
            [property: Description("The second operand")] double B
        );

        public override string Name => "calculate";

        public override Task<string> ExecuteAsync(Args args)
        {
            try
            {
                double result = args.Operation switch
                {
                    CalculatorOperation.Add => args.A + args.B,
                    CalculatorOperation.Subtract => args.A - args.B,
                    CalculatorOperation.Multiply => args.A * args.B,
                    CalculatorOperation.Divide => args.B != 0 ? args.A / args.B : double.NaN,
                    CalculatorOperation.Power => Math.Pow(args.A, args.B),
                    CalculatorOperation.Modulo => args.B != 0 ? args.A % args.B : double.NaN,
                    _ => double.NaN
                };

                if (double.IsNaN(result))
                {
                    return Task.FromResult(CreateErrorResult(
                        args.Operation == CalculatorOperation.Divide && args.B == 0 ? "Division by zero" :
                        args.Operation == CalculatorOperation.Modulo && args.B == 0 ? "Modulo by zero" :
                        $"Unknown operation: {args.Operation}"
                    ));
                }

                if (double.IsInfinity(result))
                {
                    return Task.FromResult(CreateErrorResult("Result is infinity"));
                }

                var response = new ToolSuccessResult<CalculatorResult>(new CalculatorResult
                {
                    Operation = args.Operation.ToString().ToLowerInvariant(),
                    A = args.A,
                    B = args.B,
                    Result = result,
                    Expression = $"{args.A} {GetOperatorSymbol(args.Operation)} {args.B} = {result}"
                });

                return Task.FromResult(JsonSerializer.Serialize(response, ToolResultsJsonContext.Default.ToolSuccessResultCalculatorResult));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CreateErrorResult($"Calculation error: {ex.Message}"));
            }
        }

        private static string GetOperatorSymbol(CalculatorOperation operation)
        {
            return operation switch
            {
                CalculatorOperation.Add => "+",
                CalculatorOperation.Subtract => "-",
                CalculatorOperation.Multiply => "×",
                CalculatorOperation.Divide => "÷",
                CalculatorOperation.Power => "^",
                CalculatorOperation.Modulo => "%",
                _ => "?"
            };
        }
    }
}
