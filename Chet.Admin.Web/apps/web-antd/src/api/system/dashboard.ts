import { requestClient } from '#/api/request';

export async function getDashboardStatsApi() {
  return requestClient.get('/dashboard/stats');
}

export async function getDashboardTrendApi(days: number = 7) {
  return requestClient.get('/dashboard/trend', { params: { days } });
}

export async function getRecentLogsApi(count: number = 10) {
  return requestClient.get('/dashboard/recent-logs', { params: { count } });
}
