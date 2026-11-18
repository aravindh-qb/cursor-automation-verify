namespace Calculator;

public sealed class DivisionOperation : ICalculatorOperation
{
    public string Symbol => "/";

    public double Calculate(double left, double right) => left / right;
}
