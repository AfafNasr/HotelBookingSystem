# Performance Testing

Performance testing is performed with **k6** against a seeded Development database containing approximately:

- 200 hotels
- 5,000 rooms
- 20,000 bookings

The goal is to measure realistic query behavior, identify bottlenecks, optimize only when justified, and compare results before and after changes.

---

## Hotel Search

### Problem

The original hotel search query calculated availability statistics separately for each hotel:

- Available room count
- Adult capacity
- Child capacity
- Minimum room price

EF Core generated repeated correlated subqueries over `Room`, `BookingRooms`, and `Bookings`, causing duplicated database work.

### Optimization

Available rooms are now grouped once by `HotelId`, and the required statistics are calculated together:

```text
Available Rooms
      ↓
GROUP BY HotelId
      ↓
RoomCount
AdultsCapacity
ChildrenCapacity
MinPrice
      ↓
JOIN Hotels
```

This removed the repeated availability calculations.

### Results

| Load | Before p95 | After p95 | Failures |
|---|---:|---:|---:|
| 10 VUs | 280.66 ms | 82.33 ms | 0% |
| 50 VUs | — | 92.52 ms | 0% |
| 100 VUs | — | 116.02 ms | 0% |

At 10 VUs, p95 latency improved by approximately **71%**.

### Database Review

The generated SQL and SQL Server Actual Execution Plan were inspected before and after the refactor.

No additional index was added because the main measured issue was repeated query work, and the optimized query already met the performance target.

### Thresholds

```text
p95 < 500 ms
failure rate < 1%
```

Both thresholds passed.

## Available Rooms

The available rooms endpoint was tested against the seeded performance dataset.

| Load | p95 | Average | Failures |
|---|---:|---:|---:|
| 10 VUs | 18.38 ms | 9.76 ms | 0% |

The query already performed well, so no optimization was introduced.

## Hotel Details

The hotel details endpoint was tested against the seeded performance dataset.

| Load | p95 | Average | Failures |
|---|---:|---:|---:|
| 10 VUs | 18.99 ms | 15.81 ms | 0% |

Although the query performs multiple database round-trips, measured performance was already strong, so no optimization was introduced.

## Featured Deals

### Problem

The original query evaluated room data multiple times per hotel:

- one `EXISTS` check
- one `MIN(PricePerNight)` for the original price
- another `MIN(PricePerNight)` for the discounted price

This caused repeated reads against the `Room` table.

### Optimization

Room prices are now grouped once by `HotelId`, and the minimum price is calculated a single time and reused.

### Results

| Load | Before p95 | After p95 | Failures |
|---|---:|---:|---:|
| 10 VUs | 271.01 ms | 25.88 ms | 0% |

The p95 latency improved by approximately **90%**.

## Hotel Bookings

The hotel bookings endpoint was tested against the seeded performance dataset.

| Load | p95 | Average | Failures |
|---|---:|---:|---:|
| 10 VUs | 20.63 ms | 11.49 ms | 0% |

The paginated query already performed well, so no optimization was introduced.

