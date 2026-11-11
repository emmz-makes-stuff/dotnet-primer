# Probe: a C# 3 file under nullable, and the version table

<!-- Section: 1 · Tasks: 1.3 · Base: f5e6842 -->

## The C# 3-style file (D8)

Scratch library under the repo's rules, with one file shaped like exercise 14's Starter but not copied
from it. It contains `Customer` (`Id`, `Name`, `Email` as `{ get; set; }`, `IsGold`), a
`CustomerDirectory` whose `Find` uses `TryGetValue` and `return null`, and an `Invoice` with a
constructor-injected directory, `CustomerId` and `Note` properties, `Greeting()` (`Customer customer =
directory.Find(CustomerId); return "Dear " + customer.Name;`) and `EmailLength()`
(`return directory.Find(CustomerId).Email.Length;`).

- **With `#nullable disable` on line 1:** `0 Warning(s)`, `0 Error(s)`. D8 holds.
- **Stage 1, the line deleted** (`7 Error(s)`, all of them nullable warnings raised to errors):

  ```
  Shop.cs(5,19): error CS8618: Non-nullable property 'Id' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
  Shop.cs(6,19): error CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
  Shop.cs(7,19): error CS8618: Non-nullable property 'Email' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
  Shop.cs(23,43): error CS8600: Converting null literal or possible null value to non-nullable type.
  Shop.cs(27,16): error CS8603: Possible null reference return.
  Shop.cs(35,12): error CS8618: Non-nullable property 'CustomerId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
  Shop.cs(35,12): error CS8618: Non-nullable property 'Note' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
  ```

  (23,43) is `out customer` in `TryGetValue`; (27,16) is `return null`; (35,12) is the `Invoice`
  constructor. **There's no warning at the dereferences yet,** because `Find` still claims to return
  a non-null `Customer`. This is the key teaching point: the warnings move once the API tells the truth.
- **Stage 2, `Find` annotated as `Customer?` (and its local), `Email` as `string?`:**

  ```
  Shop.cs(5,19): error CS8618: Non-nullable property 'Id' must contain a non-null value when exiting constructor. …
  Shop.cs(6,19): error CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. …
  Shop.cs(35,12): error CS8618: Non-nullable property 'CustomerId' must contain a non-null value when exiting constructor. …
  Shop.cs(35,12): error CS8618: Non-nullable property 'Note' must contain a non-null value when exiting constructor. …
  Shop.cs(45,29): error CS8600: Converting null literal or possible null value to non-nullable type.
  Shop.cs(46,26): error CS8602: Dereference of a possibly null reference.
  Shop.cs(51,16): error CS8602: Dereference of a possibly null reference.
  ```

  (45,29) is `Customer customer = directory.Find(...)`, and (46,26) is `customer.Name`. **On line 51,
  `directory.Find(CustomerId).Email.Length` gives one CS8602, not two:** after the first warning
  in a chain, the compiler treats the value as not-null.
- **Stage 3, fixed:** `required` on `Id`, `Name` and `CustomerId`, `string? Note`, `Customer?` local
  with `customer is null ? … : …`, and `directory.Find(CustomerId)?.Email?.Length ?? 0`. Result:
  `0 Warning(s)`, `0 Error(s)`.

So exercise 14's description can say: deleting `#nullable disable` gives CS8618, CS8600 and CS8603
first, and the CS8602s appear only after the lookup is annotated `Customer?`. §2.4 re-measures this on
the real Starter.

## Version table (D2)

Sources, all fetched 2026-11-11:
- **H** is the [language version history](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-version-history).
  (`whats-new/csharp-9`, `-10` and `-11` now redirect there.)
- **W12, W13 and W14** are [What's new in C# 12](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-12),
  [13](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-13) and
  [14](https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14).
- **LV** means measured on the pinned compiler with `-p:LangVersion=<n>`: the version below fails
  with "Feature '…' is not available in C# n", and the tagged version builds. I used it where Learn
  doesn't name a version.

| Feature | C# | Source |
| --- | --- | --- |
| `??` | already in C# 3 (builds with LangVersion 2): no tag | LV; the [`??` reference](https://learn.microsoft.com/dotnet/csharp/language-reference/operators/null-coalescing-operator) gives no version |
| Lambdas, LINQ, `var`, extension methods, auto-properties, object and collection initializers, anonymous types | 3 | H ("C# version 3.0") |
| Expression-bodied members (methods, read-only properties) | 6 | H ("C# version 6.0") |
| `?.` (null propagator) | 6 | H |
| String interpolation | 6 | H |
| `nameof` | 6 | H |
| Auto-property initializers | 6 | H |
| Tuples and deconstruction | 7.0 | H |
| Pattern matching: `is` type pattern, `is null`, `case … when` | 7.0 | H, plus LV for `is null` (CS8059 at 6) and the `when` case guard (CS0151 at 6) |
| Local functions | 7.0 | H |
| Expanded expression-bodied members (constructors, accessors) | 7.0 | H |
| Out variables, discards, throw expressions | 7.0 | H |
| Switch expressions | 8.0 | H |
| Property, tuple and positional patterns | 8.0 | H |
| Using declarations | 8.0 | H |
| Nullable reference types | 8.0 | H |
| `??=` | 8.0 | H |
| Nullable attributes (`[NotNullWhen]`, `[MaybeNullWhen]`, `[MemberNotNull]`, `[AllowNull]`) | arrived with nullable reference types (8.0); `[MemberNotNull]` is .NET 5 | [nullable-analysis attributes](https://learn.microsoft.com/dotnet/csharp/language-reference/attributes/nullable-analysis). **Don't tag them with a version on the page:** say they're library attributes that the compiler reads |
| Records | 9 | H |
| `init` | 9 | H |
| Top-level statements | 9 | H |
| Relational and logical patterns (`is not null`) | 9 | H (it shows `if (e is not null)`) |
| Target-typed `new()` | 9 | H ("Fit and finish") |
| Record structs | 10 | H |
| `global using` | 10 | H |
| File-scoped namespaces | 10 | H |
| Extended property patterns (`{ Customer.Tier: … }`) | 10 | H |
| `with` on structs and anonymous types | 10 | LV (`CS8773: Feature 'with on structs' is not available in C# 9.0`); the [`with` reference](https://learn.microsoft.com/dotnet/csharp/language-reference/operators/with-expression) lists the operand types but no version |
| Implicit usings | an SDK feature from .NET 6, built on `global using` (10) | [.NET project SDK overview](https://learn.microsoft.com/dotnet/core/project-sdk/overview), "Starting in .NET 6". Tag it "(.NET 6, C# 10)" |
| Raw string literals, and interpolated raw strings | 11 | H |
| List patterns (slice `..` included) | 11 | H |
| Required members | 11 | H |
| Newlines in interpolation expressions | 11 | H |
| Primary constructors (class and struct) | 12 | H, W12 |
| Collection expressions (`[..]`, spread) | 12 | H, W12 |
| Alias any type | 12 | H |
| `params` collections | 13 | W13 |
| Extension members (blocks, properties, static members) | 14 | H, W14 |
| `field` | 14 | H, W14 |
| Null-conditional assignment (`a?.B = c`, and compound `?.X += 1`, but not `++`/`--`) | 14 | W14 |

Notes for the pages:
- Primary constructors on records came with records (9, positional records). Primary constructors on
  classes and structs are 12. The Types page should say so.
- W14's own extension-member sample puts two generic `extension<TSource>` blocks in one class, and
  a static member in a generic block. **Both fail under this repo's analyzers** (CA1708 and CA1000, as
  the 1.1 note records), so a reader copying Learn would hit them. That's a real Pitfall.
- W14: compound assignment works with `?.`, but `++` and `--` don't. Worth a line.

## Next

- §1 is complete: write the section-done note, run `make gates`, and commit §1.
- Then §2, task 2.1: scaffold `samples/exercises/14-modern-csharp/`.
