// ReSharper disable ClassNeverInstantiated.Global

namespace CachedInventory.Tests;
using Xunit.Abstractions;
public class SingleRetrieval
{
  private readonly ITestOutputHelper output;

  public SingleRetrieval(ITestOutputHelper output) => this.output = output;

  [Fact(DisplayName = "retirar un producto")]
  public async Task Test() => await TestApiPerformance.Test(1, [3], false, 500, output);
}

public class FourRetrievalsSequentially
{
  private readonly ITestOutputHelper output;

  public FourRetrievalsSequentially(ITestOutputHelper output) => this.output = output;

  [Fact(DisplayName = "retirar cuatro productos secuencialmente")]
  public async Task Test() => await TestApiPerformance.Test(2, [1, 2, 3, 4], false, 1_000, output);
}

public class SevenRetrievalsSequentially
{
  private readonly ITestOutputHelper output;

  public SevenRetrievalsSequentially(ITestOutputHelper output) => this.output = output;

  [Fact(DisplayName = "retirar siete productos secuencialmente")]
  public async Task Test() => await TestApiPerformance.Test(5, [1, 2, 3, 4, 5, 6, 7], false, 500, output);
}

internal static class TestApiPerformance
{
  internal static async Task Test(int productId, int[] retrievals, bool isParallel, long expectedPerformance, ITestOutputHelper output)
  {
    await using var setup = await TestSetup.Initialize(output);
    output.WriteLine("Starting restock...");
    await setup.Restock(productId, retrievals.Sum());
    output.WriteLine("Starting verify stock from file...");
    await setup.VerifyStockFromFile(productId, retrievals.Sum());
    var tasks = new List<Task>();
    foreach (var retrieval in retrievals)
    {
      var task = setup.Retrieve(productId, retrieval);
      if (!isParallel)
      {
        output.WriteLine($"Starting task... product {productId}");
        await task;
      }

      tasks.Add(task);
    }
    output.WriteLine($"Starting all task...");
    await Task.WhenAll(tasks);
    output.WriteLine("Starting final stock...");
    var finalStock = await setup.GetStock(productId);
    output.WriteLine($"Duración promedio: {setup.AverageRequestDuration}ms, se esperaba un máximo de {expectedPerformance}ms.");
    Assert.True(finalStock == 0, $"El stock final no es 0, sino {finalStock}.");
    Assert.True(
      setup.AverageRequestDuration < expectedPerformance,
      $"Duración promedio: {setup.AverageRequestDuration}ms, se esperaba un máximo de {expectedPerformance}ms.");
    output.WriteLine("Starting verify stock from file...");
    await setup.VerifyStockFromFile(productId, 0);
  }
}
