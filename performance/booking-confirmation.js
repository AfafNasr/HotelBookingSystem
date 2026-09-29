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
const token = __ENV.TOKEN;

export default function () {
    const bookingId = 1;

    const url =
        `${baseUrl}/api/bookings/${bookingId}/confirmation`;

    const response = http.get(url, {
        headers: {
            Authorization: `Bearer ${token}`
        },
        tags: {
            endpoint: 'booking-confirmation'
        }
    });

    check(response, {
        'status is 200': (r) => r.status === 200
    });

    sleep(1);
}