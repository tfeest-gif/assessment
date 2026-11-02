# Rebalancing – Test Cases

## Expected result for Account ABC ($100,000)

| Security | Target % | Current % | Variance | Price | Trade value | Exact shares | Whole shares |
|---|---|---|---|---|---|---|---|
| IBM  | 20 | 10 | −10 | 150 | buy $10,000  | BUY 66.6667  | BUY 66  |
| MSFT | 20 | 20 | 0   | 90  | –            | HOLD 0       | HOLD 0  |
| ORCL | 20 | 30 | +10 | 220 | sell $10,000 | SELL 45.4545 | SELL 46 |
| AAPL | 20 | 20 | 0   | 450 | –            | HOLD 0       | HOLD 0  |
| HD   | 20 | 20 | 0   | 70  | –            | HOLD 0       | HOLD 0  |

**Formula:** shares = |current % − target %| / 100 × total assets / unit price

**To reach zero target variance:** buy 66.6667 IBM and sell 45.4545 ORCL. Only
fractional shares reach exactly zero; the current holdings are already fractional
too (e.g. 136.3636 ORCL, 222.2222 MSFT).

## Assumptions

1. Total assets = sum of all holdings. The account has **no cash**, so buys are paid for by sells.
2. No fees or taxes, and prices don't change during the rebalance.
3. Target % and current % must each add up to exactly 100, and each value must be between 0 and 100.
4. **Fractional mode** (default) gives the exact number of shares, rounded to 4 decimals. That rounding can leave differences of a few cents (Account ABC: buys $10,000.005, sells $9,999.99).
5. **Whole-share mode** (optional) is for brokers that only trade whole shares. It rounds sells **up** and buys **down**, so the sales always cover the purchases; the small surplus stays as cash ($220 for Account ABC). Zero variance is not reachable this way; each security ends up less than one share away from target. A sell never exceeds the shares held.
6. Money is calculated with `decimal`, not `double`, to avoid rounding errors.
7. No application was provided, so `Rebalancer.cs` is a minimal implementation for the automated tests to run against. The manual test cases assume the real application has an input form like the table in the assignment, with a Rebalance button and an output column.

## Test cases

Steps for each case: enter the total assets and the rows, choose the mode, run the
rebalance, and compare the output column with the expected result.

### Functional

| ID | Scenario | Input | Expected | Automated |
|---|---|---|---|---|
| TC-01 | Account ABC (default: exact shares) | see table above | IBM BUY 66.6667, ORCL SELL 45.4545, rest HOLD | ✔ |
| TC-02 | Account ABC, whole-share mode | see table above | IBM BUY 66, ORCL SELL 46, rest HOLD | ✔ |
| TC-03 | Target variance column | Account ABC | −10, 0, +10, 0, 0 | ✔ |
| TC-04 | Fractional trades reach zero variance | Account ABC | every security at $20,000 afterwards | ✔ |
| TC-05 | Buys are paid for by sells | Account ABC, whole | sold $10,120 ≥ bought $9,900 | ✔ |
| TC-06 | Already balanced | all 20/20 | all HOLD 0 | ✔ |
| TC-07 | Several buys and sells | $200,000; AAA 30/10/100, BBB 30/20/250, CCC 20/40/80, DDD 20/30/125 | BUY 400, BUY 80, SELL 500, SELL 160 | ✔ |
| TC-08 | Full liquidation + new position | $50,000; OLD 0/50/100, NEW 100/50/50 | OLD SELL 250, NEW BUY 500 | ✔ |
| TC-09 | Trade smaller than one share | $10,000; PRICY 50/49/5000, CHEAP 50/51/20 | PRICY HOLD 0, CHEAP SELL 5 | ✔ |
| TC-10 | Never sell more than held | $1,000; AAA 3/19/100 (holds 1.9), BBB 97/81/10 | AAA SELL 1.9 (not 2), BBB BUY 16 | ✔ |
| TC-11 | Decimal percentages | $90,000; AAA 33.34/40/12.50, BBB 33.33/30/7.25, CCC 33.33/30/300 | SELL 479.52, BUY 413.3793, BUY 9.99 | ✔ |

### Invalid input

Expected for every case: an error message, no trades.

| ID | Scenario | Input | Expected message | Automated |
|---|---|---|---|---|
| TC-12 | Target % over 100 | sum 105 | "Target % must add up to 100" | ✔ |
| TC-13 | Boundary: target % 100.01 | sum 100.01 | "Target % must add up to 100" | ✔ |
| TC-14 | Current % under 100 | sum 95 | "Current % must add up to 100" | ✔ |
| TC-15 | Percentage below 0 or above 100 | 110 / −10 | "between 0 and 100" | ✔ |
| TC-16 | Price 0 | price 0 | "price must be greater than 0" | ✔ |
| TC-17 | Duplicate security | IBM and ibm | "only appear once" | ✔ |
| TC-18 | Empty account | no rows | "no securities" | ✔ |
| TC-19 | Total assets 0 or negative | 0, −100,000 | "Total assets must be greater than 0" | ✔ |

### UI (manual only)

| ID | Scenario | Expected |
|---|---|---|
| TC-20 | Output column | read-only; BUY/SELL clearly visible |
| TC-21 | Change an input after calculating | old result disappears or recalculates |
| TC-22 | Text instead of a number in a field | field is marked, clear error, no crash |
| TC-23 | Large amounts ($1 billion) | numbers shown in full, no scientific notation |
