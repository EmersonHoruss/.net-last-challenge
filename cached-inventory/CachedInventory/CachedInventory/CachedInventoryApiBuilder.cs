namespace CachedInventory;

using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

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

    async Task<IResult> RetrieveStock([FromServices] IWarehouseStockSystemClient client,  [FromBody] RetrieveStockRequest req)
    {
      var stock = await client.GetStock(req.ProductId);
      Console.WriteLine("HELLO WORLD");
      if (stock < req.Amount)
      {
        return Results.BadRequest("Not enough stock.");
      }

      await client.UpdateStock(req.ProductId, stock - req.Amount);
      return Results.Ok();
    }

    async Task<IResult> Restock([FromServices] IWarehouseStockSystemClient client, [FromBody] RestockRequest req)
    {
      var stock = await client.GetStock(req.ProductId);
      await client.UpdateStock(req.ProductId, req.Amount + stock);
      return Results.Ok();
    }

    // Use the functions in the app.MapGet and app.MapPost calls
    app.MapGet("/stock/{productId:int}", GetStock)
         .WithName("GetStock")
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
