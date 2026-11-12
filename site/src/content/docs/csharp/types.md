---
title: "Types: records, constructors and members"
description: "Records and record structs, init and required members, with-expressions, primary constructors on classes and on records, tuples and deconstruction, the field keyword, and C# 14's extension members."
sidebar:
  label: Types
prev:
  link: /csharp/null-safety/
  label: Null safety
next:
  link: /csharp/patterns/
  label: Patterns
learn:
  - title: "Records"
    url: https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/record
  - title: "Primary constructors"
    url: https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/instance-constructors#primary-constructors
  - title: "Extension members"
    url: https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/extension-methods
---

By the end of this page you'll be able to replace a C# 3 class of private fields, a constructor and getters with a record, make a type's members required and set-once, copy a value with one change, and tell a record's primary constructor from a class's. You'll also be able to read the `field` keyword and C# 14's extension blocks when you meet them.

:::tip[Cheatsheet]
The [C# cheatsheet](/cheatsheets/csharp/#types) has this page's C# 3 → modern forms in one table.
:::

Every example was compiled and run with SDK 10.0.404 (C# 14) and the samples' settings, with nullable on and warnings as errors, on 12 November 2026.

## Records (C# 9)

A record is a class that the compiler fills in for you. Write its properties, and it generates value equality (`Equals`, `GetHashCode`, `==` and `!=`), a readable `ToString`, and a copy for `with`. The shortest form is a positional record:

```csharp
public sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}
```

Two records with the same values are equal, though they're different objects:

```csharp
var first = new OrderLine("SKU-1", 2, 9.99m);
var second = new OrderLine("SKU-1", 2, 9.99m);
Console.WriteLine(first == second);
Console.WriteLine(first.Equals(second));
Console.WriteLine(ReferenceEquals(first, second));
Console.WriteLine(first);
```

That prints:

```text
True
True
False
OrderLine { Sku = SKU-1, Quantity = 2, UnitPrice = 9.99, Total = 19.98 }
```

`ToString` lists every public property, including the computed `Total`. A class would print `Shop.OrderLine`, and its `==` would compare references. The C# 3 equivalent is the class on the [section's first page](/csharp/#one-class-c-3-and-c-14), plus an `Equals`, a `GetHashCode`, an `==` and a `ToString`, all by hand.

A positional record also **deconstructs** into its parts:

```csharp
var (sku, quantity, _) = first;
Console.WriteLine($"{sku} × {quantity}");
```

That prints `SKU-1 × 2`. The `_` is a discard, for the part you don't need.

## Record structs (C# 10)

`record struct` is the same idea for a value type. Make it `readonly`, so that it can't be changed after it's created:

```csharp
public readonly record struct Money(decimal Amount, string Currency);
```

```csharp
var price = new Money(10m, "GBP");
Console.WriteLine(price);
Console.WriteLine(price with { Amount = 12m });
```

That prints `Money { Amount = 10, Currency = GBP }`, then `Money { Amount = 12, Currency = GBP }`. A plain `record`, or `record class`, is a reference type.

## `init` (C# 9) and `required` (C# 11)

A record doesn't have to be positional. With named properties, `init` means "can be set in an object initializer, and never after", and `required` means "must be set in it":

```csharp
public sealed record Order
{
    public required string CustomerId { get; init; }

    public required IReadOnlyList<OrderLine> Lines { get; init; }

    public decimal Discount { get; init; }
}
```

The compiler enforces both. Leaving a required member out:

```csharp
var order = new Order { CustomerId = "C-42" };
```

```text
error CS9035: Required member 'Order.Lines' must be set in the object initializer or attribute constructor.
```

Setting an `init` member later:

```csharp
var order = new Order { CustomerId = "C-42", Lines = [first] };
order.Discount = 0.1m;
```

```text
error CS8852: Init-only property or indexer 'Order.Discount' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
```

A positional record's properties are `init` too, so `first.Quantity = 3;` gives the same `CS8852`. `init` and `required` work on classes and structs as well as records. With nullable on, `required` is also how a non-nullable property avoids `CS8618` (see [Null safety](/csharp/null-safety/#turning-it-on-in-old-code-a-file-at-a-time)).

Object initializers are C# 3, and work as you remember. What's new is that `init` and `required` make them the way to create an immutable object, in place of a constructor with many parameters.

## `with` (C# 9)

`with` copies a record and changes the properties you name. The original is left alone:

```csharp
var order = new Order { CustomerId = "C-42", Lines = [first] };
var discounted = order with { Discount = 0.05m };
Console.WriteLine(order.Discount);
Console.WriteLine(discounted.Discount);
```

That prints `0`, then `0.05`. Since C# 10, `with` works on structs and anonymous types too:

```csharp
var label = new { Sku = "SKU-1", Quantity = 2 };
Console.WriteLine(label with { Quantity = 3 });
```

That prints `{ Sku = SKU-1, Quantity = 3 }`. The copy is shallow: a property that holds a list holds the same list in both (see [Pitfalls](#pitfalls)).

## Primary constructors (C# 12)

A parameter list after a type's name is a **primary constructor**. Records have had them since C# 9, and since C# 12, classes and structs can have them too. **They mean different things:**

- **On a record, each parameter becomes a public `init` property.** `OrderLine(string Sku, …)` above has an `OrderLine.Sku` property, and it's part of equality, `ToString` and deconstruction.
- **On a class or struct, each parameter is only captured.** It's in scope in the whole type, for its members to use, but it isn't a property or a public field, and nothing outside can see it.

```csharp
public sealed class Pricing(CustomerDirectory directory, decimal goldRate)
{
    public decimal RateFor(string customerId) => directory.Find(customerId) is null ? 0m : goldRate;
}
```

Outside the class, `goldRate` doesn't exist:

```csharp
var pricing = new Pricing(directory, 0.10m);
Console.WriteLine(pricing.goldRate);
```

```text
error CS1061: 'Pricing' does not contain a definition for 'goldRate' and no accessible extension method 'goldRate' accepting a first argument of type 'Pricing' could be found (are you missing a using directive or an assembly reference?)
```

That makes a class's primary constructor a short way to take dependencies, which is how the [Dependency Injection page](/aspnet-core/dependency-injection/#constructor-and-primary-constructor-injection) uses it. In C# 3 you'd write a `private readonly` field and a constructor that assigns it. A captured parameter isn't `readonly`, though: the class's own code can assign to it.

## Tuples and deconstruction (C# 7)

A tuple returns several values without a class to hold them, and its elements can have names:

```csharp
public static (decimal Subtotal, int Count) Summarise(IReadOnlyList<OrderLine> lines) =>
    (lines.Sum(line => line.Total), lines.Count);
```

```csharp
var (subtotal, count) = Totals.Summarise([first, new("SKU-2", 1, 25m)]);
Console.WriteLine($"{count} lines, {subtotal}");
```

That prints `2 lines, 44.98`. Use a tuple inside a class or between a few private methods. For a public API, a record says more: its name, and its properties' documentation.

## The `field` keyword (C# 14)

Inside a property's accessor, `field` is the property's own backing field, which the compiler creates. A property can then do some work in one accessor without a hand-written private field:

```csharp
public sealed class Product
{
    public required string Name
    {
        get;
        init => field = value.Trim();
    }
}
```

`new Product { Name = "  Mug " }.Name` is `"Mug"`, of length 3. The [Null safety page](/csharp/null-safety/#the-nullable-attributes) uses `field` to turn a null into `""`.

## Extension members (C# 14)

C# 3's extension methods are still here: a `static` method in a `static` class, with `this` on its first parameter.

```csharp
public static class LegacyExtensions
{
    public static bool IsBulk(this OrderLine line) => line.Quantity >= 10;
}
```

C# 14 adds an **extension block**, which can hold extension properties as well as methods, and static members that appear on the type itself:

```csharp
public static class OrderLineExtensions
{
    extension(OrderLine line)
    {
        public bool IsGiftCard => line.Sku.StartsWith("GIFT-", StringComparison.Ordinal);

        public OrderLine WithQuantity(int quantity) => line with { Quantity = quantity };
    }
}
```

```csharp
var gift = new OrderLine("GIFT-50", 1, 50m);
Console.WriteLine(gift.IsGiftCard);
Console.WriteLine(gift.WithQuantity(3));
```

That prints `True`, then `OrderLine { Sku = GIFT-50, Quantity = 3, UnitPrice = 50, Total = 150 }`. `IsGiftCard` reads as a property, which C# 3 couldn't do. An extension method is called the same way in either form: `first.IsBulk()` works as it always has.

**In this primer's samples, keep one extension block per static class.** Learn's own example puts two blocks in one class, but with the samples' analyzers, two blocks (for different types, even) fail with `CA1708: Names of 'Members' and '…' should differ by more than case`. A static member in a generic block, such as `extension<T>(List<T>) { public static List<T> Empty … }`, fails with `CA1000: Do not declare static members on generic types`. A block with only instance members, generic or not, is fine.

## Coming from C# 3

| In C# 3 you'd…                                                                                                    | Now                                                                            | See                                                  |
| ----------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ | ---------------------------------------------------- |
| Write a class with private fields, a constructor and getters, then `Equals`, `GetHashCode` and `ToString` by hand | `public sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice);` | [Records](#records-c-9)                              |
| Write a `struct` with its own `Equals`                                                                            | `public readonly record struct Money(decimal Amount, string Currency);`        | [Record structs](#record-structs-c-10)               |
| Give a read-only property a constructor parameter                                                                 | `{ get; init; }`, set in an object initializer                                 | [`init` and `required`](#init-c-9-and-required-c-11) |
| Make a parameter compulsory by putting it in the constructor                                                      | `required`                                                                     | [`init` and `required`](#init-c-9-and-required-c-11) |
| Copy every field by hand to change one                                                                            | `order with { Discount = 0.05m }`                                              | [`with`](#with-c-9)                                  |
| Write a `private readonly` field and a constructor that assigns it                                                | `public sealed class Pricing(CustomerDirectory directory, decimal goldRate)`   | [Primary constructors](#primary-constructors-c-12)   |
| Return two values with `out` parameters, or a small class                                                         | `(decimal Subtotal, int Count)`, and `var (subtotal, count) = …`               | [Tuples](#tuples-and-deconstruction-c-7)             |
| Write a private field for a property that does some work                                                          | `field`                                                                        | [The `field` keyword](#the-field-keyword-c-14)       |
| Write an extension method                                                                                         | Still works; an `extension` block adds properties and static members           | [Extension members](#extension-members-c-14)         |

## Pitfalls

- **A record that holds a list isn't deep-equal.** A record compares its properties with their own `Equals`, and a `List<T>` compares references. Measured: two `record Basket(string CustomerId, List<string> Skus)` built with the same values gave `a == b` → `False`. Fix: compare the lists yourself in the record's `Equals`, or hold a type with value equality.
- **`with` copies shallowly.** The copy and the original share any list or object that a property holds, so changing one changes both. Fix: hold immutable collections, such as `IReadOnlyList<T>` filled once, and replace them in the `with` (`order with { Lines = [.. order.Lines, line] }`).
- **Expecting a class's primary constructor parameter to be a property.** `pricing.goldRate` gives `CS1061`. Fix: on a class, add a property, as in `public decimal GoldRate { get; } = goldRate;`, or use a record if it's a value.
- **A primary constructor parameter that's captured and also copied into a field.** The class then holds two copies, which can drift apart. Measured: a class that sets `_rate = goldRate` and also reads `goldRate` in a member fails with `error CS9124: Parameter 'decimal goldRate' is captured into the state of the enclosing type and its value is also used to initialize a field, property, or event.` Fix: use one or the other.
- **Assigning to a captured parameter.** It isn't `readonly`, so a method can change it, and every member then sees the new value. Fix: don't. If it needs to change, make it a property.
- **Two extension blocks in one class, in these samples.** It fails with `CA1708`. Fix: one block per static class.

## Practise

[Exercise 14](/csharp/#exercise-14-modern-c), on the section's first page, practises this page in its second and third steps: an order line becomes a record with `required` and `init` members, and applying a discount returns a copy made with `with`.
