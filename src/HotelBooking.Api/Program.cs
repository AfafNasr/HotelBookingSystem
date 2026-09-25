using HotelBooking.Api.Authentication;
using HotelBooking.Api.Authorization;
using HotelBooking.Api.ErrorHandling;
using HotelBooking.Application;
using HotelBooking.Application.Bookings;
using HotelBooking.Application.Common.Security;
using HotelBooking.Infrastructure;
using HotelBooking.Infrastructure.Identity;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddApplication();



builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddPermissionAuthorization();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped< ICurrentUserService, CurrentUserService>();

QuestPDF.Settings.License = LicenseType.Community;

builder.Services
    .AddOptions<BookingOptions>()
    .Bind(builder.Configuration.GetSection(BookingOptions.SectionName))
    .Validate(
        options => options.PaymentHoldDurationMinutes > 0,
        "Payment hold duration must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddSingleton(
    serviceProvider =>
        serviceProvider
            .GetRequiredService<IOptions<BookingOptions>>()
            .Value);

var app = builder.Build();


// Initialize the default Identity roles at application startup.
if (!app.Environment.IsEnvironment("Testing"))
{
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var dbContext =
       scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();



        await dbContext.Database.MigrateAsync();
        var identityInitializer =
            scope.ServiceProvider.GetRequiredService<IdentityInitializer>();
       

        await identityInitializer.InitializeAsync();
        

    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

