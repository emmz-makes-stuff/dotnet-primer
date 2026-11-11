# Probe: the reflection exercise 14's tests rely on

<!-- Section: 1 · Tasks: 1.2 · Base: f5e6842 -->

Same scratch tree as the 1.1 note. A C# 3-style file (`#nullable disable`, class with get/set
properties, a directory whose `Find` returns a plain `OldCustomer`) and a modern one (`sealed record`
with `required … { get; init; }`, `NewCustomer? Find`, `string? Email`). Both built with
`0 Warning(s)`, `0 Error(s)` once each directory held a real `Dictionary` (a stub `Find` trips
`CA1822: Member 'Find' does not access instance data and can be marked as static`, which the
exercise's directory avoids by holding its customers).

Output of the reflection run, verbatim:

```
OldLine.Sku: required=False init=False modreqs=[]
OldLine.Quantity: required=False init=False modreqs=[]
OldLine type has RequiredMemberAttribute: False
NewLine.Sku: required=True init=True modreqs=[System.Runtime.CompilerServices.IsExternalInit]
NewLine.Quantity: required=True init=True modreqs=[System.Runtime.CompilerServices.IsExternalInit]
NewLine type has RequiredMemberAttribute: True
OldDirectory.Find return: read=Unknown write=Unknown
OldCustomer.Name: read=Unknown write=Unknown
OldCustomer.Email: read=Unknown write=Unknown
NewDirectory.Find return: read=Nullable write=Nullable
NewCustomer.Name: read=NotNull write=NotNull
NewCustomer.Email: read=Nullable write=Nullable
NewDirectory.Find id param: read=NotNull write=NotNull
class: Equals=False ==False
record: Equals=True ==True
record ToString: NewLine { Sku = A, Quantity = 1 }
IsRecord-ish (EqualityContract): old=False new=True
```

Probes used:
- `property.GetCustomAttribute<RequiredMemberAttribute>()` (also set on the declaring type);
- `property.SetMethod!.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit))`;
- `new NullabilityInfoContext().Create(...)`, reading `ReadState` (properties, and a method's
  `ReturnParameter`);
- `Equals` and `==`.

**Verdict: every check works as D7 needs.** No stop.

Consequences for §2's tests:
- **`#nullable disable` gives `Unknown`, not `NotNull`.** The "name is never null" test expects
  `NotNull`, so it fails against the Starter too. That's right: an oblivious name isn't annotated.
- The `==` test compiles against both sides: on the Starter's class, it's a reference comparison
  (`False`); on the Solution's record, a value comparison (`True`).
- In `Solution.Tests`, the lookup returns `Customer?`, so a test that dereferences it without a
  check would give CS8602 under warnings as errors. The tests must check for null, or not
  dereference it.
- The record's `ToString` form (`NewLine { Sku = A, Quantity = 1 }`) is real output, for the Types page.

## Next

- Task 1.3: build a C# 3-style file with `#nullable disable` under the repo's rules, delete the line
  and record the exact warnings; then fetch Learn's "What's new in C#" pages (7.0–14) and the version
  history, and write the feature → version → URL table.
