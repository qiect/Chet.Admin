<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page } from '@vben/common-ui';
import { useAccess } from '@vben/access';
import { formatDateTime } from '@vben/utils';

import { Button, DatePicker, message, Tag } from 'ant-design-vue';

import { useVbenVxeGrid } from '#/adapter/vxe-table';
import { getAuditLogListApi, clearAuditLogsApi } from '#/api/system/audit-log';
import { $t } from '#/locales';
import { ref, h } from 'vue';

const { hasAccessByCodes } = useAccess();

const actionColorMap: Record<string, string> = {
  Create: 'blue',
  Update: 'orange',
  Delete: 'red',
  Login: 'green',
  Logout: 'default',
  Assign: 'purple',
};
const moduleColorMap: Record<string, string> = {
  User: 'blue',
  Role: 'purple',
  Menu: 'cyan',
  Department: 'green',
  Permission: 'orange',
  Dictionary: 'default',
  Auth: 'geekblue',
  AuditLog: 'default',
};

const searchSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'keyword', label: $t('system.common.search.keyword') },
  {
    component: 'Select',
    fieldName: 'module',
    label: $t('system.auditLog.search.module'),
    componentProps: {
      options: [
        { label: $t('system.auditLog.module.User'), value: 'User' },
        { label: $t('system.auditLog.module.Role'), value: 'Role' },
        { label: $t('system.auditLog.module.Menu'), value: 'Menu' },
        { label: $t('system.auditLog.module.Department'), value: 'Department' },
        { label: $t('system.auditLog.module.Permission'), value: 'Permission' },
        { label: $t('system.auditLog.module.Dictionary'), value: 'Dictionary' },
        { label: $t('system.auditLog.module.Auth'), value: 'Auth' },
      ],
      allowClear: true,
      placeholder: $t('system.common.all'),
      style: { width: '100%' },
    },
  },
  {
    component: 'Select',
    fieldName: 'action',
    label: $t('system.auditLog.search.action'),
    componentProps: {
      options: [
        { label: $t('system.auditLog.action.Create'), value: 'Create' },
        { label: $t('system.auditLog.action.Update'), value: 'Update' },
        { label: $t('system.auditLog.action.Delete'), value: 'Delete' },
        { label: $t('system.auditLog.action.Login'), value: 'Login' },
        { label: $t('system.auditLog.action.Logout'), value: 'Logout' },
        { label: $t('system.auditLog.action.Assign'), value: 'Assign' },
      ],
      allowClear: true,
      placeholder: $t('system.common.all'),
      style: { width: '100%' },
    },
  },
  {
    component: 'RangePicker',
    fieldName: 'timeRange',
    label: $t('system.auditLog.search.timeRange'),
    componentProps: { style: { width: '100%' }, format: 'YYYY-MM-DD' },
  },
];

const columns: VxeTableGridColumns = [
  { field: 'id', title: $t('system.common.columns.id'), width: 70 },
  { field: 'userName', title: $t('system.auditLog.columns.userName'), minWidth: 100 },
  {
    field: 'module',
    title: $t('system.auditLog.columns.module'),
    width: 100,
    slots: {
      default: ({ row }) =>
        h(Tag, { color: moduleColorMap[row.module] || 'default' }, () => row.module),
    },
  },
  {
    field: 'action',
    title: $t('system.auditLog.columns.action'),
    width: 80,
    slots: {
      default: ({ row }) =>
        h(Tag, { color: actionColorMap[row.action] || 'default' }, () => row.action),
    },
  },
  { field: 'description', title: $t('system.common.columns.description'), minWidth: 180 },
  { field: 'requestPath', title: $t('system.auditLog.columns.requestPath'), minWidth: 160 },
  { field: 'clientIp', title: $t('system.auditLog.columns.clientIp'), width: 130 },
  {
    field: 'duration',
    title: $t('system.auditLog.columns.duration'),
    width: 80,
    slots: { default: ({ row }) => `${row.duration}ms` },
  },
  { field: 'operatedAt', title: $t('system.auditLog.columns.operatedAt'), minWidth: 170,
    slots: { default: ({ row }) => row.operatedAt ? formatDateTime(row.operatedAt) : $t('system.common.empty') },
  },
];

const [Grid, gridApi] = useVbenVxeGrid({
  formOptions: { schema: searchSchema, submitOnChange: true },
  gridOptions: {
    columns,
    height: 'auto',
    keepSource: true,
    proxyConfig: {
      ajax: {
        query: async ({ page }, formValues) => {
          const params: any = {
            pageNumber: page.currentPage,
            pageSize: page.pageSize,
            keyword: formValues.keyword,
            module: formValues.module,
            action: formValues.action,
          };
          if (formValues.timeRange && formValues.timeRange.length === 2) {
            params.startTime = formValues.timeRange[0];
            params.endTime = formValues.timeRange[1];
          }
          return await getAuditLogListApi(params);
        },
      },
    },
    rowConfig: { keyField: 'id' },
    toolbarConfig: { custom: true, refresh: true, search: true, zoom: true },
  } as VxeTableGridOptions,
});

const clearDateStr = ref('');

function onClearDateChange(_date: any, dateStr: string | string[]) {
  clearDateStr.value = Array.isArray(dateStr) ? dateStr[0] : dateStr;
}

async function onClearLogs() {
  if (!clearDateStr.value) {
    message.warning($t('system.auditLog.messages.selectClearDate'));
    return;
  }
  await clearAuditLogsApi(clearDateStr.value);
  message.success($t('system.auditLog.messages.clearSuccess'));
  gridApi.query();
}
</script>

<template>
  <Page auto-content-height>
    <Grid :table-title="$t('system.auditLog.tableTitle')">
      <template #toolbar-tools>
        <div
          v-if="hasAccessByCodes(['system:audit:clear'])"
          class="flex items-center gap-2"
        >
          <DatePicker
            :placeholder="$t('system.auditLog.clearBeforeDatePlaceholder')"
            format="YYYY-MM-DD"
            @change="onClearDateChange"
          />
          <Button danger size="small" @click="onClearLogs">{{ $t('system.auditLog.clearButton') }}</Button>
        </div>
      </template>
    </Grid>
  </Page>
</template>
