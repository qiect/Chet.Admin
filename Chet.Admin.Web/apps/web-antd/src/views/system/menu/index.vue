<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page, useVbenModal } from '@vben/common-ui';
import { Pencil, Plus, Trash2 } from '@vben/icons';
import { useAccess } from '@vben/access';

import { Button, message, Tag } from 'ant-design-vue';

import { useVbenForm } from '#/adapter/form';
import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import { createMenuApi, deleteMenuApi, getAllMenusApi, updateMenuApi } from '#/api/system/menu';
import { pathToI18nKey } from '#/api/core/menu';
import { $t } from '#/locales';
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
          label: m.path ? $t(pathToI18nKey(m.path)) : m.name,
          value: m.id,
          children: children.length > 0 ? children : undefined,
        };
      });
  };
  return build(0);
}

const typeMap: Record<string, { color: string; label: string }> = {
  Directory: { color: 'processing', label: $t('system.menu.type.Directory') },
  Menu: { color: 'success', label: $t('system.menu.type.Menu') },
  Button: { color: 'warning', label: $t('system.menu.type.Button') },
};

const columns: VxeTableGridColumns = [
  { field: 'name', title: $t('system.menu.columns.name'), minWidth: 180, treeNode: true, align: 'left',
    slots: {
      default: ({ row }) => row.path ? $t(pathToI18nKey(row.path)) : row.name,
    },
  },
  { field: 'type', title: $t('system.menu.columns.type'), width: 80,
    slots: {
      default: ({ row }) => {
        const t = typeMap[row.type];
        return h(Tag, { color: t?.color || 'default' }, () => t?.label || row.type);
      },
    },
  },
  { field: 'permission', title: $t('system.menu.columns.permission'), minWidth: 180 },
  { field: 'description', title: $t('system.common.columns.description'), minWidth: 150 },
  { field: 'path', title: $t('system.menu.columns.path'), minWidth: 160 },
  { field: 'component', title: $t('system.menu.columns.component'), minWidth: 160 },
  { field: 'icon', title: $t('system.menu.columns.icon'), width: 90 },
  { field: 'sort', title: $t('system.common.columns.sort'), width: 70 },
  { field: 'isEnabled', title: $t('system.common.columns.isEnabled'), width: 70, cellRender: { name: 'CellTag' } },
  { field: 'isVisible', title: $t('system.menu.columns.isVisible'), width: 70,
    formatter: ({ cellValue }) => cellValue ? $t('system.common.yes') : $t('system.common.no'),
  },
  { align: 'center', field: 'operation', fixed: 'right', slots: { default: 'action' }, title: $t('system.common.columns.operation'), width: 120 },
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
  { component: 'Select', fieldName: 'type', label: $t('system.menu.form.type'), defaultValue: 'Menu', rules: 'required',
    componentProps: { options: [{ label: $t('system.menu.type.Directory'), value: 'Directory' }, { label: $t('system.menu.type.Menu'), value: 'Menu' }, { label: $t('system.menu.type.Button'), value: 'Button' }] },
    help: $t('system.menu.form.typeHelp'),
  },
  { component: 'Input', fieldName: 'name', label: $t('system.menu.form.name'), rules: 'required' },
  { component: 'Input', fieldName: 'permission', label: $t('system.menu.form.permission'),
    help: $t('system.menu.form.permissionHelp'),
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Menu' || values.type === 'Button'; },
    },
  },
  { component: 'Input', fieldName: 'description', label: $t('system.common.form.description'),
    help: $t('system.menu.form.descriptionHelp'),
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Button'; },
    },
  },
  { component: 'Input', fieldName: 'path', label: $t('system.menu.form.path'),
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Directory' || values.type === 'Menu'; },
    },
  },
  { component: 'Input', fieldName: 'component', label: $t('system.menu.form.component'),
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Menu'; },
    },
  },
  { component: 'Input', fieldName: 'redirect', label: $t('system.menu.form.redirect'),
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Directory'; },
    },
  },
  { component: 'IconPicker', fieldName: 'icon', label: $t('system.menu.form.icon'),
    componentProps: {
      prefix: 'lucide',
      autoFetchApi: false,
    },
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type !== 'Button'; },
    },
  },
  { component: 'TreeSelect', fieldName: 'parentId', label: $t('system.menu.form.parentId'), defaultValue: 0,
    componentProps: { treeData: [], placeholder: $t('system.menu.form.parentIdPlaceholder'), allowClear: true, showSearch: true, treeNodeFilterProp: 'label', treeLine: true, treeDefaultExpandAll: true, dropdownStyle: { maxHeight: '400px' }, style: { width: '100%' } },
  },
  { component: 'InputNumber', fieldName: 'sort', label: $t('system.common.form.sort'), defaultValue: 0, componentProps: { style: { width: '100%' } } },
  { component: 'Switch', fieldName: 'isEnabled', label: $t('system.common.form.isEnabled'), defaultValue: true },
  { component: 'Switch', fieldName: 'isVisible', label: $t('system.menu.form.isVisible'), defaultValue: true,
    help: $t('system.menu.form.isVisibleHelp'),
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type !== 'Button'; },
    },
  },
  { component: 'Switch', fieldName: 'isCache', label: $t('system.menu.form.isCache'), defaultValue: false,
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Menu'; },
    },
  },
  { component: 'Switch', fieldName: 'isExternal', label: $t('system.menu.form.isExternal'), defaultValue: false,
    dependencies: {
      triggerFields: ['type'],
      if(values) { return values.type === 'Menu'; },
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

// 当前编辑的菜单ID，0 表示新增
const editingId = ref(0);

const [Modal, modalApi] = useVbenModal({
  draggable: true,
  onConfirm: async () => {
    const values = await formApi.getValues();
    if (editingId.value) {
      await updateMenuApi(editingId.value, values);
      message.success($t('system.common.messages.updateSuccess'));
    } else {
      await createMenuApi(values);
      message.success($t('system.common.messages.createSuccess'));
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
  deleteMenuApi(row.id).then(() => { message.success($t('system.common.messages.deleteSuccess')); gridApi.query(); });
}
</script>

<template>
  <Page auto-content-height>
    <Modal :title="$t('system.menu.title')">
      <Form />
    </Modal>
    <Grid :table-title="$t('system.menu.tableTitle')">
      <template #toolbar-tools>
        <Button class="mr-2" @click="expandAll">{{ $t('system.common.actions.expandAll') }}</Button>
        <Button class="mr-2" @click="collapseAll">{{ $t('system.common.actions.collapseAll') }}</Button>
        <Button v-if="hasAccessByCodes(['system:menu:create'])" type="primary" @click="onCreate(0)"><Plus class="mr-2 size-4" />{{ $t('system.common.actions.create') }}</Button>
      </template>
      <template #action="{ row }">
        <VbenTableAction
          :actions="[
            { icon: Plus, auth: 'system:menu:create', tooltip: $t('system.common.actions.create'), onClick: () => onCreate(row.id) },
            { icon: Pencil, auth: 'system:menu:update', tooltip: $t('system.common.actions.edit'), onClick: () => onEdit(row) },
          ]"
          :dropdown-actions="[{ icon: Trash2, text: $t('system.common.actions.delete'), auth: 'system:menu:delete', danger: true, popConfirm: { title: $t('system.common.actions.confirmDelete'), confirm: () => onDelete(row) } }]"
        />
      </template>
    </Grid>
  </Page>
</template>
