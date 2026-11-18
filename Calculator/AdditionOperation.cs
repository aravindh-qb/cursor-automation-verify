namespace Calculator;

public sealed class AdditionOperation : ICalculatorOperation
{
    public string Symbol => "+";

    public double Calculate(double left, double right) => left + right;
}
