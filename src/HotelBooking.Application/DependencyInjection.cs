using FluentValidation;
using HotelBooking.Application.Authentication.Login;
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Cities.CreateCity;
using HotelBooking.Application.Cities.UpdateCity;
using HotelBooking.Application.Countries.GetCountries;
using HotelBooking.Application.Hotels.CreateHotel;
using HotelBooking.Application.Hotels.UpdateHotel;
using HotelBooking.Application.Users.PromoteToHotelOwner;
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
        services.AddScoped<GetCountriesQueryHandler>();
        services.AddScoped<UpdateCityCommandHandler>();
        services.AddScoped<PromoteToHotelOwnerHandler>();
        services.AddScoped<CreateHotelCommandHandler>();
        services.AddScoped<UpdateHotelCommandHandler>();

        return services;
    }
}