## MODIFIED Requirements

### Requirement: Full-content pages carry no placeholder
A learning-path page, a C# language page, the Coming from .NET 3.5 page or a cheatsheet delivered
with full content SHALL NOT show the "Coming soon" placeholder note. If one of them shows the
placeholder, the site test gate SHALL fail.

#### Scenario: Every finished page is checked
- **WHEN** the site test gate runs
- **THEN** it checks for the placeholder on the ASP.NET Core entry page, the Configuration,
  Dependency Injection, Middleware and Session and state pages and their four cheatsheets, the .NET
  10 and `dotnet` CLI page with the `dotnet` CLI cheatsheet, the Logging, Tracing and Metrics pages
  with their three cheatsheets, the EF Core page with its cheatsheet, the gRPC page with its
  cheatsheet, the Testing page with its cheatsheet, the Security & auth page with its cheatsheet,
  the Async & performance page with its cheatsheet, the Containers page with its cheatsheet, the
  Orchestration & AWS page with its cheatsheet, the four C# language pages with the C# cheatsheet,
  and the Coming from .NET 3.5 page with its cheatsheet

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

#### Scenario: Reader opens the finished Containers page
- **WHEN** a reader opens the Containers page or the Containers cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Orchestration page
- **WHEN** a reader opens the Orchestration & AWS page or the Orchestration & AWS cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens a finished C# page
- **WHEN** a reader opens any of the four C# language pages, or the C# cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Session and state page
- **WHEN** a reader opens the Session and state page or its cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Reader opens the finished Coming from .NET 3.5 page
- **WHEN** a reader opens the Coming from .NET 3.5 page or its cheatsheet
- **THEN** it has no "Coming soon" note

#### Scenario: Placeholder left on a finished page is caught
- **WHEN** a page listed as full content still contains the "Coming soon" placeholder note
- **THEN** the site test gate fails and names the page

### Requirement: gRPC page maps WCF habits
The gRPC page SHALL have a "Coming from WCF" section, after its concepts and before its Pitfalls. It
SHALL map these WCF habits to gRPC: service and operation contracts, data contracts, bindings and
XML configuration, `netTcpBinding`, generated proxies, `FaultException`, and duplex callback
contracts. It SHALL say that CoreWCF exists for moving WCF services as they are.

#### Scenario: Engineer coming from WCF looks for data contracts
- **WHEN** an engineer used to `[DataContract]` and `[DataMember]` reads the mapping
- **THEN** it says that messages are defined in the `.proto`, and that field numbers, not member
  names, are what goes on the wire

#### Scenario: Engineer coming from WCF looks for faults
- **WHEN** an engineer used to `FaultException<T>` reads the mapping
- **THEN** it says to fail with a status code and rich error details, and links to the page's
  errors section

#### Scenario: Engineer coming from WCF over net.tcp
- **WHEN** an engineer whose services use `netTcpBinding` reads the mapping
- **THEN** it says that gRPC runs over HTTP/2 instead, as a fast binary transport that works across
  platforms and through load balancers that support HTTP/2

## ADDED Requirements

### Requirement: Learning-path pages map .NET 3.5 habits
The ASP.NET Core entry page and the Configuration, Dependency Injection, Middleware, Logging,
Tracing and Metrics pages SHALL each have a "Coming from .NET 3.5" section, after the concepts and
before Pitfalls. It SHALL be a table of old thing and modern equivalent, each modern entry linking
to the part of the page that explains it, with rows for an MVC, Web API and WCF codebase.

#### Scenario: Engineer used to two controller stacks
- **WHEN** an engineer who knows `System.Web.Mvc.Controller` and Web API's `ApiController` reads the
  ASP.NET Core page's mapping
- **THEN** it says that ASP.NET Core has one controller stack, and minimal APIs, in place of both

#### Scenario: Engineer used to HTTP modules
- **WHEN** an engineer who knows `IHttpModule` and Web API's `DelegatingHandler` reads the Middleware
  page's mapping
- **THEN** it says that both become middleware, and links to the page's section on writing
  middleware

#### Scenario: Engineer used to performance counters
- **WHEN** an engineer who knows Windows performance counters reads the Metrics page's mapping
- **THEN** it says to use a `Meter` and its instruments, and links to the page's section on them

### Requirement: Hub rows link to the topic pages
Each row on the Coming from .NET 3.5 page and its cheatsheet SHALL link to the "Coming from" section,
or the section, of the topic page that explains the modern equivalent. Every learning-path page
with a "Coming from" section SHALL be linked from the hub at least once, and every link SHALL
resolve to a heading that exists.

#### Scenario: Hub links reach every mapping
- **WHEN** the site test gate runs
- **THEN** it checks that the hub links to the "Coming from" section of every learning-path page
  that has one, and fails naming any page it misses

#### Scenario: Hub link to a missing heading is caught
- **WHEN** a hub row links to a heading id that doesn't exist on its target page
- **THEN** the site test gate fails and names the link

### Requirement: Session and state page covers ISession
The Session and state page SHALL show how to turn on session, and how to read and write it in an
endpoint. It SHALL say that `ISession` holds only strings, integers and bytes, and show typed
access by serializing to JSON. It SHALL say that ASP.NET Core takes no per-user lock on session,
and show what two concurrent requests do to the same session, from a real run.

#### Scenario: Engineer stores an object in session
- **WHEN** an engineer used to `Session["basket"] = basket` follows the page
- **THEN** it shows the basket serialized to JSON in session, and read back as the same type

#### Scenario: Engineer relies on the session lock
- **WHEN** an engineer reads the section on concurrent requests
- **THEN** it shows a real run where two concurrent requests to one session lose one write, and
  says how to avoid it

### Requirement: Session and state page covers session across replicas
The Session and state page SHALL explain that session data lives in an `IDistributedCache`, and that
the in-memory one is per process. For more than one instance it SHALL show a distributed cache such
as Redis, and say that every instance must share one Data Protection key ring and application
name to read the session cookie, linking to the Security page's Data Protection section.

#### Scenario: Engineer scales out to two replicas
- **WHEN** an engineer runs two instances behind a load balancer and follows the page
- **THEN** it says that session needs a shared distributed cache and a shared Data Protection key
  ring, and shows the configuration for both

### Requirement: Session and state page covers moving state out of session
The Session and state page SHALL say what belongs in session and what doesn't: data the app must
keep, such as a placed order, goes in a database; who the user is goes in the authentication
cookie or token's claims; small display preferences may go in a cookie. It SHALL say that session
expires and can be lost, so nothing the user can't recreate belongs only there.

#### Scenario: Engineer keeps an order in session
- **WHEN** an engineer whose app keeps the last placed order in session reads the page
- **THEN** it says to save the order to a store and keep at most its id in session

### Requirement: Session and state page maps System.Web session habits
The Session and state page SHALL have a "Coming from .NET 3.5" section, after the concepts and before
Pitfalls. It SHALL map `Session["key"]`, InProc, StateServer and SQL Server session modes,
`HttpContext.Current.Session`, the per-session lock, `Session_Start` and `Session_End`, and
`TempData`.

#### Scenario: Engineer used to Session_End
- **WHEN** an engineer whose app runs clean-up in `Session_End` reads the mapping
- **THEN** it says that ASP.NET Core raises no event when a session ends, and what to do instead
