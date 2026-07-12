<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page, useVbenModal } from '@vben/common-ui';
import { Pencil, Plus, Trash2 } from '@vben/icons';
import { useAccess } from '@vben/access';

import { Button, message } from 'ant-design-vue';
import { ref } from 'vue';

import { useVbenForm } from '#/adapter/form';
import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import { createDictApi, deleteDictApi, getDictListApi, updateDictApi } from '#/api/system/dictionary';
import { $t } from '#/locales';

const { hasAccessByCodes } = useAccess();

const searchSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'keyword', label: $t('system.common.search.keyword') },
  { component: 'Input', fieldName: 'dictType', label: $t('system.dictionary.search.dictType') },
];

const columns: VxeTableGridColumns = [
  { field: 'id', title: $t('system.common.columns.id'), width: 80 },
  { field: 'dictType', title: $t('system.dictionary.columns.dictType'), minWidth: 120 },
  { field: 'name', title: $t('system.dictionary.columns.name'), minWidth: 150 },
  { field: 'value', title: $t('system.dictionary.columns.value'), minWidth: 100 },
  { field: 'label', title: $t('system.dictionary.columns.label'), minWidth: 120 },
  { field: 'sort', title: $t('system.common.columns.sort'), width: 80 },
  { field: 'isEnabled', title: $t('system.common.columns.isEnabled'), width: 80, cellRender: { name: 'CellTag' } },
  { field: 'remark', title: $t('system.dictionary.columns.remark'), minWidth: 150 },
  { align: 'center', field: 'operation', fixed: 'right', slots: { default: 'action' }, title: $t('system.common.columns.operation'), width: 100 },
];

const [Grid, gridApi] = useVbenVxeGrid({
  formOptions: { schema: searchSchema, submitOnChange: true },
  gridOptions: {
    columns,
    height: 'auto',
    keepSource: true,
    proxyConfig: {
      ajax: { query: async ({ page }, formValues) => await getDictListApi({ pageNumber: page.currentPage, pageSize: page.pageSize, ...formValues }) },
    },
    rowConfig: { keyField: 'id' },
    toolbarConfig: { custom: true, refresh: true, search: true, zoom: true },
  } as VxeTableGridOptions,
});

const formSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'dictType', label: $t('system.dictionary.form.dictType'), rules: 'required' },
  { component: 'Input', fieldName: 'name', label: $t('system.dictionary.form.name'), rules: 'required' },
  { component: 'Input', fieldName: 'value', label: $t('system.dictionary.form.value'), rules: 'required' },
  { component: 'Input', fieldName: 'label', label: $t('system.dictionary.form.label'), rules: 'required' },
  { component: 'InputNumber', fieldName: 'sort', label: $t('system.common.form.sort'), defaultValue: 0, componentProps: { style: { width: '100%' } } },
  { component: 'Switch', fieldName: 'isEnabled', label: $t('system.common.form.isEnabled'), defaultValue: true },
  { component: 'Textarea', fieldName: 'remark', label: $t('system.common.form.remark') },
  { component: 'InputNumber', fieldName: 'parentId', label: $t('system.dictionary.form.parentId'), defaultValue: 0, componentProps: { style: { width: '100%' } } },
];

const [Form, formApi] = useVbenForm({
  schema: formSchema,
  showDefaultActions: false,
  commonConfig: {
    labelWidth: 140, labelClass: 'whitespace-nowrap',
  },
});

// 当前编辑的字典ID，0 表示新增
const editingId = ref(0);

const [Modal, modalApi] = useVbenModal({
  draggable: true,
  onConfirm: async () => {
    const values = await formApi.getValues();
    if (editingId.value) { await updateDictApi(editingId.value, values); message.success($t('system.common.messages.updateSuccess')); }
    else { await createDictApi(values); message.success($t('system.common.messages.createSuccess')); }
    modalApi.close(); gridApi.query();
  },
  onOpenChange(isOpen) {
    if (isOpen) {
      formApi.resetForm();
      const data = modalApi.getData<Record<string, any>>();
      editingId.value = data?.id || 0;
      if (data) formApi.setValues(data);
    }
  },
});

function onCreate() { modalApi.setData({}).open(); }
function onEdit(row: any) { modalApi.setData(row).open(); }
function onDelete(row: any) {
  deleteDictApi(row.id).then(() => { message.success($t('system.common.messages.deleteSuccess')); gridApi.query(); });
}
</script>

<template>
  <Page auto-content-height>
    <Modal :title="$t('system.dictionary.title')">
      <Form />
    </Modal>
    <Grid :table-title="$t('system.dictionary.tableTitle')">
      <template #toolbar-tools>
        <Button v-if="hasAccessByCodes(['system:dict:create'])" type="primary" @click="onCreate"><Plus class="mr-2 size-4" />{{ $t('system.common.actions.create') }}</Button>
      </template>
      <template #action="{ row }">
        <VbenTableAction
          :actions="[{ icon: Pencil, auth: 'system:dict:update', tooltip: $t('system.common.actions.edit'), onClick: () => onEdit(row) }]"
          :dropdown-actions="[{ icon: Trash2, text: $t('system.common.actions.delete'), auth: 'system:dict:delete', danger: true, popConfirm: { title: $t('system.common.actions.confirmDelete'), confirm: () => onDelete(row) } }]"
        />
      </template>
    </Grid>
  </Page>
</template>