---
title: "Patterns"
description: "Pattern matching from C# 7 to C# 11: is with a type pattern, switch expressions, property patterns, relational and logical patterns, list patterns and when guards, with an if/else chain rewritten as a switch expression."
prev:
  link: /csharp/types/
  label: Types
next: false
learn:
  - title: "Pattern matching overview"
    url: https://learn.microsoft.com/dotnet/csharp/fundamentals/functional/pattern-matching
  - title: "Patterns (C# reference)"
    url: https://learn.microsoft.com/dotnet/csharp/language-reference/operators/patterns
  - title: "The switch expression"
    url: https://learn.microsoft.com/dotnet/csharp/language-reference/operators/switch-expression
---

By the end of this page you'll be able to read and write the pattern matching that modern C# uses in place of casts, `switch` statements and long `&&` chains. You'll be able to rewrite an if/else chain as a switch expression that gives the same results, and you'll know what the compiler says when a switch expression misses a case.

:::tip[Cheatsheet]
The [C# cheatsheet](/cheatsheets/csharp/#patterns) has this page's C# 3 → modern forms in one table.
:::

Every example was compiled and run with SDK 10.0.404 (C# 14) and the samples' settings, with nullable on and warnings as errors, on 12 November 2026. The examples work out an order's shipping cost, with these types:

```csharp
public sealed record Customer(string Name, string Country);

public sealed record Line(string Sku, int Quantity, decimal UnitPrice)
{
    public bool IsDigital => Sku.StartsWith("E-", StringComparison.Ordinal);
}

public sealed record Order(Customer Customer, IReadOnlyList<Line> Lines, bool Express = false)
{
    public decimal Subtotal => Lines.Sum(line => line.Quantity * line.UnitPrice);
}
```

## `is` with a type pattern (C# 7)

In C# 3, you'd test a type with `is`, then cast, or use `as` and check for null. A type pattern does both, and names the result:

```csharp
object placed = new OrderPlaced(cases[5]);
if (placed is OrderPlaced { Order.Customer.Country: "FR" } frenchOrder)
{
    Console.WriteLine(frenchOrder.Order.Subtotal);
}
```

`frenchOrder` exists only where the test passed. This one also checks a property, which is the [property pattern](#property-patterns-c-8-extended-in-c-10) below. `x is null` is a pattern too, a constant pattern (see [Null safety](/csharp/null-safety/#is-null-and-is-not-null-c-7-and-9)).

## Switch expressions (C# 8)

A switch expression picks a value. It takes the place of a `switch` statement whose cases each set a variable and `break`, or `return`. Each arm is a pattern, `=>`, and a result, and the first arm that matches wins. `_` matches anything:

```csharp
public static string Describe(object message) => message switch
{
    OrderPlaced { Order.Lines.Count: var count } => $"placed, {count} lines",
    OrderCancelled { Reason: var reason } => $"cancelled: {reason}",
    null => "nothing",
    _ => $"unknown: {message.GetType().Name}",
};
```

For an `OrderPlaced` with two lines it gives `placed, 2 lines`, for `OrderCancelled("O-7", "out of stock")` it gives `cancelled: out of stock`, and for `42` it gives `unknown: Int32`. `var count` captures a value from inside the pattern, for the arm's result to use.

## Property patterns (C# 8, extended in C# 10)

`{ Property: pattern }` matches an object whose properties match. It's also a null check: `{ }` matches anything that isn't null. C# 10 added the dotted form, `{ Customer.Country: "GB" }`, for a property of a property. C# 8 needed `{ Customer: { Country: "GB" } }`.

## Relational and logical patterns (C# 9)

`<`, `<=`, `>` and `>=` compare with a constant, and `and`, `or` and `not` combine patterns:

```csharp
public static string For(decimal subtotal) => subtotal switch
{
    < 20m => "small",
    >= 20m and < 100m => "medium",
    >= 100m => "large",
};
```

`For(150m)` is `"large"`. They work with `is` too: with `quantity` set to 7, `quantity is > 0 and <= 10` is `True`, and `gb is not { Country: "US" }` is `True` for a British customer. `not null` is the `is not null` from the Null safety page.

## List patterns (C# 11)

`[…]` matches a list or an array by its elements. `[]` is empty, `[var only]` has exactly one element, and `..` (a slice) stands for any number of elements:

```csharp
public static string FirstSku(IReadOnlyList<Line> lines) => lines switch
{
    [] => "none",
    [var only] => only.Sku,
    [var first, ..] => $"{first.Sku} and {lines.Count - 1} more",
};
```

For two lines, that gives `SKU-1 and 1 more`. Each element can be any pattern, so `[{ IsDigital: true }]` matches a list whose only line is digital.

`.. var rest` captures the slice, but only from a type that can be sliced with a range, such as an array or a string. On an `IReadOnlyList<Line>`, it fails (see [Pitfalls](#pitfalls)). On a `Line[]`, `lines is [var first, .. var rest]` works, and `rest` is an array.

## `when` guards (C# 7)

`when` adds a condition that a pattern can't express, such as one that compares two properties, or calls a method. The arm matches only if the pattern matches and the condition is true:

```csharp
{ Express: true } when order.Subtotal < 50m => 15m,
```

`case … when …:` has worked in `switch` statements since C# 7.

## An if/else chain, rewritten

Here are the shipping rules as you'd write them in C# 3 style:

```csharp
public static decimal CostOld(Order order)
{
    if (order.Lines.Count == 0)
    {
        return 0m;
    }

    if (order.Lines.Count == 1 && order.Lines[0].IsDigital)
    {
        return 0m;
    }

    if (order.Express && order.Subtotal < 50m)
    {
        return 15m;
    }

    if (order.Subtotal >= 50m)
    {
        return 0m;
    }

    if (order.Customer.Country == "GB")
    {
        return 3.95m;
    }

    if (order.Customer.Country == "IE" || order.Customer.Country == "FR" || order.Customer.Country == "DE")
    {
        return 7.50m;
    }

    return 12m;
}
```

And as one switch expression, with the rules in the same order:

```csharp
public static decimal Cost(Order order) => order switch
{
    { Lines: [] } => 0m,
    { Lines: [{ IsDigital: true }] } => 0m,
    { Express: true } when order.Subtotal < 50m => 15m,
    { Subtotal: >= 50m } => 0m,
    { Customer.Country: "GB" } => 3.95m,
    { Customer.Country: "IE" or "FR" or "DE" } => 7.50m,
    _ => 12m,
};
```

Both give the same cost for every case, measured on one order for each rule:

| Order                              | `CostOld` | `Cost` |
| ---------------------------------- | --------- | ------ |
| GB, no lines                       | 0         | 0      |
| GB, one digital line (9.99)        | 0         | 0      |
| GB, express, 20.00                 | 15        | 15     |
| FR, express, 60.00                 | 0         | 0      |
| GB, 20.00                          | 3.95      | 3.95   |
| FR, two lines, one digital (29.99) | 7.50      | 7.50   |
| US, 20.00                          | 12        | 12     |

The order of the arms matters exactly as the order of the `if`s does: the first match wins. Each rule is now one line, so the order is easier to see, and to check.

## When a switch expression misses a case

The compiler checks that a switch expression handles every possible input. Leave the `large` arm out of `For`:

```csharp
public static string For(decimal subtotal) => subtotal switch
{
    < 20m => "small",
    >= 20m and < 100m => "medium",
};
```

```text
error CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '100.00000000000000000000000000M' is not covered.
```

On its own it's a warning, and this primer's samples make it an error. If it's built anyway, as a warning, an input that no arm matches throws at run time. Measured with `For(150m)`:

```text
Unhandled exception. System.Runtime.CompilerServices.SwitchExpressionException: Non-exhaustive switch expression failed to match its input.
Unmatched value was 150.
```

A `switch` statement with no matching `case` silently does nothing, but a switch expression must produce a value, so it throws. Fix it with the missing arm, or with a final `_` arm, if there's an honest default.

## Coming from C# 3

| In C# 3 you'd…                                                 | Now                                      | See                                                            |
| -------------------------------------------------------------- | ---------------------------------------- | -------------------------------------------------------------- |
| `if (o is OrderPlaced) { var placed = (OrderPlaced)o; … }`     | `if (o is OrderPlaced placed) { … }`     | [Type pattern](#is-with-a-type-pattern-c-7)                    |
| A `switch` statement whose cases each set a result and `break` | `x switch { pattern => result, … }`      | [Switch expressions](#switch-expressions-c-8)                  |
| `order.Customer != null && order.Customer.Country == "GB"`     | `order is { Customer.Country: "GB" }`    | [Property patterns](#property-patterns-c-8-extended-in-c-10)   |
| `q > 0 && q <= 10`                                             | `q is > 0 and <= 10`                     | [Relational and logical](#relational-and-logical-patterns-c-9) |
| `c == "IE" \|\| c == "FR" \|\| c == "DE"`                      | `c is "IE" or "FR" or "DE"`              | [Relational and logical](#relational-and-logical-patterns-c-9) |
| `lines.Count == 1 && lines[0].IsDigital`                       | `lines is [{ IsDigital: true }]`         | [List patterns](#list-patterns-c-11)                           |
| A long if/else chain                                           | One switch expression, in the same order | [Rewritten](#an-ifelse-chain-rewritten)                        |

## Pitfalls

- **Arms in the wrong order.** The first match wins, so a broad arm above a narrow one hides it. Fix: put the most specific arms first, as you would the `if`s. The compiler catches an arm that can never match (`CS8510`), but not one that matches less than you meant.
- **A switch expression that isn't exhaustive.** It fails the build here (`CS8509`), and throws `SwitchExpressionException` at run time where it's only a warning. Fix: add the missing arm, or a `_` arm with an honest default. A `_` that hides an input you forgot is the same bug, quieter.
- **`.. var rest` on a list that can't be sliced.** On an `IReadOnlyList<Line>`, it fails with `error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'`. Fix: use `..` without capturing, or match an array.
- **A `when` guard that should be a pattern.** `when order.Subtotal >= 50m` works, but the compiler can't check it for exhaustiveness, and `{ Subtotal: >= 50m }` says the same thing. Fix: keep `when` for what a pattern can't say.
- **A property pattern that hides a null check.** `{ Customer.Country: "GB" }` is false when `Customer` is null, so null inputs fall through to a later arm, often `_`. Fix: if null needs its own answer, give it its own arm, `null => …`, first.

## Practise

[Exercise 14](/csharp/#exercise-14-modern-c), on the section's first page, ends with this page: its fourth step rewrites a pricing library's discount rules, an if/else chain that gets two cases wrong, as one switch expression.
