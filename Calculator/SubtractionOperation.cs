namespace Calculator;

public sealed class SubtractionOperation : ICalculatorOperation
{
    public string Symbol => "-";

    public double Calculate(double left, double right) => left - right;
}
