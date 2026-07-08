import { requestClient } from '#/api/request';

/**
 * 获取仪表盘统计数据
 * @returns 仪表盘统计信息
 */
export async function getDashboardStatsApi() {
  return requestClient.get('/dashboard/stats');
}

/**
 * 获取仪表盘趋势数据
 * @param days 统计天数,默认为 7 天
 * @returns 趋势数据列表
 */
export async function getDashboardTrendApi(days: number = 7) {
  return requestClient.get('/dashboard/trend', { params: { days } });
}

/**
 * 获取最近的操作日志
 * @param count 获取日志数量,默认为 10 条
 * @returns 最近操作日志列表
 */
export async function getRecentLogsApi(count: number = 10) {
  return requestClient.get('/dashboard/recent-logs', { params: { count } });
}
