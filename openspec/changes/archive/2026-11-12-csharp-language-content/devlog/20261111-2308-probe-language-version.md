# Probe: language version and the C# 13/14 features

<!-- Section: 1 · Tasks: 1.1 · Base: f5e6842 -->

Scratch tree with the repo's `global.json`, `.editorconfig` and `samples/Directory.Build.props`
(net10.0, `Nullable` and `ImplicitUsings` on, `TreatWarningsAsErrors`, `AnalysisLevel=latest-recommended`,
`EnforceCodeStyleInBuild`). Platform: Darwin 25.6.0 arm64. SDKs installed: 10.0.404 and
11.0.100-rc.1; `global.json` (10.0.400, `latestFeature`) selects **10.0.404**.

## Default language version

`#error version` in a library:

```
error CS8304: Compiler version: '5.9.0-1.26529.119 (2ab2854867733112c84ac12c4273d7970ac3afdf)'. Language version: 14.0. Compiler path: '…/sdk/10.0.404/Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll'.
```

So net10.0 on the pinned SDK is **C# 14.0** by default. No `LangVersion` is needed anywhere.

## The C# 13 and 14 features

Each of these compiles with `0 Warning(s)`, `0 Error(s)` and runs:

- `params IEnumerable<int> values` (13): `Sum(1, 2, 3)` → `6`, and `Sum([4, 5])` → `9`;
- `field` (14): `set => field = value.Trim().ToUpperInvariant();` with `= ""`, so `"  abc-1 "` → `[ABC-1]`;
- extension members (14): `extension(List<int> source) { public int Total => source.Sum(); }` →
  `5`, and a static `extension(string) { public static string Dashes(int count) … }` →
  `string.Dashes(3)` = `---`;
- null-conditional assignment (14): `missing?.Sku = "never";` on null does nothing; `present?.Sku = "y"`.

## Findings the pages must respect (measured, under the repo's analyzers)

1. **Null-conditional assignment taints flow state.** After `present?.Sku = "set";` on a non-null
   local, the next `present.Sku` gives
   `error CS8602: Dereference of a possibly null reference.` The compiler treats the `?.` as
   evidence that the local might be null. Worth a Pitfall on the Null safety page.
2. **Two `extension` blocks in one static class fail the build**, even with different receivers:
   `error CA1708: Names of 'Members' and 'Ext.E.extension(int), Ext.E.extension(string)' should differ by more than case`.
   This is an analyzer false positive over the compiler-generated grouping types. One block per
   class builds clean.
3. **A static member in a generic extension block fails:**
   `error CA1000: Do not declare static members on generic types`. A generic block with only
   instance members (`extension<T>(IEnumerable<T> items) { public bool IsEmpty … }`) is clean, and so is
   a non-generic block with instance and static members mixed.

So the Types page's extension-member examples use one block per static class, and no static members
in generic blocks. Its Pitfalls may mention 2 and 3, since a reader copying the Learn sample (several
blocks in one class) into these samples would hit CA1708.

## Next

- Task 1.2: probe the reflection for exercise 14's tests (`RequiredMemberAttribute`, `IsExternalInit`
  modreq, `NullabilityInfoContext`, record and class equality). Stop and ask if any check doesn't
  work as D7 needs.
- The scratch tree is in the session scratchpad (`…/scratchpad/probe/`), which won't survive a
  /clear: rebuild it from this note if needed.
