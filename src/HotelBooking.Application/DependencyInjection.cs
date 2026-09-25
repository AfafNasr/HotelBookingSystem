using FluentValidation;
using HotelBooking.Application.Amenities.CreateAmenity;
using HotelBooking.Application.Amenities.DeleteAmenity;
using HotelBooking.Application.Amenities.UpdateAmenity;
using HotelBooking.Application.Authentication.Login;
using HotelBooking.Application.Authentication.Register;
using HotelBooking.Application.Bookings.CreateBooking;
using HotelBooking.Application.Bookings.Expiration;
using HotelBooking.Application.Bookings.GetBookingConfirmation;
using HotelBooking.Application.Bookings.Pricing;
using HotelBooking.Application.Cities.CreateCity;
using HotelBooking.Application.Cities.GetAdminCities;
using HotelBooking.Application.Cities.GetTrendingDestinations;
using HotelBooking.Application.Cities.UpdateCity;
using HotelBooking.Application.Countries.GetCountries;
using HotelBooking.Application.Deals.CreateDeal;
using HotelBooking.Application.Deals.DeleteDeal;
using HotelBooking.Application.Deals.GetFeaturedDeals;
using HotelBooking.Application.Deals.UpdateDeal;
using HotelBooking.Application.HotelAmenities.AddHotelAmenity;
using HotelBooking.Application.HotelAmenities.DeleteHotelAmenity;
using HotelBooking.Application.Hotels.CompleteHotelProfile;
using HotelBooking.Application.Hotels.CreateHotel;
using HotelBooking.Application.Hotels.DeleteHotel;
using HotelBooking.Application.Hotels.GetAdminHotelById;
using HotelBooking.Application.Hotels.GetAdminHotels;
using HotelBooking.Application.Hotels.GetHotelDetails;
using HotelBooking.Application.Hotels.GetRecentlyVisitedHotels;
using HotelBooking.Application.Hotels.SearchHotels;
using HotelBooking.Application.Hotels.UpdateHotel;
using HotelBooking.Application.Hotels.UploadHotelImage;
using HotelBooking.Application.Payments.HandleStripeWebhook;
using HotelBooking.Application.Payments.StartPayment;
using HotelBooking.Application.Reviews.CreateReview;
using HotelBooking.Application.Reviews.DeleteReview;
using HotelBooking.Application.Reviews.UpdateReview;
using HotelBooking.Application.Rooms.CreateRoom;
using HotelBooking.Application.Rooms.DeleteRoom;
using HotelBooking.Application.Rooms.GetAdminRooms;
using HotelBooking.Application.Rooms.GetAvailableRooms;
using HotelBooking.Application.Rooms.GetHotelRooms;
using HotelBooking.Application.Rooms.GetRoomById;
using HotelBooking.Application.Rooms.UpdateRoom;
using HotelBooking.Application.Rooms.UploadRoomImage;
using HotelBooking.Application.Users.PromoteToHotelOwner;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {

        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<RegisterCommand>();

        services.AddScoped<BookingPricingCalculator>();
        services.AddScoped<ExpirePendingBookingsService>();

        services.AddScoped<RegisterCommandHandler>();
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<CreateCityCommandHandler>();
        services.AddScoped<GetCountriesQueryHandler>();
        services.AddScoped<UpdateCityCommandHandler>();
        services.AddScoped<PromoteToHotelOwnerCommandHandler>();
        services.AddScoped<CreateHotelCommandHandler>();
        services.AddScoped<UpdateHotelCommandHandler>();
        services.AddScoped<CreateAmenityCommandHandler>();
        services.AddScoped<AddHotelAmenityCommandHandler>();
        services.AddScoped<CreateRoomCommandHandler>();
        services.AddScoped<CompleteHotelProfileCommandHandler>();
        services.AddScoped<UploadHotelImageCommandHandler>();
        services.AddScoped<UploadRoomImageCommandHandler>();
        services.AddScoped<CreateDealCommandHandler>();
        services.AddScoped<CreateBookingCommandHandler>();
        services.AddScoped<StartPaymentCommandHandler>();
        services.AddScoped<HandleStripeWebhookCommandHandler>();
        services.AddScoped<SearchHotelsQueryHandler>();
        services.AddScoped<GetFeaturedDealsQueryHandler>();
        services.AddScoped<CreateReviewCommandHandler>();
        services.AddScoped<GetHotelDetailsQueryHandler>();
        services.AddScoped<GetAvailableRoomsQueryHandler>();
        services.AddScoped<GetRecentlyVisitedHotelsQueryHandler>();
        services.AddScoped<GetTrendingDestinationsQueryHandler>();
        services.AddScoped<GetBookingConfirmationQueryHandler>();
        services.AddScoped<GetHotelRoomsQueryHandler>();
        services.AddScoped<UpdateRoomCommandHandler>();
        services.AddScoped<DeleteRoomCommandHandler>();
        services.AddScoped<GetAdminCitiesQueryHandler>();
        services.AddScoped<GetAdminHotelsQueryHandler>();
        services.AddScoped<GetAdminRoomsQueryHandler>();
        services.AddScoped<DeleteHotelCommandHandler>();
        services.AddScoped<GetAdminHotelByIdQueryHandler>();
        services.AddScoped<CreateReviewCommandHandler>();
        services.AddScoped<UpdateReviewCommandHandler>();
        services.AddScoped<DeleteReviewCommandHandler>();
        services.AddScoped<UpdateAmenityCommandHandler>();
        services.AddScoped<DeleteAmenityCommandHandler>();
        services.AddScoped<DeleteHotelAmenityCommandHandler>();
        services.AddScoped<UpdateDealCommandHandler>();
        services.AddScoped<DeleteDealCommandHandler>();


        return services;
    }
}