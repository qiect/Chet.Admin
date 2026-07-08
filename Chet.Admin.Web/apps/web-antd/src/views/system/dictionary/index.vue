<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page, useVbenModal } from '@vben/common-ui';
import { Plus } from '@vben/icons';
import { useAccess } from '@vben/access';

import { Button, message } from 'ant-design-vue';

import { useVbenForm } from '#/adapter/form';
import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import { createDictApi, deleteDictApi, getDictListApi, updateDictApi } from '#/api/system/dictionary';

const { hasAccessByCodes } = useAccess();

const searchSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'keyword', label: '关键字' },
  { component: 'Input', fieldName: 'dictType', label: '字典类型' },
];

const columns: VxeTableGridColumns = [
  { field: 'id', title: 'ID', width: 80 },
  { field: 'dictType', title: '字典类型', minWidth: 120 },
  { field: 'name', title: '名称', minWidth: 150 },
  { field: 'value', title: '值', minWidth: 100 },
  { field: 'label', title: '标签', minWidth: 120 },
  { field: 'sort', title: '排序', width: 80 },
  { field: 'isEnabled', title: '状态', width: 80, cellRender: { name: 'CellTag' } },
  { field: 'remark', title: '备注', minWidth: 150 },
  { align: 'center', field: 'operation', fixed: 'right', slots: { default: 'action' }, title: '操作', width: 160 },
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
  { component: 'Input', fieldName: 'dictType', label: '字典类型', rules: 'required' },
  { component: 'Input', fieldName: 'name', label: '名称', rules: 'required' },
  { component: 'Input', fieldName: 'value', label: '字典值', rules: 'required' },
  { component: 'Input', fieldName: 'label', label: '标签', rules: 'required' },
  { component: 'InputNumber', fieldName: 'sort', label: '排序', defaultValue: 0, componentProps: { style: { width: '100%' } } },
  { component: 'Switch', fieldName: 'isEnabled', label: '启用', defaultValue: true },
  { component: 'Textarea', fieldName: 'remark', label: '备注' },
  { component: 'InputNumber', fieldName: 'parentId', label: '父级ID', defaultValue: 0, componentProps: { style: { width: '100%' } } },
];

const [Form, formApi] = useVbenForm({ schema: formSchema, showDefaultActions: false });

const [Modal, modalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await formApi.getValues();
    const id = values.id;
    if (id) { await updateDictApi(id, values); message.success('更新成功'); }
    else { await createDictApi(values); message.success('创建成功'); }
    modalApi.close(); gridApi.query();
  },
  onOpenChange(isOpen) {
    if (isOpen) { formApi.resetForm(); const data = modalApi.getData<Record<string, any>>(); if (data) formApi.setValues(data); }
  },
});

function onCreate() { modalApi.setData({}).open(); }
function onEdit(row: any) { modalApi.setData(row).open(); }
function onDelete(row: any) {
  deleteDictApi(row.id).then(() => { message.success('删除成功'); gridApi.query(); });
}
</script>

<template>
  <Page auto-content-height>
    <Modal title="字典管理">
      <Form />
    </Modal>
    <Grid table-title="字典列表">
      <template #toolbar-tools>
        <Button v-if="hasAccessByCodes(['system:dict:create'])" type="primary" @click="onCreate"><Plus class="mr-2 size-4" />新增</Button>
      </template>
      <template #action="{ row }">
        <VbenTableAction
          :actions="[{ text: '编辑', auth: 'system:dict:update', onClick: () => onEdit(row) }]"
          :dropdown-actions="[{ text: '删除', auth: 'system:dict:delete', danger: true, popConfirm: { title: '确认删除？', confirm: () => onDelete(row) } }]"
        />
      </template>
    </Grid>
  </Page>
</template>