using Rebalancing;

namespace Rebalancing.Tests;

/// <summary>Invalid input must be rejected with a clear message instead of producing trades.</summary>
public class ValidationTests
{
    private static Position[] Valid() =>
    [
        new("IBM", 50, 40, 150),
        new("ORCL", 50, 60, 220),
    ];

    private static Position[] WithFirstRow(Position row)
    {
        var rows = Valid();
        rows[0] = row;
        return rows;
    }

    public static TheoryData<string, Position[], string> InvalidAccounts() => new()
    {
        { "TC-12 target % over 100", WithFirstRow(new("IBM", 55, 40, 150)), "Target % must add up to 100" },
        { "TC-13 target % 100.01 (boundary)", WithFirstRow(new("IBM", 50.01m, 40, 150)), "Target % must add up to 100" },
        { "TC-14 current % under 100", WithFirstRow(new("IBM", 50, 35, 150)), "Current % must add up to 100" },
        { "TC-15 negative percentage", [new("IBM", 110, 40, 150), new("ORCL", -10, 60, 220)], "between 0 and 100" },
        { "TC-16 price 0", WithFirstRow(new("IBM", 50, 40, 0)), "price must be greater than 0" },
        { "TC-17 duplicate security", [new("IBM", 50, 40, 150), new("ibm", 50, 60, 150)], "only appear once" },
        { "TC-18 empty account", [], "no securities" },
    };

    [Theory]
    [MemberData(nameof(InvalidAccounts))]
    public void Invalid_account_is_rejected(string testCase, Position[] positions, string message)
    {
        var ex = Assert.Throws<ArgumentException>(() => Rebalancer.Rebalance(100_000, positions));

        Assert.Contains(message, ex.Message);
        _ = testCase; // only used to name the test
    }

    // TC-19
    [Theory]
    [InlineData(0)]
    [InlineData(-100_000)]
    public void Total_assets_must_be_positive(int total)
    {
        var ex = Assert.Throws<ArgumentException>(() => Rebalancer.Rebalance(total, Valid()));

        Assert.Contains("Total assets must be greater than 0", ex.Message);
    }
}
