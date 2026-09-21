using FluentValidation;
using HotelBooking.Application.Amenities.CreateAmenity;
using HotelBooking.Application.Authentication.Login;
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Cities.CreateCity;
using HotelBooking.Application.Cities.UpdateCity;
using HotelBooking.Application.Countries.GetCountries;
using HotelBooking.Application.HotelAmenities.AddHotelAmenity;
using HotelBooking.Application.Hotels.CompleteHotelProfile;
using HotelBooking.Application.Hotels.CreateHotel;
using HotelBooking.Application.Hotels.UpdateHotel;
using HotelBooking.Application.Hotels.UploadHotelImage;
using HotelBooking.Application.Rooms.CreateRoom;
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
        services.AddScoped<CreateAmenityCommandHandler>();
        services.AddScoped<AddHotelAmenityCommandHandler>();
        services.AddScoped<CreateRoomCommandHandler>();
        services.AddScoped<CompleteHotelProfileCommandHandler>();
        services.AddScoped<UploadHotelImageCommandHandler>();


        return services;
    }
}