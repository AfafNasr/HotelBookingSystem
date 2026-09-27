import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 10,
    duration: '30s',

    thresholds: {
        http_req_failed: ['rate<0.01'],
        http_req_duration: ['p(95)<500']
    }
};

const baseUrl = __ENV.BASE_URL;

export default function () {
    // Use a hotel created by the performance data seeder.
    const hotelId = 1;

    const url =
        `${baseUrl}/api/hotels/${hotelId}/rooms/available` +
        `?roomType=1` +
        `&checkInDate=2026-10-10` +
        `&checkOutDate=2026-10-12` +
        `&adults=2` +
        `&children=0`;

    const response = http.get(url, {
        tags: {
            endpoint: 'available-rooms'
        }
    });

    check(response, {
        'status is 200': (r) => r.status === 200
    });

    sleep(1);
}