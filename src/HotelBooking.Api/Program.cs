using HotelBooking.Infrastructure;
using HotelBooking.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

// Initialize the default Identity roles at application startup.
await using (var scope = app.Services.CreateAsyncScope())
{
    var identityInitializer =
        scope.ServiceProvider.GetRequiredService<IdentityInitializer>();

    await identityInitializer.InitializeRolesAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

