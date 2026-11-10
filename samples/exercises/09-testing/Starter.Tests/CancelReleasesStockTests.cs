namespace TestingOrders.Tests;

// TODO 2: test that cancelling an order tells the warehouse to put its stock back.
//   OrderService.CancelAsync calls IWarehouse.ReleaseAsync. Create an OrderService with a
//   test double for IWarehouse: a small hand-written fake that records its calls, or an
//   NSubstitute substitute (Substitute.For<IWarehouse>(), then .Received(...)). Then check what
//   the warehouse was told, and when it shouldn't have been told anything.
//   OrderService also needs an OrderStore (use a real one) and a TimeProvider (see TODO 3).
//   CancelAsync is async, so the test is too: make it `async Task`, and pass it
//   TestContext.Current.CancellationToken (the build insists, through xUnit's analyzer).
//   The Testing page: "Test doubles".
public class CancelReleasesStockTests
{
}
