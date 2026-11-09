using WTExpCalc.Services.Calculator;
using Xunit;

namespace WTExpCalc.Tests;

public class CalculatorExpressionTests
{
    [Theory]
    [InlineData("2+3×4", 14)]
    [InlineData("(2+3)×4", 20)]
    [InlineData("100+10%", 110)]
    [InlineData("100-10%", 90)]
    [InlineData("50%", 0.5)]
    [InlineData("1,5+2", 3.5)]
    [InlineData("2(3+4)", 14)]
    public void TryEvaluate_ReturnsExpectedValue(string expression, double expected)
    {
        Assert.True(CalculatorExpression.TryEvaluate(expression, out var actual));
        Assert.Equal(expected, actual, 6);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1+")]
    [InlineData("(1+2")]
    [InlineData("1/0")]
    [InlineData("abc")]
    public void TryEvaluate_RejectsInvalidExpressions(string expression)
    {
        Assert.False(CalculatorExpression.TryEvaluate(expression, out _));
    }

    [Fact]
    public void NormalizeExpression_ReplacesOperatorsAndCollapsesDuplicates()
    {
        Assert.Equal("2×3", CalculatorExpression.NormalizeExpression("2*3"));
        Assert.Equal("1+2", CalculatorExpression.NormalizeExpression("1++2"));
        Assert.Equal("0,5", CalculatorExpression.NormalizeExpression(",5"));
    }
}
