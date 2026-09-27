import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 100,
    duration: '30s',

    thresholds: {
        http_req_failed: ['rate<0.01'],
        http_req_duration: ['p(95)<500']
    }
};

const baseUrl = __ENV.BASE_URL;

export default function () {
    const destination =
        encodeURIComponent('Performance City 01');

    const url =
        `${baseUrl}/api/hotels/search` +
        `?destination=${destination}` +
        `&checkInDate=2026-10-10` +
        `&checkOutDate=2026-10-12` +
        `&adults=2` +
        `&children=0` +
        `&rooms=1` +
        `&page=1` +
        `&pageSize=20`;

    const response = http.get(url, {
        tags: {
            endpoint: 'hotel-search'
        }
    });

    check(response, {
        'status is 200': (r) => r.status === 200
    });

    sleep(1);
}