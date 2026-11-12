---
title: "Null safety"
description: "Nullable reference types from C# 8 on: string against string?, the warnings, ?. and ??=, is null, the null-forgiving operator, the nullable attributes, null-conditional assignment in C# 14, and turning nullable on in old code a file at a time."
prev:
  link: /csharp/
  label: Modern C# at a glance
next:
  link: /csharp/types/
  label: Types
learn:
  - title: "Nullable reference types"
    url: https://learn.microsoft.com/dotnet/csharp/nullable-references
  - title: "Attributes for nullable static analysis"
    url: https://learn.microsoft.com/dotnet/csharp/language-reference/attributes/nullable-analysis
  - title: "Update a codebase with nullable reference types"
    url: https://learn.microsoft.com/dotnet/csharp/nullable-migration-strategies
---

By the end of this page you'll be able to read and write code with nullable reference types on, as every new .NET project has them: you'll know what `string?` promises and what it doesn't, which operator to reach for, and when the null-forgiving `!` is honest. You'll also be able to turn nullable on in an old codebase, one file at a time, and fix the warnings that appear.

:::tip[Cheatsheet]
The [C# cheatsheet](/cheatsheets/csharp/#null-safety) has this page's C# 3 → modern forms in one table.
:::

Every example was compiled and run with SDK 10.0.404 (C# 14) and the samples' settings, with nullable on and warnings as errors, on 12 November 2026. That's why the messages below say `error`: on its own, each is a warning.

## What nullable reference types change (C# 8)

In C# 3, any reference could be null, and nothing in the code said which ones might be. With `<Nullable>enable</Nullable>` in the project file, which the `dotnet new` templates put in new projects (measured on `classlib`), the compiler reads the types differently:

- **`string` means "never null".** Assigning null to it, or not setting it, is a warning.
- **`string?` means "might be null".** Using it without checking is a warning.

The types themselves don't change: `string?` and `string` are the same `System.String` at run time. **It's a compile-time check only.** Nothing stops a null at run time, so a null can still arrive from code with nullable off, or from JSON that's missing a field. The [Pitfalls](#pitfalls) show both.

## `string` and `string?`, and the warnings

Each of these is from a real build:

```csharp
string name = null;
Console.WriteLine(name);
```

```text
error CS8600: Converting null literal or possible null value to non-nullable type.
```

Passing null where the parameter isn't nullable:

```csharp
directory.Add(null);
```

```text
error CS8625: Cannot convert null literal to non-nullable reference type.
```

Using something that might be null:

```csharp
Customer? found = directory.Find("C-42");
Console.WriteLine(found.Name);
```

```text
error CS8602: Dereference of a possibly null reference.
```

And a non-nullable property that nothing sets gives `CS8618`, which you'll meet in [turning it on in old code](#turning-it-on-in-old-code-a-file-at-a-time). The directory these examples use says what can be null in its signatures:

```csharp
public sealed class Customer
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? Email { get; set; }

    public int Visits { get; set; }
}

public sealed class CustomerDirectory
{
    private readonly Dictionary<string, Customer> _customers = [];

    public void Add(Customer customer) => _customers[customer.Id] = customer;

    public Customer? Find(string id) => _customers.GetValueOrDefault(id);
```

A customer always has an `Id` and a `Name`, so they're `required` (C# 11, on the [Types](/csharp/types/#init-c-9-and-required-c-11) page). An email is optional, so it's `string?`. `Find` returns `Customer?`, because it returns null for an id it doesn't know.

## The null operators: `?.` (C# 6) and `??=` (C# 8)

`?.` reads a member only when the value isn't null, and gives null otherwise. `??`, which C# 2 already had, gives a fallback:

```csharp
Customer? found = directory.Find("C-42");
Console.WriteLine(found?.Email?.Length);
Console.WriteLine(directory.Find("nobody")?.Name ?? "no such customer");
```

That prints `15`, then `no such customer`. `??=` assigns only when the variable is null:

```csharp
string? greeting = null;
greeting ??= "Hello";
Console.WriteLine(greeting);
```

## `is null` and `is not null` (C# 7 and 9)

`x is null` (C# 7) and `x is not null` (C# 9) always mean what they say. `x == null` calls the type's `==` operator, which a type can overload, and get wrong. Here's a SKU type with a careless `==`:

```csharp title="Sku.cs"
public sealed class Sku : IEquatable<Sku>
{
    public required string Code { get; init; }

    // A careless == that treats every SKU as equal to null.
    public static bool operator ==(Sku? left, Sku? right) => left is null || right is null || left.Equals(right);
```

```csharp
var sku = new Sku { Code = "SKU-1" };
Console.WriteLine(sku == null);
Console.WriteLine(sku is null);
```

That prints `True`, then `False`. Use `is null` and `is not null` for null checks.

## The null-forgiving `!` (C# 8)

`!` after an expression tells the compiler "this isn't null", and silences the warning. It changes nothing at run time, so if you're wrong, you get the `NullReferenceException` anyway:

```csharp
Console.WriteLine(directory.Find("nobody")!.Name);
```

That builds with no warning, and fails at run time with `System.NullReferenceException: Object reference not set to an instance of an object.`

**When `!` is justified:** when you know something the compiler can't see, and you could say why in a comment. For example, the customer was added two lines up, a test has just checked it, or a framework sets the property before your code runs. **When it hides a bug:** when you use it to make the warning go away. The warning was the compiler telling you that this can be null, as `Find("nobody")` above can. Check for null instead, or give the type an honest signature.

## The nullable attributes

Some APIs can't say what they mean with `?` alone. The attributes in `System.Diagnostics.CodeAnalysis` tell the compiler more. They're library attributes that the compiler reads, and the most useful are:

- **`[NotNullWhen(true)]`**: an `out` parameter isn't null when the method returns `true`. It's the shape of a `Try…` method whose `out` parameter is nullable:

  ```csharp
  public bool TryFind(string id, [NotNullWhen(true)] out Customer? customer) =>
      _customers.TryGetValue(id, out customer);
  ```

  ```csharp
  if (directory.TryFind("C-42", out var customer))
  {
      Console.WriteLine(customer.Name);
  }
  ```

  Without the attribute, `customer.Name` gives `CS8602`, because `customer` is a `Customer?`.

- **`[MaybeNullWhen(false)]`** is the same promise from the other side, for an `out` parameter of a non-nullable or generic type: it may be null when the method returns `false`. `Dictionary<TKey, TValue>.TryGetValue` uses it, because its `TValue` might be a non-nullable type.
- **`[MemberNotNull]`**: once this method has run, the named field isn't null.

  ```csharp
  public sealed class Report
  {
      private string? _title;

      public string Title => _title ?? "(untitled)";

      [MemberNotNull(nameof(_title))]
      public void Start(string title) => _title = title;

      public int TitleLength()
      {
          Start("Orders");
          return _title.Length;
      }
  }
  ```

  Without the attribute, `_title.Length` gives `CS8602`.

- **`[AllowNull]`**: a setter accepts null, though the getter never returns it. With the `field` keyword (C# 14), the setter can swap null for a default:

  ```csharp
  public sealed class Note
  {
      [AllowNull]
      public string Text
      {
          get;
          set => field = value ?? "";
      } = "";
  }
  ```

  `new Note { Text = null }` builds, and its `Text` is `""`.

## Null-conditional assignment (C# 14)

`?.` now works on the left of an assignment, too. The assignment happens only when the value isn't null, and the right-hand side isn't evaluated otherwise:

```csharp
Customer? maybe = directory.Find("C-42");
maybe?.Email = "new@example.com";
maybe?.Visits += 1;
```

Compound assignments such as `+=` work, but `++` and `--` don't: `maybe?.Visits++;` fails with `error CS1059: The operand of an increment or decrement operator must be a variable, property or indexer`.

**The compiler takes `?.` as a sign that the value might be null.** After `maybe?.Email = …`, using `maybe.Name` gives `CS8602`, even on a local that you've just set to a new object. Write `?.` only where null is possible, and check with `is not null` when you then need the value.

## Checking arguments in a public API

A nullable annotation is a promise to callers that have nullable on. A public method that other code calls, including code with nullable off, still checks its arguments. `ArgumentNullException.ThrowIfNull` (.NET 6) does it in one line, and names the parameter for you:

```csharp
public sealed record Order
{
    public Order(string customerId, IReadOnlyList<string> skus)
    {
        ArgumentNullException.ThrowIfNull(customerId);
        ArgumentNullException.ThrowIfNull(skus);
        CustomerId = customerId;
        Skus = skus;
    }
```

`new Order("C-42", null!)` throws, with `Value cannot be null. (Parameter 'skus')`. The analyzers suggest it too: the C# 3 `if (x == null) throw new ArgumentNullException("x");` gives `CA1510: Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing a new exception instance`.

## Turning it on in old code, a file at a time

An old codebase with nullable switched on all at once gives hundreds of warnings. Instead, turn it on in the project file, and add `#nullable disable` at the top of every file. Then remove that line from one file at a time, and fix what appears. (Or the other way round: leave it off in the project, and add `#nullable enable` file by file.)

Here's a C# 3-style file, with the line at the top. It builds clean:

```csharp title="Invoice.cs"
#nullable disable
namespace Shop;

public class Customer
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class CustomerDirectory
{
    private readonly Dictionary<string, Customer> _customers = new Dictionary<string, Customer>();

    public Customer Find(string id)
    {
        Customer customer;
        if (_customers.TryGetValue(id, out customer))
        {
            return customer;
        }

        return null;
    }
}

public class Invoice
{
    private readonly CustomerDirectory _directory;

    public Invoice(CustomerDirectory directory)
    {
        _directory = directory;
    }

    public string CustomerId { get; set; }

    public string Greeting()
    {
        Customer customer = _directory.Find(CustomerId);
        return "Dear " + customer.Name;
    }
}
```

**Step 1: delete `#nullable disable`.** The build gives six warnings:

```text
Invoice.cs(5,19): error CS8618: Non-nullable property 'Id' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Invoice.cs(6,19): error CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Invoice.cs(7,19): error CS8618: Non-nullable property 'Email' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Invoice.cs(17,44): error CS8600: Converting null literal or possible null value to non-nullable type.
Invoice.cs(22,16): error CS8603: Possible null reference return.
Invoice.cs(30,12): error CS8618: Non-nullable property 'CustomerId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
```

Notice what's missing: there's no warning where `Greeting` uses `customer.Name`, though `Find` can return null. That's because `Find` still says it returns a `Customer`. The `CS8600` and `CS8603` warnings are inside `Find`, where `return null` contradicts its signature.

**Step 2: make the signatures tell the truth.** Make `Find` return `Customer?`, with its local `Customer? customer`, and make `Email` a `string?`, because a customer needn't have one. Now the warnings move to the caller:

```text
Invoice.cs(5,19): error CS8618: Non-nullable property 'Id' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Invoice.cs(6,19): error CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Invoice.cs(30,12): error CS8618: Non-nullable property 'CustomerId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
Invoice.cs(39,29): error CS8600: Converting null literal or possible null value to non-nullable type.
Invoice.cs(40,26): error CS8602: Dereference of a possibly null reference.
```

Lines 39 and 40 are `Greeting`, which is the bug that was there all along: an unknown customer would have thrown `NullReferenceException`.

**Step 3: fix each one.**

- `CS8618` on `Id`, `Name` and `CustomerId`: these always have a value, so mark them `required`, and the compiler makes every caller set them. (Or give them a default, or set them in a constructor.)
- `CS8600` and `CS8602` in `Greeting`: handle the customer that isn't there.

```csharp title="Invoice.cs"
public string Greeting()
{
    Customer? customer = _directory.Find(CustomerId);
    return customer is null ? "Dear customer" : "Dear " + customer.Name;
}
```

The file then builds clean, and the next file is ready to go. Exercise 14's first step is exactly this, on its `Customers.cs`.

## Coming from C# 3

| In C# 3 you'd…                                                      | Now                                                                    | See                                                                |
| ------------------------------------------------------------------- | ---------------------------------------------------------------------- | ------------------------------------------------------------------ |
| Assume any reference might be null, and check where you remember to | `string` is never null and `string?` might be, and the compiler checks | [What nullable changes](#what-nullable-reference-types-change-c-8) |
| `if (c != null) { name = c.Name; }`                                 | `name = c?.Name;`                                                      | [The null operators](#the-null-operators--c-6-and--c-8)            |
| `if (x == null) { x = "Hello"; }`                                   | `x ??= "Hello";`                                                       | [The null operators](#the-null-operators--c-6-and--c-8)            |
| `x == null`                                                         | `x is null`, and `x is not null`                                       | [`is null`](#is-null-and-is-not-null-c-7-and-9)                    |
| `if (c != null) { c.Email = e; }`                                   | `c?.Email = e;`                                                        | [Null-conditional assignment](#null-conditional-assignment-c-14)   |
| `if (x == null) throw new ArgumentNullException("x");`              | `ArgumentNullException.ThrowIfNull(x);`                                | [Checking arguments](#checking-arguments-in-a-public-api)          |
| Turn a whole project on at once                                     | `#nullable disable` in each file, removed one file at a time           | [A file at a time](#turning-it-on-in-old-code-a-file-at-a-time)    |

## Pitfalls

- **`!` that hides a bug.** `directory.Find("nobody")!.Name` builds with no warning, and throws `NullReferenceException`. Fix: use `!` only for what you can explain in a comment. Otherwise check for null, or fix the signature.
- **Believing `string` can't be null at run time.** It's a compile-time check. A caller with nullable off can pass null with no warning, and a JSON document that's missing a field gives null too: measured, `JsonSerializer.Deserialize` on `{"CustomerId": "C-42"}` gave a record whose non-nullable `Lines` was null. Fix: check arguments at the edges of your code (`ArgumentNullException.ThrowIfNull`), and validate what you deserialize.
- **An array of a non-nullable type.** `var skus = new string[3];` is three nulls, but `skus[0].Length` builds with no warning, and throws `NullReferenceException`. Fix: fill the array before you read it, or use a collection expression with its values.
- **A null check after `?.` assignment.** After `maybe?.Email = …`, the compiler treats `maybe` as maybe-null, so the next `maybe.Name` gives `CS8602`. Fix: check `maybe is not null` once, and use it inside the check.
- **`== null` on a type that overloads `==`.** A careless operator can make a real object equal null. Fix: `is null` and `is not null`, which can't be overloaded.
- **Turning nullable on for a whole old project at once.** Hundreds of warnings at once are hard to work through, and tempt you to scatter `!` everywhere. Fix: a file at a time, with `#nullable disable`.

## Practise

[Exercise 14](/csharp/#exercise-14-modern-c), on the section's first page, starts with this page: its first step deletes a `#nullable disable`, annotates a customer directory, and makes pricing an unknown customer's order safe.
