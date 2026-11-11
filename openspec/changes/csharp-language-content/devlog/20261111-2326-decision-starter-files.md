# Decision: the Starter has three files, two with `#nullable disable`

<!-- Section: 2 · Tasks: 2.1–2.4 · Base: c01d405 -->

D6 and D8 talk about "the Starter's file" opening with `#nullable disable`. With one file, deleting
that line also raises CS8618 on `OrderLine` and `Order`, whose `string` and `List` properties have no
value in a C# 3-style class. TODO 1 would then turn into half of TODOs 2 and 3.

So the Starter, and the Solution to match, has three files:
- **`Customers.cs`** (`LoyaltyTier`, `Customer`, `CustomerDirectory`) opens with `#nullable disable`.
  TODO 1 starts by deleting it.
- **`Orders.cs`** (`OrderLine`, `Order`) opens with `#nullable disable` too, and a comment says to
  delete it once TODOs 2 and 3 are done. With `required` members, it then builds clean.
- **`Pricing.cs`** has nullable on, and needs no `#nullable disable`: while `Find` is oblivious,
  `Customer customer = _directory.Find(...)` gives no warning. Once TODO 1 annotates `Find` as
  `Customer?`, the compiler points at Pricing's dereference by itself (CS8600, CS8602). That's the
  "the warnings move once the API tells the truth" lesson from the 1.3 probe, in the exercise.

This keeps D8's point, which is that nullable is turned off in source files rather than in the
project file. It's also the "a file at a time" migration the Null safety page teaches. It doesn't
change any spec scenario.

Rejected:
- one file, with TODO 1's text telling the engineer to do TODOs 2 and 3 first. That's wordy, and it
  muddles which test belongs to which TODO;
- giving the Starter's `OrderLine` a constructor so that nullable-on raises no CS8618. The tests
  build lines with object initializers, so they can run against the Solution's `required`/`init`
  record, and a constructor-only class couldn't be built that way.

## Next

- See the section-2-built note.
