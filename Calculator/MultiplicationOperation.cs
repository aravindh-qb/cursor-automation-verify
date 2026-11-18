namespace Calculator;

public sealed class MultiplicationOperation : ICalculatorOperation
{
    public string Symbol => "*";

    public double Calculate(double left, double right) => left * right;
}
