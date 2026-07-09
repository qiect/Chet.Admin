<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page, useVbenModal } from '@vben/common-ui';
import { Plus } from '@vben/icons';
import { useAccess } from '@vben/access';

import { Button, message, Tag } from 'ant-design-vue';

import { useVbenForm } from '#/adapter/form';
import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import { createMenuApi, deleteMenuApi, getAllMenusApi, updateMenuApi } from '#/api/system/menu';
import { h, ref } from 'vue';

const { hasAccessByCodes } = useAccess();

// 构建菜单树选择数据
function buildMenuTreeSelect(flatMenus: any[], excludeId?: number): any[] {
  // 先过滤掉排除的节点
  const filtered = flatMenus.filter((m: any) => m.id !== excludeId);
  // 递归建树
  const build = (parentId: number): any[] => {
    return filtered
      .filter((m: any) => (parentId === 0 ? !m.parentId || m.parentId === 0 : m.parentId === parentId))
      .map((m: any) => {
        const children = build(m.id);
        return {
          label: m.name,
          value: m.id,
          children: children.length > 0 ? children : undefined,
        };
      });
  };
  return build(0);
}

const typeMap: Record<string, { color: string; label: string }> = {
  Directory: { color: 'processing', label: '目录' },
  Menu: { color: 'success', label: '菜单' },
  Button: { color: 'warning', label: '按钮' },
};

const columns: VxeTableGridColumns = [
  { field: 'name', title: '菜单名称', minWidth: 180, treeNode: true, align: 'left' },
  { field: 'type', title: '类型', width: 80,
    slots: {
      default: ({ row }) => {
        const t = typeMap[row.type];
        return h(Tag, { color: t?.color || 'default' }, () => t?.label || row.type);
      },
    },
  },
  { field: 'permission', title: '权限标识', minWidth: 180 },
  { field: 'description', title: '描述', minWidth: 150 },
  { field: 'path', title: '路径', minWidth: 160 },
  { field: 'component', title: '组件', minWidth: 160 },
  { field: 'icon', title: '图标', width: 90 },
  { field: 'sort', title: '排序', width: 70 },
  { field: 'isEnabled', title: '状态', width: 70, cellRender: { name: 'CellTag' } },
  { field: 'isVisible', title: '可见', width: 70,
    formatter: ({ cellValue }) => cellValue ? '是' : '否',
  },
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
          // 使用扁平数据，让 vxe-table 通过 treeConfig.transform 自动构建树
          const list = await getAllMenusApi();
          return { items: list || [], total: list?.length || 0 };
        },
      },
    },
    rowConfig: { keyField: 'id' },
    treeConfig: { parentField: 'parentId', rowField: 'id', transform: true, expandAll: true, indent: 20 },
    // 菜单为树形结构，一次性加载所有节点构建树，禁用分页器
    pagerConfig: { enabled: false },
    toolbarConfig: { custom: true, refresh: true, zoom: true },
  } as VxeTableGridOptions,
});

function expandAll() { gridApi.grid?.setAllTreeExpand(true); }
function collapseAll() { gridApi.grid?.setAllTreeExpand(false); }

const formSchema: VbenFormSchema[] = [
  { component: 'Select', fieldName: 'type', label: '类型', defaultValue: 'Menu', rules: 'required',
    componentProps: { options: [{ label: '目录', value: 'Directory' }, { label: '菜单', value: 'Menu' }, { label: '按钮', value: 'Button' }] },
    help: '目录=一级分组, 菜单=页面, 按钮=操作权限',
  },
  { component: 'Input', fieldName: 'name', label: '名称', rules: 'required' },
  { component: 'Input', fieldName: 'permission', label: '权限标识',
    help: '如 system:user:list',
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Menu' || values.type === 'Button'; },
    },
  },
  { component: 'Input', fieldName: 'description', label: '描述',
    help: '按钮/权限的说明描述',
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Button'; },
    },
  },
  { component: 'Input', fieldName: 'path', label: '路由路径',
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Directory' || values.type === 'Menu'; },
    },
  },
  { component: 'Input', fieldName: 'component', label: '组件路径',
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Menu'; },
    },
  },
  { component: 'Input', fieldName: 'redirect', label: '重定向',
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Directory'; },
    },
  },
  { component: 'IconPicker', fieldName: 'icon', label: '图标',
    componentProps: {
      prefix: 'lucide',
      autoFetchApi: false,
    },
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type !== 'Button'; },
    },
  },
  { component: 'TreeSelect', fieldName: 'parentId', label: '父级菜单', defaultValue: 0,
    componentProps: { treeData: [], placeholder: '留空为顶级菜单', allowClear: true, showSearch: true, treeNodeFilterProp: 'label', treeLine: true, treeDefaultExpandAll: true, dropdownStyle: { maxHeight: '400px' }, style: { width: '100%' } },
  },
  { component: 'InputNumber', fieldName: 'sort', label: '排序', defaultValue: 0, componentProps: { style: { width: '100%' } } },
  { component: 'Switch', fieldName: 'isEnabled', label: '启用', defaultValue: true },
  { component: 'Switch', fieldName: 'isVisible', label: '菜单可见', defaultValue: true,
    help: '关闭后菜单中不显示，但路由仍可访问',
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type !== 'Button'; },
    },
  },
  { component: 'Switch', fieldName: 'isCache', label: '缓存', defaultValue: false,
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Menu'; },
    },
  },
  { component: 'Switch', fieldName: 'isExternal', label: '外链', defaultValue: false,
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Menu'; },
    },
  },
];

const [Form, formApi] = useVbenForm({ schema: formSchema, showDefaultActions: false });

// 当前编辑的菜单ID，0 表示新增
const editingId = ref(0);

const [Modal, modalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await formApi.getValues();
    if (editingId.value) {
      await updateMenuApi(editingId.value, values);
      message.success('更新成功');
    } else {
      await createMenuApi(values);
      message.success('创建成功');
    }
    modalApi.close(); gridApi.query();
  },
  async onOpenChange(isOpen) {
    if (isOpen) {
      formApi.resetForm();
      const data = modalApi.getData<Record<string, any>>();
      editingId.value = data?.id || 0;
      // 加载菜单树用于父级选择
      try {
        const menus: any[] = await getAllMenusApi() || [];
        const excludeId = data?.id;
        const treeData = buildMenuTreeSelect(menus, excludeId);
        formApi.updateSchema([{ fieldName: 'parentId', componentProps: { treeData } }]);
      } catch {
        // ignore
      }
      if (data) formApi.setValues(data);
    }
  },
});

function onCreate(parentId = 0) { modalApi.setData({ parentId }).open(); }
function onEdit(row: any) { modalApi.setData(row).open(); }
function onDelete(row: any) {
  deleteMenuApi(row.id).then(() => { message.success('删除成功'); gridApi.query(); });
}
</script>

<template>
  <Page auto-content-height>
    <Modal title="菜单管理">
      <Form />
    </Modal>
    <Grid table-title="菜单列表">
      <template #toolbar-tools>
        <Button class="mr-2" @click="expandAll">展开全部</Button>
        <Button class="mr-2" @click="collapseAll">折叠全部</Button>
        <Button v-if="hasAccessByCodes(['system:menu:create'])" type="primary" @click="onCreate(0)"><Plus class="mr-2 size-4" />新增</Button>
      </template>
      <template #action="{ row }">
        <VbenTableAction
          :actions="[
            { text: '新增', auth: 'system:menu:create', onClick: () => onCreate(row.id) },
            { text: '编辑', auth: 'system:menu:update', onClick: () => onEdit(row) },
          ]"
          :dropdown-actions="[{ text: '删除', auth: 'system:menu:delete', danger: true, popConfirm: { title: '确认删除？', confirm: () => onDelete(row) } }]"
        />
      </template>
    </Grid>
  </Page>
</template>
