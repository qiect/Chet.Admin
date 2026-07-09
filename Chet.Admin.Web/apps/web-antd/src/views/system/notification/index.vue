<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page, useVbenModal } from '@vben/common-ui';
import { Plus } from '@vben/icons';
import { useAccess } from '@vben/access';

import { Button, message, Tag } from 'ant-design-vue';

import { useVbenForm } from '#/adapter/form';
import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import {
  createNotificationApi,
  deleteNotificationApi,
  getNotificationListApi,
} from '#/api/system/notification';
import { getUserListApi } from '#/api/system/user';
import { h, ref } from 'vue';

const { hasAccessByCodes } = useAccess();

const typeColorMap: Record<string, string> = {
  Announcement: 'blue',
  Notification: 'green',
  Todo: 'orange',
};
const priorityColorMap: Record<string, string> = {
  Low: 'default',
  Normal: 'blue',
  High: 'orange',
  Urgent: 'red',
};
const typeLabelMap: Record<string, string> = {
  Announcement: '公告',
  Notification: '通知',
  Todo: '待办',
};
const priorityLabelMap: Record<string, string> = {
  Low: '低',
  Normal: '普通',
  High: '高',
  Urgent: '紧急',
};

const searchSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'keyword', label: '关键字' },
];

const columns: VxeTableGridColumns = [
  { field: 'id', title: 'ID', width: 70 },
  { field: 'title', title: '标题', minWidth: 180 },
  {
    field: 'type',
    title: '类型',
    width: 90,
    slots: {
      default: ({ row }) =>
        h(
          Tag,
          { color: typeColorMap[row.type] || 'default' },
          () => typeLabelMap[row.type] || row.type,
        ),
    },
  },
  {
    field: 'priority',
    title: '优先级',
    width: 90,
    slots: {
      default: ({ row }) =>
        h(
          Tag,
          { color: priorityColorMap[row.priority] || 'default' },
          () => priorityLabelMap[row.priority] || row.priority,
        ),
    },
  },
  {
    field: 'isGlobal',
    title: '全局',
    width: 70,
    cellRender: { name: 'CellTag' },
  },
  {
    field: 'senderName',
    title: '发送者',
    width: 110,
    slots: { default: ({ row }) => row.senderName ?? (row.senderId ? `用户${row.senderId}` : '系统') },
  },
  { field: 'createdAt', title: '创建时间', minWidth: 170,
    slots: { default: ({ row }) => row.createdAt ? new Date(row.createdAt).toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' }) : '-' },
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
  formOptions: { schema: searchSchema, submitOnChange: true },
  gridOptions: {
    columns,
    height: 'auto',
    keepSource: true,
    proxyConfig: {
      ajax: {
        query: async ({ page }, formValues) =>
          await getNotificationListApi({
            pageNumber: page.currentPage,
            pageSize: page.pageSize,
            ...formValues,
          }),
      },
    },
    rowConfig: { keyField: 'id' },
    toolbarConfig: { custom: true, refresh: true, search: true, zoom: true },
  } as VxeTableGridOptions,
});

// ========== 创建通知表单 ==========
const userOptions = ref<{ label: string; value: number }[]>([]);

const formSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'title', label: '标题', rules: 'required' },
  { component: 'Textarea', fieldName: 'content', label: '内容', rules: 'required', componentProps: { rows: 4 } },
  {
    component: 'Select',
    fieldName: 'type',
    label: '类型',
    defaultValue: 'Notification',
    componentProps: {
      options: [
        { label: '公告', value: 'Announcement' },
        { label: '通知', value: 'Notification' },
        { label: '待办', value: 'Todo' },
      ],
      style: { width: '100%' },
    },
  },
  {
    component: 'Select',
    fieldName: 'priority',
    label: '优先级',
    defaultValue: 'Normal',
    componentProps: {
      options: [
        { label: '低', value: 'Low' },
        { label: '普通', value: 'Normal' },
        { label: '高', value: 'High' },
        { label: '紧急', value: 'Urgent' },
      ],
      style: { width: '100%' },
    },
  },
  { component: 'Switch', fieldName: 'isGlobal', label: '全局通知', defaultValue: false },
  {
    component: 'Select',
    fieldName: 'recipientUserIds',
    label: '接收者',
    dependencies: {
      triggerFields: ['isGlobal'],
      rule: (values) => {
        return { componentProps: { mode: 'multiple', options: userOptions.value, placeholder: '选择接收用户', allowClear: true, style: { width: '100%' } } };
      },
      if: (values) => !values.isGlobal,
    },
  },
];

const [Form, formApi] = useVbenForm({ schema: formSchema, showDefaultActions: false });

const [Modal, modalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await formApi.getValues();
    const { ...submitData } = values;
    if (!submitData.isGlobal && (!submitData.recipientUserIds || submitData.recipientUserIds.length === 0)) {
      message.warning('非全局通知请选择接收者');
      return;
    }
    if (submitData.isGlobal) {
      delete submitData.recipientUserIds;
    }
    await createNotificationApi(submitData);
    message.success('创建成功');
    modalApi.close();
    gridApi.query();
  },
  async onOpenChange(isOpen) {
    if (isOpen) {
      formApi.resetForm();
      // Load user options
      try {
        const result = await getUserListApi({ pageNumber: 1, pageSize: 200 });
        userOptions.value = (result.items || []).map((u: any) => ({
          label: `${u.name} (${u.email})`,
          value: u.id,
        }));
        formApi.updateSchema([
          {
            fieldName: 'recipientUserIds',
            componentProps: {
              mode: 'multiple',
              options: userOptions.value,
              placeholder: '选择接收用户',
              allowClear: true,
              style: { width: '100%' },
            },
          },
        ]);
      } catch {
        // ignore
      }
    }
  },
});

function onCreate() {
  modalApi.setData({}).open();
}
function onDelete(row: any) {
  deleteNotificationApi(row.id).then(() => {
    message.success('删除成功');
    gridApi.query();
  });
}
</script>

<template>
  <Page auto-content-height>
    <Modal title="新增通知">
      <Form />
    </Modal>
    <Grid table-title="通知公告列表">
      <template #toolbar-tools>
        <Button
          v-if="hasAccessByCodes(['system:notification:create'])"
          type="primary"
          @click="onCreate"
        >
          <Plus class="mr-2 size-4" />新增
        </Button>
      </template>
      <template #action="{ row }">
        <VbenTableAction
          :dropdown-actions="[
            {
              text: '删除',
              auth: 'system:notification:delete',
              danger: true,
              popConfirm: { title: '确认删除？', confirm: () => onDelete(row) },
            },
          ]"
        />
      </template>
    </Grid>
  </Page>
</template>
