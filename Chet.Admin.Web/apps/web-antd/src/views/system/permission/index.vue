<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page, useVbenModal } from '@vben/common-ui';
import { Plus } from '@vben/icons';
import { useAccess } from '@vben/access';

import { Button, message, Tag } from 'ant-design-vue';

import { useVbenForm } from '#/adapter/form';
import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import { createPermissionApi, deletePermissionApi, getPermissionListApi, updatePermissionApi } from '#/api/system/permission';
import { getMenuTreeApi } from '#/api/system/menu';
import { h, onMounted, ref } from 'vue';

const { hasAccessByCodes } = useAccess();

const typeMap: Record<string, { color: string; label: string }> = {
  Menu: { color: 'processing', label: '菜单' },
  Button: { color: 'warning', label: '按钮' },
  Api: { color: 'error', label: '接口' },
};

// 菜单名称映射（表格列用）
const menuNameMap = ref<Map<number, string>>(new Map());

async function loadMenuNames() {
  try {
    const menus: any[] = await getMenuTreeApi() || [];
    const map = new Map<number, string>();
    function collect(items: any[]) {
      for (const item of items) {
        map.set(item.id, item.name);
        if (item.children) collect(item.children);
      }
    }
    collect(menus);
    menuNameMap.value = map;
    return menus;
  } catch {
    return [];
  }
}

function buildMenuTreeSelect(items: any[]): any[] {
  return items.map((item: any) => ({
    label: item.name,
    value: item.id,
    children: item.children ? buildMenuTreeSelect(item.children) : undefined,
  }));
}

onMounted(() => { loadMenuNames(); });

const searchSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'keyword', label: '关键字' },
];

const columns: VxeTableGridColumns = [
  { field: 'id', title: 'ID', width: 70 },
  { field: 'code', title: '权限编码', minWidth: 200 },
  { field: 'name', title: '权限名称', minWidth: 120 },
  { field: 'type', title: '类型', width: 90,
    slots: {
      default: ({ row }) => {
        const t = typeMap[row.type];
        return h(Tag, { color: t?.color || 'default' }, () => t?.label || row.type);
      },
    },
  },
  { field: 'menuId', title: '关联菜单', minWidth: 120,
    slots: {
      default: ({ row }) => {
        const name = menuNameMap.value.get(row.menuId);
        return name || (row.menuId ? 'ID:' + row.menuId : '-');
      },
    },
  },
  { field: 'description', title: '描述', minWidth: 150 },
  { align: 'center', field: 'operation', fixed: 'right', slots: { default: 'action' }, title: '操作', width: 160 },
];

const [Grid, gridApi] = useVbenVxeGrid({
  formOptions: { schema: searchSchema, submitOnChange: true },
  gridOptions: {
    columns,
    height: 'auto',
    keepSource: true,
    proxyConfig: {
      ajax: { query: async ({ page }, formValues) => await getPermissionListApi({ pageNumber: page.currentPage, pageSize: page.pageSize, ...formValues }) },
    },
    rowConfig: { keyField: 'id' },
    toolbarConfig: { custom: true, refresh: true, search: true, zoom: true },
  } as VxeTableGridOptions,
});

// 表单 - TreeSelect初始treeData为空，弹窗打开时通过updateSchema动态设置
const formSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'code', label: '权限编码', rules: 'required',
    help: '如 system:user:create',
  },
  { component: 'Input', fieldName: 'name', label: '权限名称', rules: 'required' },
  { component: 'Select', fieldName: 'type', label: '类型', defaultValue: 'Button',
    componentProps: { options: [{ label: '菜单', value: 'Menu' }, { label: '按钮', value: 'Button' }, { label: '接口', value: 'Api' }] },
  },
  { component: 'TreeSelect', fieldName: 'menuId', label: '关联菜单',
    componentProps: {
      treeData: [],
      placeholder: '选择关联菜单',
      allowClear: true,
      showSearch: true,
      treeNodeFilterProp: 'label',
      treeLine: true,
      treeDefaultExpandAll: true,
      dropdownStyle: { maxHeight: '400px' },
      style: { width: '100%' },
    },
  },
  { component: 'Textarea', fieldName: 'description', label: '描述' },
];

const [Form, formApi] = useVbenForm({ schema: formSchema, showDefaultActions: false });

const [Modal, modalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await formApi.getValues();
    const id = values.id;
    if (id) { await updatePermissionApi(id, values); message.success('更新成功'); }
    else { await createPermissionApi(values); message.success('创建成功'); }
    modalApi.close(); gridApi.query(); loadMenuNames();
  },
  async onOpenChange(isOpen) {
    if (isOpen) {
      formApi.resetForm();
      const menus = await loadMenuNames();
      formApi.updateSchema([{
        fieldName: 'menuId',
        componentProps: { treeData: buildMenuTreeSelect(menus || []) },
      }]);
      const data = modalApi.getData<Record<string, any>>();
      if (data) formApi.setValues(data);
    }
  },
});

function onCreate() { modalApi.setData({}).open(); }
function onEdit(row: any) { modalApi.setData(row).open(); }
function onDelete(row: any) {
  deletePermissionApi(row.id).then(() => { message.success('删除成功'); gridApi.query(); });
}
</script>

<template>
  <Page auto-content-height>
    <Modal title="权限管理">
      <Form />
    </Modal>
    <Grid table-title="权限列表">
      <template #toolbar-tools>
        <Button v-if="hasAccessByCodes(['system:permission:create'])" type="primary" @click="onCreate"><Plus class="mr-2 size-4" />新增</Button>
      </template>
      <template #action="{ row }">
        <VbenTableAction
          :actions="[{ text: '编辑', auth: 'system:permission:update', onClick: () => onEdit(row) }]"
          :dropdown-actions="[{ text: '删除', auth: 'system:permission:delete', danger: true, popConfirm: { title: '确认删除？', confirm: () => onDelete(row) } }]"
        />
      </template>
    </Grid>
  </Page>
</template>
