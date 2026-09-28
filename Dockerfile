# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/HotelBooking.Domain/HotelBooking.Domain.csproj \
     src/HotelBooking.Domain/

COPY src/HotelBooking.Application/HotelBooking.Application.csproj \
     src/HotelBooking.Application/

COPY src/HotelBooking.Infrastructure/HotelBooking.Infrastructure.csproj \
     src/HotelBooking.Infrastructure/

COPY src/HotelBooking.Api/HotelBooking.Api.csproj \
     src/HotelBooking.Api/

RUN dotnet restore src/HotelBooking.Api/HotelBooking.Api.csproj

COPY . .

RUN dotnet publish \
    src/HotelBooking.Api/HotelBooking.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore


# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "HotelBooking.Api.dll"]