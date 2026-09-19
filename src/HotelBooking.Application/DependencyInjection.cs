using FluentValidation;
using HotelBooking.Application.Authentication.Login;
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Cities.CreateCity;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterCommand>();

        services.AddScoped<RegisterCommandHandler>();
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<CreateCityCommandHandler>();

        return services;
    }
}