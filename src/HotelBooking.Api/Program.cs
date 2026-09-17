using HotelBooking.Infrastructure;
using HotelBooking.Infrastructure.Identity;
using HotelBooking.Application;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

// Initialize the default Identity roles at application startup.
await using (var scope = app.Services.CreateAsyncScope())
{
    var identityInitializer =
        scope.ServiceProvider.GetRequiredService<IdentityInitializer>();

    await identityInitializer.InitializeAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

