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
  { field: 'name', title: '部门名称', minWidth: 180, treeNode: true },
  { field: 'code', title: '编码', minWidth: 120 },
  { field: 'leader', title: '负责人', width: 100 },
  { field: 'phone', title: '电话', minWidth: 120 },
  { field: 'email', title: '邮箱', minWidth: 160 },
  { field: 'sort', title: '排序', width: 70 },
  { field: 'isEnabled', title: '状态', width: 70, cellRender: { name: 'CellTag' } },
  { align: 'center', field: 'operation', fixed: 'right', slots: { default: 'action' }, title: '操作', width: 180 },
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
    toolbarConfig: { custom: true, refresh: true, zoom: true },
  } as VxeTableGridOptions,
});

function expandAll() { gridApi.grid?.setAllTreeExpand(true); }
function collapseAll() { gridApi.grid?.setAllTreeExpand(false); }

const formSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'name', label: '部门名称', rules: 'required' },
  { component: 'Input', fieldName: 'code', label: '部门编码', rules: 'required',
    help: '如 TECH, HR, FIN',
  },
  { component: 'TreeSelect', fieldName: 'parentId', label: '上级部门',
    componentProps: {
      treeData: [],
      placeholder: '留空为顶级部门',
      allowClear: true,
      showSearch: true,
      treeNodeFilterProp: 'label',
      treeLine: true,
      treeDefaultExpandAll: true,
      dropdownStyle: { maxHeight: '400px' },
      style: { width: '100%' },
    },
  },
  { component: 'Input', fieldName: 'leader', label: '负责人' },
  { component: 'Input', fieldName: 'phone', label: '联系电话' },
  { component: 'Input', fieldName: 'email', label: '邮箱' },
  { component: 'InputNumber', fieldName: 'sort', label: '排序', defaultValue: 0, componentProps: { style: { width: '100%' } } },
  { component: 'Switch', fieldName: 'isEnabled', label: '启用', defaultValue: true },
];

const [Form, formApi] = useVbenForm({ schema: formSchema, showDefaultActions: false });

const [Modal, modalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await formApi.getValues();
    const id = values.id;
    if (id) { await updateDeptApi(id, values); message.success('更新成功'); }
    else { await createDeptApi(values); message.success('创建成功'); }
    modalApi.close(); gridApi.query();
  },
  async onOpenChange(isOpen) {
    if (isOpen) {
      formApi.resetForm();
      const data = modalApi.getData<Record<string, any>>();
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
  deleteDeptApi(row.id).then(() => { message.success('删除成功'); gridApi.query(); });
}
</script>

<template>
  <Page auto-content-height>
    <Modal title="部门管理">
      <Form />
    </Modal>
    <Grid table-title="部门列表">
      <template #toolbar-tools>
        <Button class="mr-2" @click="expandAll">展开全部</Button>
        <Button class="mr-2" @click="collapseAll">折叠全部</Button>
        <Button v-if="hasAccessByCodes(['system:dept:create'])" type="primary" @click="onCreate(0)"><Plus class="mr-2 size-4" />新增</Button>
      </template>
      <template #action="{ row }">
        <VbenTableAction
          :actions="[
            { text: '新增', auth: 'system:dept:create', onClick: () => onCreate(row.id) },
            { text: '编辑', auth: 'system:dept:update', onClick: () => onEdit(row) },
          ]"
          :dropdown-actions="[{ text: '删除', auth: 'system:dept:delete', danger: true, popConfirm: { title: '确认删除？', confirm: () => onDelete(row) } }]"
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
