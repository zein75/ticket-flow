using TicketFlow.Domain.ValueObjects;

namespace TicketFlow.UnitTests.Domain;

public sealed class MoneyTests
{
    [Fact]
    public void Add_ReturnsSum_WhenCurrencyIsSame()
    {
        var left = new Money(100, "usd");
        var right = new Money(50, "USD");

        var result = left + right;

        Assert.Equal(150, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Add_Throws_WhenCurrencyIsDifferent()
    {
        var left = new Money(100, "USD");
        var right = new Money(50, "EUR");

        Assert.Throws<InvalidOperationException>(() => left + right);
    }
}
