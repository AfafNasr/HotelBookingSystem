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
    const url =
        `${baseUrl}/api/hotels/featured-deals`;

    const response = http.get(url, {
        tags: {
            endpoint: 'featured-deals'
        }
    });

    check(response, {
        'status is 200': (r) => r.status === 200
    });

    sleep(1);
}