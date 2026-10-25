import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 10 },
    { duration: '1m', target: 50 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.05'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export function setup() {
  const res = http.post(`${BASE_URL}/api/v1/auth/login`, JSON.stringify({
    email: 'loadtest@example.com',
    password: 'LoadTest123',
  }), { headers: { 'Content-Type': 'application/json' } });

  check(res, { 'login ok': (r) => r.status === 200 });

  return { cookie: res.cookies.access_token[0].value };
}

export default function (data) {
  const params = {
    headers: {
      Cookie: `access_token=${data.cookie}`,
    },
  };

  const res = http.get(`${BASE_URL}/api/v1/tasks`, params);

  check(res, {
    'list tasks ok': (r) => r.status === 200,
    'list tasks fast': (r) => r.timings.duration < 1000,
  });

  sleep(1);
}