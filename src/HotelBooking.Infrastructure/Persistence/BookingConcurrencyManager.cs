using HotelBooking.Application.Bookings;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence;

public sealed class BookingConcurrencyManager
    : IBookingConcurrencyManager
{
    private readonly ApplicationDbContext _dbContext;

    public BookingConcurrencyManager(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteWithRoomLocksAsync<T>(
        IReadOnlyCollection<int> roomIds,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var orderedRoomIds = roomIds
            .Distinct()
            .OrderBy(roomId => roomId)
            .ToArray();

        if (orderedRoomIds.Length == 0)
        {
            throw new ArgumentException(
                "At least one room is required.",
                nameof(roomIds));
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        foreach (var roomId in orderedRoomIds)
        {
            var lockedRoom = await _dbContext.Room
                .FromSqlInterpolated(
                    $"""
            SELECT *
            FROM [Room] WITH (UPDLOCK, HOLDLOCK)
            WHERE [Id] = {roomId}
            """)
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);

            if (lockedRoom is null)
            {
                throw new InvalidOperationException(
                    $"Room {roomId} does not exist.");
            }
        }

        var result = await operation(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return result;
    }

    public async Task<T> ExecuteWithBookingLockAsync<T>(
    int bookingId,
    Func<CancellationToken, Task<T>> operation,
    CancellationToken cancellationToken)
    {
        if (bookingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bookingId));
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var lockedBooking = await _dbContext.Bookings
            .FromSqlInterpolated(
                $"""
            SELECT *
            FROM [Bookings] WITH (UPDLOCK, HOLDLOCK)
            WHERE [Id] = {bookingId}
            """)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (lockedBooking is null)
        {
            throw new InvalidOperationException(
                $"Booking {bookingId} does not exist.");
        }

        var result = await operation(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return result;
    }
}