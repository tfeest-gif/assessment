# Rebalancing Assignment

Calculates how many shares to buy or sell so that every security in an account
reaches its target %, plus manual and automated tests for it.

**Answer for Account ABC:** buy **66 IBM** and sell **46 ORCL**; no trades for
MSFT, AAPL or HD. Exactly, that would be 66.67 and 45.45 shares. With whole
shares, sells are rounded up and buys rounded down, so the sale ($10,120) pays
for the purchase ($9,900).

## Contents

| File | Content |
|---|---|
| `TEST_CASES.md` | Assumptions and 23 test cases (manual), marked where automated |
| `src/Rebalancing/Rebalancer.cs` | The calculation (stand-in for the application under test) |
| `tests/Rebalancing.Tests/RebalancerTests.cs` | Functional tests, TC-01 to TC-11 |
| `tests/Rebalancing.Tests/ValidationTests.cs` | Invalid input, TC-12 to TC-19 |

C# / .NET with xUnit.

## Run

```bash
dotnet test
```

## With more time I would add

- Property-based tests (e.g. CsCheck): random accounts, checking rules like "buys are always paid for by sells"
- Mutation testing (Stryker.NET) to see whether the tests really catch bugs
- UI/E2E tests (e.g. Playwright) for TC-20 to TC-23
