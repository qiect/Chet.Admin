<script lang="ts" setup>
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page } from '@vben/common-ui';
import { useAccess } from '@vben/access';

import { Button, message } from 'ant-design-vue';

import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import { getOnlineUsersApi, forceOfflineApi } from '#/api/system/online-user';

const { hasAccessByCodes } = useAccess();

// 时间格式化
function formatTime(value: string) {
  if (!value) return '-';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '-';
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}

const columns: VxeTableGridColumns<any> = [
  { field: 'userId', title: 'ID', width: 70 },
  { field: 'userName', title: '用户名', minWidth: 120 },
  { field: 'clientIp', title: '登录IP', minWidth: 140 },
  {
    field: 'loginTime',
    title: '登录时间',
    minWidth: 180,
    slots: { default: ({ row }) => formatTime(row.loginTime) },
  },
  {
    field: 'lastActiveTime',
    title: '最后活跃',
    minWidth: 180,
    slots: { default: ({ row }) => formatTime(row.lastActiveTime) },
  },
  {
    align: 'center',
    field: 'operation',
    fixed: 'right',
    slots: { default: 'action' },
    title: '操作',
    width: 120,
  },
];

const [Grid, gridApi] = useVbenVxeGrid({
  gridOptions: {
    columns,
    height: 'auto',
    pagerConfig: { enabled: false },
    proxyConfig: {
      ajax: {
        query: async () => {
          const res = await getOnlineUsersApi();
          return { items: res || [], total: (res || []).length };
        },
      },
    },
    rowConfig: {
      keyField: 'userId',
    },
    toolbarConfig: {
      custom: false,
      refresh: { code: 'query' },
      zoom: false,
    },
  } as VxeTableGridOptions,
});

async function onForceOffline(userId: number) {
  try {
    await forceOfflineApi(userId);
    message.success('已强制下线');
    gridApi.query();
  } catch {
    message.error('操作失败');
  }
}
</script>

<template>
  <Page auto-content-height>
    <Grid>
      <template #action="{ row }">
        <VbenTableAction
          v-if="hasAccessByCodes(['system:online:force-offline'])"
          :actions="[
            { text: '强制下线', danger: true, onClick: () => onForceOffline(row.userId) },
          ]"
        />
      </template>
    </Grid>
  </Page>
</template>
