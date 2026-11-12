---
title: C# cheatsheet
description: The C# 3 way and the modern way, side by side, with the version each feature arrived in. Less ceremony, null safety, types and patterns, on one page.
prev: false
next: false
learn:
  - title: The history of C#
    url: https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-version-history
  - title: What's new in C# 14
    url: https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14
  - title: C# language reference
    url: https://learn.microsoft.com/dotnet/csharp/language-reference/
---

Back to [Modern C# at a glance](/csharp/).

Each row is the C# 3 way, the modern way, and the C# version that introduced it. Lambdas, LINQ, `var`, auto-properties, object and collection initializers, extension methods and `??` are all C# 3 or older, and haven't changed.

## Less ceremony

| C# 3                                                           | Modern                                                                                                   | C#                                      |
| -------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- | --------------------------------------- |
| `namespace Shop { … }`                                         | [`namespace Shop;`](/csharp/#file-scoped-namespaces-c-10)                                                | 10                                      |
| `using System.Linq;` at the top of every file                  | [Implicit usings](/csharp/#implicit-and-global-usings-net-6-c-10), and `global using Shop.Pricing;` once | .NET 6, 10                              |
| `static void Main(string[] args) { … }`                        | [Statements in `Program.cs`](/csharp/#top-level-statements-c-9)                                          | 9                                       |
| `Dictionary<string, int> map = new Dictionary<string, int>();` | [`Dictionary<string, int> map = new();`](/csharp/#target-typed-new-c-9)                                  | 9                                       |
| `new List<string> { "SKU-1", "SKU-2" }`                        | [`["SKU-1", "SKU-2"]`](/csharp/#collection-expressions-c-12)                                             | 12                                      |
| `new List<OrderLine>()`                                        | `[]`                                                                                                     | 12                                      |
| `skus.Concat(more).ToArray()`                                  | `[.. skus, .. more]`                                                                                     | 12                                      |
| `params decimal[] amounts`                                     | [`params IEnumerable<decimal> amounts`](/csharp/#params-collections-c-13)                                | 13                                      |
| `"{\"sku\": \"" + sku + "\"}"`                                 | [`$$"""{"sku": "{{sku}}"}"""`](/csharp/#raw-string-literals-c-11)                                        | 11                                      |
| `n + " lines"`                                                 | [`$"{n} lines"`](/csharp/#string-interpolation-c-6)                                                      | 6                                       |
| `get { return _quantities.Count; }`                            | [`=> _quantities.Count;`](/csharp/#expression-bodied-members-c-6-and-7)                                  | 6, and 7 for constructors and accessors |
| `using (var reader = new StreamReader(path)) { … }`            | [`using var reader = new StreamReader(path);`](/csharp/#using-declarations-c-8)                          | 8                                       |
| A private helper method that only one method calls             | [A `static` local function](/csharp/#local-functions-c-7)                                                | 7                                       |

## Null safety

| C# 3                                                         | Modern                                                                                                                                    | C#                                                   |
| ------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------- |
| `string` that might be null, and nothing says so             | `string` is never null, [`string?`](/csharp/null-safety/#string-and-string-and-the-warnings) might be, with `<Nullable>enable</Nullable>` | 8                                                    |
| Nullable off for a file                                      | [`#nullable disable`](/csharp/null-safety/#turning-it-on-in-old-code-a-file-at-a-time) at its top, while you migrate it                   | 8                                                    |
| `if (c != null) { c.Email = e; }`                            | [`c?.Email = e;`](/csharp/null-safety/#null-conditional-assignment-c-14)                                                                  | 14                                                   |
| `if (x == null) { x = "default"; }`                          | [`x ??= "default";`](/csharp/null-safety/#the-null-operators--c-6-and--c-8)                                                               | 8                                                    |
| `x == null`, `x != null`                                     | [`x is null` (7), `x is not null` (9)](/csharp/null-safety/#is-null-and-is-not-null-c-7-and-9)                                            | 7, 9                                                 |
| An `out` parameter that's set when the method returns `true` | [`[NotNullWhen(true)] out Customer? customer`](/csharp/null-safety/#the-nullable-attributes)                                              | A library attribute that the compiler reads, since 8 |

`?.` on a read, as in `c?.Email`, is C# 6.

## Types

| C# 3                                                                            | Modern                                                                                                                                            | C#                                    |
| ------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------- |
| A class with private fields, a constructor, getters, `Equals` and `GetHashCode` | [`public sealed record Line(string Sku, int Quantity, decimal UnitPrice);`](/csharp/types/#records-c-9)                                           | 9                                     |
| A struct with its own `Equals`                                                  | [`public readonly record struct Money(decimal Amount, string Currency);`](/csharp/types/#record-structs-c-10)                                     | 10                                    |
| A read-only property set by the constructor                                     | [`public decimal Discount { get; init; }`](/csharp/types/#init-c-9-and-required-c-11)                                                             | 9                                     |
| A constructor parameter that callers must pass                                  | [`public required string Id { get; init; }`](/csharp/types/#init-c-9-and-required-c-11)                                                           | 11                                    |
| Copy every field by hand, and change one                                        | [`p with { Discount = 0.05m }`](/csharp/types/#with-c-9)                                                                                          | 9; on structs and anonymous types, 10 |
| A constructor that only stores its parameters                                   | [`public sealed class Pricing(CustomerDirectory directory)`](/csharp/types/#primary-constructors-c-12)                                            | 12 (classes and structs)              |
| `out` parameters, or a small class, to return two values                        | [`(decimal Total, int Count) Summary()`](/csharp/types/#tuples-and-deconstruction-c-7), then `var (total, count) = Summary();`                    | 7                                     |
| A backing field for a property that does some work                              | [`set => field = value.Trim();`](/csharp/types/#the-field-keyword-c-14)                                                                           | 14                                    |
| `public static bool IsGiftCard(this string s)`                                  | [`extension(string s) { public bool IsGiftCard => … }`](/csharp/types/#extension-members-c-14), which can also hold properties and static members | 14                                    |

## Patterns

| C# 3                                                           | Modern                                                                                                 | C#                                               |
| -------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ | ------------------------------------------------ |
| `if (o is Line) { var line = (Line)o; … }`                     | [`if (o is Line line) { … }`](/csharp/patterns/#is-with-a-type-pattern-c-7)                            | 7                                                |
| A `switch` statement whose cases each set a result and `break` | [`q switch { < 10 => "small", _ => "large" }`](/csharp/patterns/#switch-expressions-c-8)               | 8                                                |
| `c != null && c.IsGold`                                        | [`c is { IsGold: true }`](/csharp/patterns/#property-patterns-c-8-extended-in-c-10)                    | 8; nested, as in `{ Customer.IsGold: true }`, 10 |
| `q >= 0 && q < 100`                                            | [`q is >= 0 and < 100`](/csharp/patterns/#relational-and-logical-patterns-c-9)                         | 9                                                |
| `!(o is Customer)`                                             | [`o is not Customer`](/csharp/patterns/#relational-and-logical-patterns-c-9)                           | 9                                                |
| `lines.Count == 1 && lines[0].Sku == "GIFT-50"`                | [`lines is [{ Sku: "GIFT-50" }]`](/csharp/patterns/#list-patterns-c-11)                                | 11                                               |
| `case` guards                                                  | [`case int n when n > 0:`](/csharp/patterns/#when-guards-c-7), and `when` on a switch expression's arm | 7                                                |

## Not here

`async` and `await`, `IAsyncEnumerable<T>` and `Span<T>` are on the [Async & performance cheatsheet](/cheatsheets/async-and-performance/).
