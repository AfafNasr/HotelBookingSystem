using System.Globalization;
using System.Net.Http.Json;
using HotelBooking.Application.Hotels.GetNearbyAttractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Hotels;

public sealed class GeoapifyNearbyAttractionsService
    : INearbyAttractionsService
{
    private readonly HttpClient _httpClient;
    private readonly GeoapifyOptions _options;
    private readonly ILogger<GeoapifyNearbyAttractionsService> _logger;

    public GeoapifyNearbyAttractionsService(
        HttpClient httpClient,
        IOptions<GeoapifyOptions> options,
        ILogger<GeoapifyNearbyAttractionsService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<NearbyAttraction>> GetAsync(
        decimal latitude,
        decimal longitude,
        int radiusMeters,
        int limit,
        CancellationToken cancellationToken)
    {
        var latitudeText = latitude.ToString(
            CultureInfo.InvariantCulture);

        var longitudeText = longitude.ToString(
            CultureInfo.InvariantCulture);

        var requestUri =
            "v2/places" +
            "?categories=tourism" +
            $"&filter=circle:{longitudeText},{latitudeText},{radiusMeters}" +
            $"&bias=proximity:{longitudeText},{latitudeText}" +
            $"&limit={limit}" +
            $"&apiKey={Uri.EscapeDataString(_options.ApiKey)}";

        try
        {
            using var response = await _httpClient.GetAsync(
                requestUri,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var geoapifyResponse =
                await response.Content
                    .ReadFromJsonAsync<GeoapifyPlacesResponse>(
                        cancellationToken: cancellationToken);

            if (geoapifyResponse?.Features is null)
            {
                return Array.Empty<NearbyAttraction>();
            }

            return geoapifyResponse.Features
                .Where(feature =>
                    feature.Properties is not null &&
                    !string.IsNullOrWhiteSpace(
                        feature.Properties.Name))
                .Select(feature =>
                    new NearbyAttraction(
                        feature.Properties!.Name!,
                        feature.Properties.Formatted,
                        feature.Properties.Latitude,
                        feature.Properties.Longitude,
                        feature.Properties.Distance))
                .ToArray();
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Geoapify nearby attractions request timed out.");

            throw;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "Geoapify nearby attractions request failed.");

            throw;
        }
    }

    private sealed class GeoapifyPlacesResponse
    {
        public IReadOnlyCollection<GeoapifyFeature>? Features
        {
            get;
            init;
        }
    }

    private sealed class GeoapifyFeature
    {
        public GeoapifyProperties? Properties
        {
            get;
            init;
        }
    }

    private sealed class GeoapifyProperties
    {
        public string? Name
        {
            get;
            init;
        }

        public string? Formatted
        {
            get;
            init;
        }

        public decimal Lat
        {
            get;
            init;
        }

        public decimal Lon
        {
            get;
            init;
        }

        public int? Distance
        {
            get;
            init;
        }

        public decimal Latitude => Lat;

        public decimal Longitude => Lon;
    }
}