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

