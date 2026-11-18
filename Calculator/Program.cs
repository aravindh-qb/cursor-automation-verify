using Calculator;

var service = new CalculatorService(
[
    new AdditionOperation(),
    new SubtractionOperation(),
    new MultiplicationOperation()
]);

new CalculatorApplication(service, Console.In, Console.Out).Run();
