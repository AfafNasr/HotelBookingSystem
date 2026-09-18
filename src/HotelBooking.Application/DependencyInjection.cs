using FluentValidation;
using HotelBooking.Application.Authentication.Login;
using HotelBooking.Application.Authentication.Register;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<RegisterCommandHandler>();
        services.AddScoped<IValidator<RegisterCommand>, RegisterCommandValidator>();
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();
        return services;
    }
}