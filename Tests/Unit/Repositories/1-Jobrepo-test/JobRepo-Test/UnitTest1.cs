namespace MyApp.Tests;

public class Calculator
{
    public static int Add(int var1, int var2)
    {
        return var1 + var2;
    }
}

public class CalculatorTests
{
    [Fact]
    public void Add_TwoPositiveNumbers_ReturnsSum()
    {
        var result = Calculator.Add(2, 3);
        Assert.Equal(5, result);
    }
}