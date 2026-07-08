<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page } from '@vben/common-ui';
import { useAccess } from '@vben/access';

import { Button, DatePicker, message, Tag } from 'ant-design-vue';

import { useVbenVxeGrid } from '#/adapter/vxe-table';
import { getAuditLogListApi, clearAuditLogsApi } from '#/api/system/audit-log';
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
  { component: 'Input', fieldName: 'keyword', label: '关键字' },
  {
    component: 'Select',
    fieldName: 'module',
    label: '操作模块',
    componentProps: {
      options: [
        { label: '用户', value: 'User' },
        { label: '角色', value: 'Role' },
        { label: '菜单', value: 'Menu' },
        { label: '部门', value: 'Department' },
        { label: '权限', value: 'Permission' },
        { label: '字典', value: 'Dictionary' },
        { label: '认证', value: 'Auth' },
      ],
      allowClear: true,
      placeholder: '全部',
      style: { width: '100%' },
    },
  },
  {
    component: 'Select',
    fieldName: 'action',
    label: '操作类型',
    componentProps: {
      options: [
        { label: '创建', value: 'Create' },
        { label: '更新', value: 'Update' },
        { label: '删除', value: 'Delete' },
        { label: '登录', value: 'Login' },
        { label: '登出', value: 'Logout' },
        { label: '分配', value: 'Assign' },
      ],
      allowClear: true,
      placeholder: '全部',
      style: { width: '100%' },
    },
  },
  {
    component: 'RangePicker',
    fieldName: 'timeRange',
    label: '操作时间',
    componentProps: { style: { width: '100%' }, format: 'YYYY-MM-DD' },
  },
];

const columns: VxeTableGridColumns = [
  { field: 'id', title: 'ID', width: 70 },
  { field: 'userName', title: '操作人', minWidth: 100 },
  {
    field: 'module',
    title: '模块',
    width: 100,
    slots: {
      default: ({ row }) =>
        h(Tag, { color: moduleColorMap[row.module] || 'default' }, () => row.module),
    },
  },
  {
    field: 'action',
    title: '操作',
    width: 80,
    slots: {
      default: ({ row }) =>
        h(Tag, { color: actionColorMap[row.action] || 'default' }, () => row.action),
    },
  },
  { field: 'description', title: '描述', minWidth: 180 },
  { field: 'requestPath', title: '请求路径', minWidth: 160 },
  { field: 'clientIp', title: 'IP', width: 130 },
  {
    field: 'duration',
    title: '耗时',
    width: 80,
    slots: { default: ({ row }) => `${row.duration}ms` },
  },
  { field: 'operatedAt', title: '操作时间', minWidth: 170,
    slots: { default: ({ row }) => row.operatedAt ? new Date(row.operatedAt).toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' }) : '-' },
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
    message.warning('请选择清理日期');
    return;
  }
  await clearAuditLogsApi(clearDateStr.value);
  message.success('清理成功');
  gridApi.query();
}
</script>

<template>
  <Page auto-content-height>
    <Grid table-title="操作日志">
      <template #toolbar-tools>
        <div
          v-if="hasAccessByCodes(['system:audit:clear'])"
          class="flex items-center gap-2"
        >
          <DatePicker
            :placeholder="'清理此日期之前'"
            format="YYYY-MM-DD"
            @change="onClearDateChange"
          />
          <Button danger size="small" @click="onClearLogs">清理</Button>
        </div>
      </template>
    </Grid>
  </Page>
</template>
