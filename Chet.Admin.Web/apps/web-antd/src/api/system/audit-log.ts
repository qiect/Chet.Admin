import { requestClient } from '#/api/request';

/**
 * 分页查询审计日志列表
 * @param params 查询参数,包含分页信息、关键字、用户ID、模块、操作类型及时间范围
 * @returns 审计日志分页列表,包含数据项和总记录数
 */
export async function getAuditLogListApi(params: {
  pageNumber: number;
  pageSize: number;
  keyword?: string;
  userId?: number;
  module?: string;
  action?: string;
  startTime?: string;
  endTime?: string;
}) {
  const result = await requestClient.get('/auditlogs/paged', { params });
  return { items: result?.items || [], total: result?.metadata?.totalCount || 0 };
}

/**
 * 清除指定时间之前的审计日志
 * @param before 截止时间,早于此时间的日志将被清除
 * @returns 清除操作结果
 */
export async function clearAuditLogsApi(before: string) {
  return requestClient.delete('/auditlogs/clear', { params: { before } });
}
