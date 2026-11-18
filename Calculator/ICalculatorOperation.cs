namespace Calculator;

public interface ICalculatorOperation
{
    string Symbol { get; }

    double Calculate(double left, double right);
}
