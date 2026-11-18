using System.Globalization;

namespace Calculator;

public sealed class CalculatorApplication
{
    private readonly CalculatorService _calculator;
    private readonly TextReader _input;
    private readonly TextWriter _output;

    public CalculatorApplication(CalculatorService calculator, TextReader input, TextWriter output)
    {
        _calculator = calculator;
        _input = input;
        _output = output;
    }

    public void Run()
    {
        _output.WriteLine("Basic Calculator (+, -, *, /). Enter q to quit.");

        while (true)
        {
            _output.Write("First number: ");
            var firstInput = _input.ReadLine();
            if (firstInput is null || firstInput.Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!TryReadNumber(firstInput, out var left))
            {
                _output.WriteLine("Please enter a valid number.");
                continue;
            }

            _output.Write("Operator (+, -, *, /): ");
            var symbol = _input.ReadLine();
            if (symbol is null)
            {
                return;
            }

            _output.Write("Second number: ");
            var secondInput = _input.ReadLine();
            if (secondInput is null)
            {
                return;
            }

            if (!TryReadNumber(secondInput, out var right))
            {
                _output.WriteLine("Please enter a valid number.");
                continue;
            }

            if (!_calculator.TryCalculate(symbol, left, right, out var result))
            {
                _output.WriteLine("Unsupported operator. Use +, -, *, or /.");
                continue;
            }

            _output.WriteLine($"Result: {result}");
        }
    }

    private static bool TryReadNumber(string input, out double number) =>
        double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out number)
        && double.IsFinite(number);
}
