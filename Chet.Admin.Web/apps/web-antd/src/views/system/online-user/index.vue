<script lang="ts" setup>
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page } from '@vben/common-ui';
import { useAccess } from '@vben/access';
import { formatDateTime } from '@vben/utils';

import { Button, message } from 'ant-design-vue';

import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import { getOnlineUsersApi, forceOfflineApi } from '#/api/system/online-user';
import { $t } from '#/locales';

const { hasAccessByCodes } = useAccess();

// 时间格式化
function formatTime(value: string) {
  if (!value) return $t('system.common.empty');
  return formatDateTime(value);
}

const columns: VxeTableGridColumns<any> = [
  { field: 'userId', title: $t('system.common.columns.id'), width: 70 },
  { field: 'userName', title: $t('system.onlineUser.columns.userName'), minWidth: 120 },
  { field: 'clientIp', title: $t('system.onlineUser.columns.clientIp'), minWidth: 140 },
  {
    field: 'loginTime',
    title: $t('system.onlineUser.columns.loginTime'),
    minWidth: 180,
    slots: { default: ({ row }) => formatTime(row.loginTime) },
  },
  {
    field: 'lastActiveTime',
    title: $t('system.onlineUser.columns.lastActiveTime'),
    minWidth: 180,
    slots: { default: ({ row }) => formatTime(row.lastActiveTime) },
  },
  {
    align: 'center',
    field: 'operation',
    fixed: 'right',
    slots: { default: 'action' },
    title: $t('system.common.columns.operation'),
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
    message.success($t('system.onlineUser.messages.forceOfflineSuccess'));
    gridApi.query();
  } catch {
    message.error($t('system.onlineUser.messages.forceOfflineFailed'));
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
            { text: $t('system.onlineUser.actions.forceOffline'), danger: true, onClick: () => onForceOffline(row.userId) },
          ]"
        />
      </template>
    </Grid>
  </Page>
</template>
