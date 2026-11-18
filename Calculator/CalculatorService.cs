namespace Calculator;

public sealed class CalculatorService
{
    private readonly IReadOnlyDictionary<string, ICalculatorOperation> _operations;

    public CalculatorService(IEnumerable<ICalculatorOperation> operations)
    {
        _operations = operations.ToDictionary(operation => operation.Symbol);
    }

    public bool TryCalculate(string symbol, double left, double right, out double result)
    {
        if (_operations.TryGetValue(symbol, out var operation))
        {
            result = operation.Calculate(left, right);
            return true;
        }

        result = default;
        return false;
    }
}
