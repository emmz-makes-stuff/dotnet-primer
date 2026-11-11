## MODIFIED Requirements

### Requirement: Full-content pages carry no placeholder
A learning-path page or cheatsheet delivered with full content SHALL NOT show the "Coming soon"
placeholder note. If one of them shows the placeholder, the site test gate SHALL fail.

#### Scenario: Every finished page is checked
- **WHEN** the site test gate runs
- **THEN** it checks for the placeholder on the ASP.NET Core entry page, the Configuration,
  Dependency Injection and Middleware pages and their three cheatsheets, the .NET 10 and `dotnet`
  CLI page with the `dotnet` CLI cheatsheet, the Logging, Tracing and Metrics pages with their three
  cheatsheets, the EF Core page with its cheatsheet, the gRPC page with its cheatsheet, the Testing
  page with its cheatsheet, the Security & auth page with its cheatsheet, and the Async &
  performance page with its cheatsheet

#### Scenario: Reader opens a finished page
- **WHEN** a reader opens the Dependency Injection page
- **THEN** the page has no "Coming soon" note

#### Scenario: Reader opens the finished dotnet CLI cheatsheet
- **WHEN** a reader opens the `dotnet` CLI cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens a finished Observability page
- **WHEN** a reader opens the Logging, Tracing or Metrics page, or its cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished EF Core page
- **WHEN** a reader opens the EF Core page or the EF Core cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished gRPC page
- **WHEN** a reader opens the gRPC page or the gRPC cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Testing page
- **WHEN** a reader opens the Testing page or the Testing cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Security page
- **WHEN** a reader opens the Security & auth page or the Security cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Async page
- **WHEN** a reader opens the Async & performance page or the Async & performance cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Placeholder left on a finished page is caught
- **WHEN** a page listed as full content still contains the "Coming soon" placeholder note
- **THEN** the site test gate fails and names the page

## ADDED Requirements

### Requirement: Async page covers async end to end and cancellation
The Async & performance page SHALL cover what `await` does while it waits, keeping code async from
the endpoint down to the I/O, and cancellation: taking the request's token, passing it to every call
that waits, timeouts, and what a cancelled call throws. It SHALL show independent awaits run
concurrently, and when `ValueTask` is worth using, with the rule that it is awaited only once.

#### Scenario: Reader passes cancellation down
- **WHEN** a reader follows the page's cancellation example and the client disconnects mid-request
- **THEN** the work the endpoint started stops, and the page shows what is thrown

#### Scenario: Reader runs independent calls together
- **WHEN** a reader follows the page's example of two independent awaits
- **THEN** the page shows them taking the time of the slower one, not the sum, with the measured
  times

### Requirement: Async page covers blocking and thread-pool starvation
The Async & performance page SHALL explain why blocking on a task (`.Result`, `.Wait()`,
`GetAwaiter().GetResult()`) holds a thread, how many such requests starve the thread pool, and what
`async void` does to an exception. It SHALL show how to see starvation in a running app with
`dotnet-counters`, and how to find the blocking call with `dotnet-trace`.

#### Scenario: Reader sees starvation in the counters
- **WHEN** a reader runs the page's blocking example under load and watches it with
  `dotnet-counters`
- **THEN** the page shows the thread-pool counters it reports, with the real values, against the
  same load on the async version

#### Scenario: Reader learns what async void does
- **WHEN** a reader reads the page's `async void` section
- **THEN** it says what happens to an exception thrown inside one, and what to write instead

### Requirement: Async page covers spans, allocations and pooling
The Async & performance page SHALL cover `Span<T>` and `Memory<T>`: slicing without copying, and
where each may be stored. It SHALL cover finding allocations, renting buffers from a pool, building
strings without intermediate copies, source-generated JSON serialization, and frozen collections for
lookups built once and read often. Each claim of a saving SHALL come with a measured result.

#### Scenario: Reader parses without allocating
- **WHEN** a reader follows the page's span example
- **THEN** the page shows its allocations against the string-splitting version, as measured

#### Scenario: Reader knows where a span can't go
- **WHEN** a reader wants to keep a buffer across an `await` or in a field
- **THEN** the page says that a span can't, and that `Memory<T>` can

### Requirement: Async page covers caching
The Async & performance page SHALL cover caching data in the app with a cache that also stops many
concurrent requests for a missing entry from all computing it, and caching whole responses with
output caching. It SHALL say how entries expire and how to remove them.

#### Scenario: Reader caches an expensive lookup
- **WHEN** many requests ask for the same uncached entry at once, in the page's example
- **THEN** the page shows that the value is computed once

#### Scenario: Reader caches a response
- **WHEN** a reader adds the page's output caching to an endpoint
- **THEN** a repeated request within the expiry gets the cached response, and the page shows how to
  tell

### Requirement: Async page covers benchmarking
The Async & performance page SHALL cover measuring with BenchmarkDotNet: writing a benchmark,
including allocations, running it in Release, and reading its results. It SHALL say why a
`Stopwatch` loop misleads, and link to the samples' benchmark project.

#### Scenario: Reader reads a benchmark result
- **WHEN** a reader reads the page's benchmark results
- **THEN** the table is real output, and the page explains its mean, error and allocated columns

### Requirement: Async page maps .NET 3.5 habits
The Async & performance page SHALL have a "Coming from .NET 3.5" section, after its concepts and
before its Pitfalls. It SHALL map these habits: the `BeginXxx`/`EndXxx` pattern, the event-based
async pattern, `ThreadPool.QueueUserWorkItem`, `Thread` for background work, `HttpRuntime.Cache`,
and `string` concatenation in loops.

#### Scenario: Engineer looks for Begin and End
- **WHEN** an engineer used to `BeginXxx`/`EndXxx` reads the mapping
- **THEN** it points to the `async` method and `await`, and to the page's async section

#### Scenario: Engineer looks for HttpRuntime.Cache
- **WHEN** an engineer used to `HttpRuntime.Cache` reads the mapping
- **THEN** it points to the page's caching section
