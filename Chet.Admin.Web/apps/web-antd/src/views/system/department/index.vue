<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page, useVbenModal } from '@vben/common-ui';
import { Plus } from '@vben/icons';
import { useAccess } from '@vben/access';

import { Button, message } from 'ant-design-vue';

import { useVbenForm } from '#/adapter/form';
import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import { createDeptApi, deleteDeptApi, getAllDeptsApi, getDeptTreeApi, updateDeptApi } from '#/api/system/department';
import { $t } from '#/locales';
import { ref } from 'vue';

const { hasAccessByCodes } = useAccess();

function buildTreeSelect(items: any[], excludeId?: number): any[] {
  return items
    .filter((item: any) => item.id !== excludeId)
    .map((item: any) => ({
      label: item.name,
      value: item.id,
      children: item.children ? buildTreeSelect(item.children, excludeId) : undefined,
    }));
}

const columns: VxeTableGridColumns = [
  { field: 'name', title: $t('system.department.columns.name'), minWidth: 180, treeNode: true, align: 'left' },
  { field: 'code', title: $t('system.department.columns.code'), minWidth: 120 },
  { field: 'leader', title: $t('system.department.columns.leader'), width: 100 },
  { field: 'phone', title: $t('system.department.columns.phone'), minWidth: 120 },
  { field: 'email', title: $t('system.department.columns.email'), minWidth: 160 },
  { field: 'sort', title: $t('system.common.columns.sort'), width: 70 },
  { field: 'isEnabled', title: $t('system.common.columns.isEnabled'), width: 70, cellRender: { name: 'CellTag' } },
  { align: 'center', field: 'operation', fixed: 'right', slots: { default: 'action' }, title: $t('system.common.columns.operation'), width: 180 },
];

const [Grid, gridApi] = useVbenVxeGrid({
  gridOptions: {
    columns,
    height: 'auto',
    keepSource: true,
    proxyConfig: {
      ajax: {
        query: async () => {
          const list = await getAllDeptsApi();
          return { items: list || [], total: list?.length || 0 };
        },
      },
    },
    rowConfig: { keyField: 'id' },
    treeConfig: { parentField: 'parentId', rowField: 'id', transform: true, expandAll: true, indent: 20 },
    // 部门为树形结构，一次性加载所有节点构建树，禁用分页器
    pagerConfig: { enabled: false },
    toolbarConfig: { custom: true, refresh: true, zoom: true },
  } as VxeTableGridOptions,
});

function expandAll() { gridApi.grid?.setAllTreeExpand(true); }
function collapseAll() { gridApi.grid?.setAllTreeExpand(false); }

const formSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'name', label: $t('system.department.form.name'), rules: 'required' },
  { component: 'Input', fieldName: 'code', label: $t('system.department.form.code'), rules: 'required',
    help: $t('system.department.form.codeHelp'),
  },
  { component: 'TreeSelect', fieldName: 'parentId', label: $t('system.department.form.parentId'),
    componentProps: {
      treeData: [],
      placeholder: $t('system.department.form.parentIdPlaceholder'),
      allowClear: true,
      showSearch: true,
      treeNodeFilterProp: 'label',
      treeLine: true,
      treeDefaultExpandAll: true,
      dropdownStyle: { maxHeight: '400px' },
      style: { width: '100%' },
    },
  },
  { component: 'Input', fieldName: 'leader', label: $t('system.department.form.leader') },
  { component: 'Input', fieldName: 'phone', label: $t('system.department.form.phone') },
  { component: 'Input', fieldName: 'email', label: $t('system.department.form.email') },
  { component: 'InputNumber', fieldName: 'sort', label: $t('system.common.form.sort'), defaultValue: 0, componentProps: { style: { width: '100%' } } },
  { component: 'Switch', fieldName: 'isEnabled', label: $t('system.common.form.isEnabled'), defaultValue: true },
];

const [Form, formApi] = useVbenForm({ schema: formSchema, showDefaultActions: false });

// 当前编辑的部门ID，0 表示新增
const editingId = ref(0);

const [Modal, modalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await formApi.getValues();
    if (editingId.value) { await updateDeptApi(editingId.value, values); message.success($t('system.common.messages.updateSuccess')); }
    else { await createDeptApi(values); message.success($t('system.common.messages.createSuccess')); }
    modalApi.close(); gridApi.query();
  },
  async onOpenChange(isOpen) {
    if (isOpen) {
      formApi.resetForm();
      const data = modalApi.getData<Record<string, any>>();
      editingId.value = data?.id || 0;
      const deptTree: any[] = await getDeptTreeApi() || [];
      const excludeId = data?.id;
      formApi.updateSchema([{
        fieldName: 'parentId',
        componentProps: { treeData: buildTreeSelect(deptTree, excludeId) },
      }]);
      if (data) formApi.setValues(data);
    }
  },
});

function onCreate(parentId = 0) { modalApi.setData({ parentId }).open(); }
function onEdit(row: any) { modalApi.setData(row).open(); }
function onDelete(row: any) {
  deleteDeptApi(row.id).then(() => { message.success($t('system.common.messages.deleteSuccess')); gridApi.query(); });
}
</script>

<template>
  <Page auto-content-height>
    <Modal :title="$t('system.department.title')">
      <Form />
    </Modal>
    <Grid :table-title="$t('system.department.tableTitle')">
      <template #toolbar-tools>
        <Button class="mr-2" @click="expandAll">{{ $t('system.common.actions.expandAll') }}</Button>
        <Button class="mr-2" @click="collapseAll">{{ $t('system.common.actions.collapseAll') }}</Button>
        <Button v-if="hasAccessByCodes(['system:dept:create'])" type="primary" @click="onCreate(0)"><Plus class="mr-2 size-4" />{{ $t('system.common.actions.create') }}</Button>
      </template>
      <template #action="{ row }">
        <VbenTableAction
          :actions="[
            { text: $t('system.common.actions.create'), auth: 'system:dept:create', onClick: () => onCreate(row.id) },
            { text: $t('system.common.actions.edit'), auth: 'system:dept:update', onClick: () => onEdit(row) },
          ]"
          :dropdown-actions="[{ text: $t('system.common.actions.delete'), auth: 'system:dept:delete', danger: true, popConfirm: { title: $t('system.common.actions.confirmDelete'), confirm: () => onDelete(row) } }]"
        />
      </template>
    </Grid>
  </Page>
</template>

<style lang="scss" scoped>
:deep(.vxe-tree--btn-wrapper) {
  .vxe-tree-icon {
    color: hsl(var(--muted-foreground));
    transition: all 0.2s ease;

    &:hover {
      color: hsl(var(--primary));
    }
  }
}

:deep(.vxe-tree-cell) {
  .vxe-tree-wrapper {
    align-items: center;
  }
}

/* 树形节点 hover 效果 */
:deep(.vxe-body--row) {
  transition: background-color 0.2s ease;
}
</style>
