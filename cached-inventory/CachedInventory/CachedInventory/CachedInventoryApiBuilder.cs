namespace CachedInventory;

using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading.Tasks;

public static class CachedInventoryApiBuilder
{
  public static WebApplication Build(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddScoped<IWarehouseStockSystemClient, WarehouseStockSystemClient>();
    builder.Services.AddSingleton<ICache, Cache>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
      app.UseSwagger();
      app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    async Task<int> GetStock([FromServices] IWarehouseStockSystemClient client, [FromServices] ICache cache, int productId)
    {
      if (!cache.Exists(productId))
      {
        var stock = await client.GetStock(productId);
        cache.AddOrUpdateValue(productId, stock);
        return stock;
      }

      return cache.GetValue(productId);
    }

    int GetStockCache([FromServices] ICache cache, int productId)
    {
      Console.WriteLine();
      return cache.GetValue(productId);
    }

    async Task<int> GetStockDB([FromServices] IWarehouseStockSystemClient client, int productId)
    {
      Console.WriteLine();
      return await client.GetStock(productId);
    }

    async Task<IResult> RetrieveStock([FromServices] IWarehouseStockSystemClient client, [FromServices] ICache cache, [FromBody] RetrieveStockRequest req)
    {
      var stock = cache.GetValue(req.ProductId);
      // var stock = await client.GetStock(req.ProductId);
      if (stock < req.Amount)
      {
        return Results.BadRequest("Not enough stock.");
      }

      // await client.UpdateStock(req.ProductId, stock - req.Amount);
      await Task.Run(() => client.UpdateStock(req.ProductId, stock - req.Amount));
      cache.AddOrUpdateValue(req.ProductId, stock - req.Amount);
      await Task.Delay(10);
      return Results.Ok();
    }

    async Task<IResult> Restock([FromServices] IWarehouseStockSystemClient client, [FromServices] ICache cache, [FromBody] RestockRequest req)
    {
      var stock = cache.GetValue(req.ProductId);
      // var stock = await client.GetStock(req.ProductId);

      // await client.UpdateStock(req.ProductId, req.Amount + stock);
      await Task.Run(() => client.UpdateStock(req.ProductId, req.Amount + stock));

      cache.AddOrUpdateValue(req.ProductId, req.Amount + stock);
      await Task.Delay(10);
      return Results.Ok();
    }

    // Use the functions in the app.MapGet and app.MapPost calls
    app.MapGet("/stock/{productId:int}", GetStock)
         .WithName("GetStock")
         .WithOpenApi();

    app.MapGet("/stockcache/{productId:int}", GetStockCache)
         .WithName("GetStockCache")
         .WithOpenApi();

    app.MapGet("/stockdb/{productId:int}", GetStockDB)
         .WithName("GetStockDB")
         .WithOpenApi();

    app.MapPost("/stock/retrieve", RetrieveStock)
         .WithName("RetrieveStock")
         .WithOpenApi();

    app.MapPost("/stock/restock", Restock)
         .WithName("Restock")
         .WithOpenApi();
    return app;
  }
}

public record RetrieveStockRequest(int ProductId, int Amount);

public record RestockRequest(int ProductId, int Amount);
