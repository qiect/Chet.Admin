<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page, useVbenModal } from '@vben/common-ui';
import { Plus, Trash2 } from '@vben/icons';
import { useAccess } from '@vben/access';
import { formatDateTime } from '@vben/utils';

import { Button, message, Tag } from 'ant-design-vue';

import { useVbenForm } from '#/adapter/form';
import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import {
  createNotificationApi,
  deleteNotificationApi,
  getNotificationListApi,
} from '#/api/system/notification';
import { getUserListApi } from '#/api/system/user';
import { $t } from '#/locales';
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
  Announcement: $t('system.notification.type.Announcement'),
  Notification: $t('system.notification.type.Notification'),
  Todo: $t('system.notification.type.Todo'),
};
const priorityLabelMap: Record<string, string> = {
  Low: $t('system.notification.priority.Low'),
  Normal: $t('system.notification.priority.Normal'),
  High: $t('system.notification.priority.High'),
  Urgent: $t('system.notification.priority.Urgent'),
};

const searchSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'keyword', label: $t('system.common.search.keyword') },
];

const columns: VxeTableGridColumns = [
  { field: 'id', title: $t('system.common.columns.id'), width: 70 },
  { field: 'title', title: $t('system.notification.columns.title'), minWidth: 180 },
  {
    field: 'type',
    title: $t('system.notification.columns.type'),
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
    title: $t('system.notification.columns.priority'),
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
    title: $t('system.notification.columns.isGlobal'),
    width: 70,
    cellRender: { name: 'CellTag' },
  },
  {
    field: 'senderName',
    title: $t('system.notification.columns.senderName'),
    width: 110,
    slots: { default: ({ row }) => row.senderName ?? (row.senderId ? `${$t('system.notification.columns.userPrefix')}${row.senderId}` : $t('system.notification.columns.systemSender')) },
  },
  { field: 'createdAt', title: $t('system.common.columns.createdAt'), minWidth: 170,
    slots: { default: ({ row }) => row.createdAt ? formatDateTime(row.createdAt) : $t('system.common.empty') },
  },
  {
    align: 'center',
    field: 'operation',
    fixed: 'right',
    slots: { default: 'action' },
    title: $t('system.common.columns.operation'),
    width: 80,
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
  { component: 'Input', fieldName: 'title', label: $t('system.notification.form.title'), rules: 'required' },
  { component: 'Textarea', fieldName: 'content', label: $t('system.notification.form.content'), rules: 'required', componentProps: { rows: 4 } },
  {
    component: 'Select',
    fieldName: 'type',
    label: $t('system.notification.form.type'),
    defaultValue: 'Notification',
    componentProps: {
      options: [
        { label: $t('system.notification.type.Announcement'), value: 'Announcement' },
        { label: $t('system.notification.type.Notification'), value: 'Notification' },
        { label: $t('system.notification.type.Todo'), value: 'Todo' },
      ],
      style: { width: '100%' },
    },
  },
  {
    component: 'Select',
    fieldName: 'priority',
    label: $t('system.notification.form.priority'),
    defaultValue: 'Normal',
    componentProps: {
      options: [
        { label: $t('system.notification.priority.Low'), value: 'Low' },
        { label: $t('system.notification.priority.Normal'), value: 'Normal' },
        { label: $t('system.notification.priority.High'), value: 'High' },
        { label: $t('system.notification.priority.Urgent'), value: 'Urgent' },
      ],
      style: { width: '100%' },
    },
  },
  { component: 'Switch', fieldName: 'isGlobal', label: $t('system.notification.form.isGlobal'), defaultValue: false },
  {
    component: 'Select',
    fieldName: 'recipientUserIds',
    label: $t('system.notification.form.recipientUserIds'),
    dependencies: {
      triggerFields: ['isGlobal'],
      rule: (values) => {
        return { componentProps: { mode: 'multiple', options: userOptions.value, placeholder: $t('system.notification.form.recipientUserIdsPlaceholder'), allowClear: true, style: { width: '100%' } } };
      },
      if: (values) => !values.isGlobal,
    },
  },
];

const [Form, formApi] = useVbenForm({
  schema: formSchema,
  showDefaultActions: false,
  commonConfig: {
    labelWidth: 140, labelClass: 'whitespace-nowrap',
  },
});

const [Modal, modalApi] = useVbenModal({
  draggable: true,
  onConfirm: async () => {
    const values = await formApi.getValues();
    const { ...submitData } = values;
    if (!submitData.isGlobal && (!submitData.recipientUserIds || submitData.recipientUserIds.length === 0)) {
      message.warning($t('system.notification.messages.selectRecipients'));
      return;
    }
    if (submitData.isGlobal) {
      delete submitData.recipientUserIds;
    }
    await createNotificationApi(submitData);
    message.success($t('system.common.messages.createSuccess'));
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
              placeholder: $t('system.notification.form.recipientUserIdsPlaceholder'),
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
    message.success($t('system.common.messages.deleteSuccess'));
    gridApi.query();
  });
}
</script>

<template>
  <Page auto-content-height>
    <Modal :title="$t('system.notification.modals.create')">
      <Form />
    </Modal>
    <Grid :table-title="$t('system.notification.tableTitle')">
      <template #toolbar-tools>
        <Button
          v-if="hasAccessByCodes(['system:notification:create'])"
          type="primary"
          @click="onCreate"
        >
          <Plus class="mr-2 size-4" />{{ $t('system.common.actions.create') }}
        </Button>
      </template>
      <template #action="{ row }">
        <VbenTableAction
          :dropdown-actions="[
            {
              icon: Trash2,
              text: $t('system.common.actions.delete'),
              auth: 'system:notification:delete',
              danger: true,
              popConfirm: { title: $t('system.common.actions.confirmDelete'), confirm: () => onDelete(row) },
            },
          ]"
        />
      </template>
    </Grid>
  </Page>
</template>
