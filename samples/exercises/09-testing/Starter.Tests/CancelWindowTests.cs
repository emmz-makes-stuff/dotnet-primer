namespace TestingOrders.Tests;

// TODO 3: test the cancel window with a FakeTimeProvider.
//   CancelPolicy.CanCancel says whether an order can still be cancelled: up to and including
//   CancelPolicy.Window (30 minutes) after it was placed. Don't wait 30 minutes, and don't read
//   the system clock: create a FakeTimeProvider (Microsoft.Extensions.Time.Testing), give the
//   order a PlacedAt from it, and move it forward with Advance(...).
//   The Testing page: "Test doubles", on fake time.
public class CancelWindowTests
{
}
