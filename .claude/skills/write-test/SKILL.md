---
name: write-test
description: Write xUnit tests for MOHProject — decide Unit vs Integration, follow naming/assertion conventions, wire fixtures. Trigger when the user asks "write tests for X", "add a test that…", "cover this with tests", "test the Y controller/repository", or mentions xUnit / FluentAssertions / Testcontainers in the context of adding tests.
---

# Writing tests for MOHProject

Anchor everything to the setup already in `MOHProject.Tests/`:
- `Unit/` — no I/O, no DB, must be <1s
- `Integration/` — Testcontainers.MsSql, real SQL Server, uses `SqlServerFixture` via `[Collection(nameof(SqlServerCollection))]`
- Global usings: `Xunit`, `FluentAssertions`. Add `Moq` per-file.

## 1. Decide unit vs integration

Ask yourself: does the code under test need a real DbContext, real SQL, filesystem, HTTP, or Docker?

- **No** → `Unit/`. Test the C# logic directly. Mock any injected dependency with Moq.
- **Yes, DB-shaped** → `Integration/` with `SqlServerFixture`.
- **Yes, HTTP-shaped** → `Integration/` with `WebApplicationFactory<Program>` (add `Microsoft.AspNetCore.Mvc.Testing` package on first use; ask before installing).

If you can rewrite the code so a unit test suffices (extract pure logic into a method that takes primitives), prefer that over an integration test — but do not refactor product code just to make testing easier without saying so.

## 2. Naming

- Class: `<TypeUnderTest>Tests` (e.g. `ProductValidationTests`, `OrderCalculatorTests`).
- Method: `Method_State_ExpectedResult` (e.g. `Add_ThenReload_RoundtripsPriceAsDecimal_18_2`, `Total_WithDiscount_SubtractsPercentage`).
- One assertion concept per test. Multiple `Should()` calls that verify the *same* concept are fine; verifying two unrelated things is not — split.

## 3. Assertions

FluentAssertions style:
```csharp
result.Should().Be(expected);
list.Should().HaveCount(3).And.Contain(x => x.Id == 42);
action.Should().ThrowExactly<ArgumentException>().WithMessage("*name*");
```

Always attach a `because` message when the assertion encodes an assumption about SQL Server or EF Core behavior (collation, `decimal` precision, `DateTime.Kind`, delete cascade, connection string quirk). Example:
```csharp
loaded.CreatedAt.Kind.Should().Be(DateTimeKind.Unspecified,
    "SQL Server datetime2 returns Unspecified; convert in the app layer if needed");
```
The `because` names the assumption so a future reader knows what a failure means.

## 4. Test data

- Unit: hand-craft the minimum viable object.
- Integration: unique per-test data — put a `Guid.NewGuid().ToString("N")` in string keys/names. The fixture does NOT reset between tests.
- Do NOT rely on row counts across tests (`.Count().Should().Be(1)` is fragile — filter first).
- Do NOT seed via migrations to make tests pass — seed inside the test using the DbContext.

## 5. Wiring an integration test

```csharp
[Collection(nameof(SqlServerCollection))]
public class WidgetRepositoryTests
{
    private readonly SqlServerFixture _fixture;

    public WidgetRepositoryTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Foo_Bar_Baz()
    {
        await using var db = _fixture.CreateContext();
        // arrange, act, assert
    }
}
```

Each `CreateContext()` call returns a fresh `AppDbContext` on the same underlying DB. Use `await using` so it's disposed.

## 6. Mocking a dependency (unit)

```csharp
var repo = new Mock<IProductRepository>();
repo.Setup(r => r.GetAsync(1)).ReturnsAsync(new Product { Id = 1, Name = "X", Price = 1m });

var sut = new ProductService(repo.Object);
var result = await sut.PriceWithTaxAsync(1);

result.Should().Be(1.10m);
repo.Verify(r => r.GetAsync(1), Times.Once);
```

Rules:
- Only mock the boundary you own (interfaces or virtual methods). Never mock `DbContext` or `DbSet<T>` — use an integration test instead.
- Do not use `MockBehavior.Loose` blindly; prefer `Strict` when the test cares that only the expected calls happen.

## 7. Verify

After writing tests:
1. `dotnet build MOHProject.Tests`
2. Run the tests you wrote — either via `/test unit` or `dotnet test --filter "FullyQualifiedName~<TestClass>"`.
3. Confirm they FAIL when the production code is broken (comment out the fix, re-run, uncomment). If a test still passes with the code broken, it's testing the wrong thing.

Skip step 3 only for pure-getter tests or trivially obvious assertions.

## 8. What NOT to do

- No `EnsureCreated()` in the fixture — the fixture already calls `MigrateAsync()`.
- No `.Result` / `.Wait()` — always `async`/`await`.
- No `Assert.True(condition)` — use `condition.Should().BeTrue(...)` with a message.
- No test that only exercises framework code (e.g., asserting that `List.Add` adds an item). Test our code, not the platform's.
- No `Thread.Sleep` — if you need to wait for something, that's a design smell in the code under test.
- No shared mutable state at the class level (static fields, `[assembly: ...]` singletons). Each test constructs what it needs.
