import { requestClient } from '#/api/request';

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

export async function clearAuditLogsApi(before: string) {
  return requestClient.delete('/auditlogs/clear', { params: { before } });
}
