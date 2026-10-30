using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<TContext>(options => options
    .UseNpgsql(connectionString)
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking) // setting to no tracking to improve performance
);

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
