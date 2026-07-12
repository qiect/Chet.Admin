<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page, useVbenModal } from '@vben/common-ui';
import { Plus } from '@vben/icons';
import { useAccess } from '@vben/access';
import { formatDateTime } from '@vben/utils';

import { Button, message, Tree, Spin, Alert } from 'ant-design-vue';

import { useVbenForm } from '#/adapter/form';
import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import {
  assignRoleMenusApi,
  createRoleApi,
  deleteRoleApi,
  getRoleListApi,
  getRoleMenusApi,
  updateRoleApi,
  updateDataScopeApi,
} from '#/api/system/role';
import { getMenuTreeApi } from '#/api/system/menu';
import { getDeptTreeApi } from '#/api/system/department';
import { $t } from '#/locales';
import { ref, watch } from 'vue';

const { hasAccessByCodes } = useAccess();

const searchSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'keyword', label: $t('system.common.search.keyword') },
];

const dataScopeOptions = [
  { color: 'success', label: $t('system.role.dataScope.All'), value: 'All' },
  { color: 'processing', label: $t('system.role.dataScope.Dept'), value: 'Dept' },
  { color: 'cyan', label: $t('system.role.dataScope.DeptAndChild'), value: 'DeptAndChild' },
  { color: 'warning', label: $t('system.role.dataScope.Self'), value: 'Self' },
  { color: 'purple', label: $t('system.role.dataScope.Custom'), value: 'Custom' },
];

const columns: VxeTableGridColumns = [
  { field: 'id', title: $t('system.common.columns.id'), width: 80 },
  { field: 'code', title: $t('system.role.columns.code'), minWidth: 120 },
  { field: 'name', title: $t('system.role.columns.name'), minWidth: 150 },
  { field: 'description', title: $t('system.common.columns.description'), minWidth: 200 },
  { field: 'dataScope', title: $t('system.role.columns.dataScope'), width: 130, cellRender: { name: 'CellTag', options: dataScopeOptions } },
  { field: 'sort', title: $t('system.common.columns.sort'), width: 80 },
  { field: 'isEnabled', title: $t('system.common.columns.isEnabled'), width: 80, cellRender: { name: 'CellTag' } },
  { field: 'createdAt', title: $t('system.common.columns.createdAt'), minWidth: 180,
    slots: { default: ({ row }) => row.createdAt ? formatDateTime(row.createdAt) : $t('system.common.empty') },
  },
  { align: 'center', field: 'operation', fixed: 'right', slots: { default: 'action' }, title: $t('system.common.columns.operation'), width: 200 },
];

const [Grid, gridApi] = useVbenVxeGrid({
  formOptions: { schema: searchSchema, submitOnChange: true },
  gridOptions: {
    columns,
    height: 'auto',
    keepSource: true,
    proxyConfig: {
      ajax: { query: async ({ page }, formValues) => await getRoleListApi({ pageNumber: page.currentPage, pageSize: page.pageSize, ...formValues }) },
    },
    rowConfig: { keyField: 'id' },
    toolbarConfig: { custom: true, refresh: true, search: true, zoom: true },
  } as VxeTableGridOptions,
});

// ========== 角色编辑 ==========
const deptTreeData = ref<any[]>([]);

function buildDeptTreeSelectData(depts: any[]): any[] {
  return (depts || []).map((d: any) => ({
    id: d.id,
    value: d.id,
    title: d.name,
    key: d.id,
    children: d.children ? buildDeptTreeSelectData(d.children) : undefined,
  }));
}

const formSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'code', label: $t('system.role.form.code'), rules: 'required' },
  { component: 'Input', fieldName: 'name', label: $t('system.role.form.name'), rules: 'required' },
  { component: 'Textarea', fieldName: 'description', label: $t('system.common.form.description') },
  { component: 'InputNumber', fieldName: 'sort', label: $t('system.common.form.sort'), defaultValue: 0, componentProps: { style: { width: '100%' } } },
  { component: 'Switch', fieldName: 'isEnabled', label: $t('system.common.form.isEnabled'), defaultValue: true },
  {
    component: 'Select',
    fieldName: 'dataScope',
    label: $t('system.role.form.dataScope'),
    defaultValue: 'Self',
    componentProps: {
      options: [
        { label: $t('system.role.dataScopeForm.All'), value: 'All' },
        { label: $t('system.role.dataScopeForm.Dept'), value: 'Dept' },
        { label: $t('system.role.dataScopeForm.DeptAndChild'), value: 'DeptAndChild' },
        { label: $t('system.role.dataScopeForm.Self'), value: 'Self' },
        { label: $t('system.role.dataScopeForm.Custom'), value: 'Custom' },
      ],
      placeholder: $t('system.role.form.dataScopePlaceholder'),
      style: { width: '100%' },
    },
  },
  {
    component: 'TreeSelect',
    fieldName: 'customDeptIds',
    label: $t('system.role.form.customDeptIds'),
    dependencies: {
      triggerFields: ['dataScope'],
      if(values) { return values.dataScope === 'Custom'; },
    },
    componentProps: {
      treeData: [],
      treeCheckable: true,
      showCheckedStrategy: 'SHOW_ALL',
      placeholder: $t('system.role.form.customDeptIdsPlaceholder'),
      allowClear: true,
      showSearch: true,
      treeNodeFilterProp: 'label',
      treeLine: true,
      treeDefaultExpandAll: true,
      dropdownStyle: { maxHeight: '400px' },
      style: { width: '100%' },
    },
  },
];

const [Form, formApi] = useVbenForm({ schema: formSchema, showDefaultActions: false });

const [Modal, modalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await formApi.getValues();
    const id = values.id;
    if (id) {
      await updateRoleApi(id, values);
      // 提交数据权限
      if (values.dataScope) {
        const dataScopeData: { dataScope: string; customDeptIds?: number[] } = { dataScope: values.dataScope };
        if (values.dataScope === 'Custom' && values.customDeptIds) {
          dataScopeData.customDeptIds = values.customDeptIds;
        }
        await updateDataScopeApi(id, dataScopeData);
      }
      message.success($t('system.common.messages.updateSuccess'));
    } else {
      await createRoleApi(values);
      message.success($t('system.common.messages.createSuccess'));
    }
    modalApi.close(); gridApi.query();
  },
  async onOpenChange(isOpen) {
    if (isOpen) {
      formApi.resetForm();
      const data = modalApi.getData<Record<string, any>>();
      if (data) formApi.setValues(data);
      // 加载部门树数据
      try {
        const deptTree = await getDeptTreeApi();
        deptTreeData.value = buildDeptTreeSelectData(deptTree || []);
        formApi.updateSchema([{
          fieldName: 'customDeptIds',
          componentProps: { treeData: deptTreeData.value },
        }]);
      } catch {
        // 部门树加载失败不阻塞表单
      }
    }
  },
});

// ========== 菜单分配 ==========
interface TreeNode {
  key: number;
  title: string;
  value: number;
  children?: TreeNode[];
  selectable?: boolean;
  disableCheckbox?: boolean;
}

const assignLoading = ref(false);
const assignRoleId = ref(0);
const treeData = ref<TreeNode[]>([]);
const checkedKeys = ref<number[]>([]);

// 构建菜单树数据
function buildMenuTree(menus: any[]): TreeNode[] {
  return (menus || []).map((m: any) => ({
    key: m.id,
    title: m.type === 'Button' ? `${m.name} ${$t('system.role.buttonSuffix')}` : m.name,
    value: m.id,
    children: m.children && m.children.length > 0 ? buildMenuTree(m.children) : undefined,
    selectable: false,
  }));
}

const [AssignModal, assignModalApi] = useVbenModal({
  onConfirm: async () => {
    // checkStrictly 模式下父子不联动，checkedKeys 即为精确的已勾选菜单ID
    // 注意：checkStrictly 为 true 时，Tree 的 v-model:checkedKeys 会被组件改写为
    // { checked: number[], halfChecked: number[] } 对象，不再是最初的 number[]
    const val = checkedKeys.value as
      | number[]
      | { checked: number[]; halfChecked: number[] };
    const menuIds = Array.isArray(val) ? val : (val?.checked ?? []);
    await assignRoleMenusApi(assignRoleId.value, menuIds);
    message.success($t('system.role.messages.assignMenuSuccess'));
    assignModalApi.close();
  },
  async onOpenChange(isOpen) {
    if (isOpen) {
      const data = assignModalApi.getData<any>();
      assignRoleId.value = data.roleId;
      assignLoading.value = true;
      try {
        const [menus, roleMenuList] = await Promise.all([
          getMenuTreeApi(),
          getRoleMenusApi(data.roleId),
        ]);

        treeData.value = buildMenuTree(menus || []);

        // 回显已分配的菜单
        checkedKeys.value = (roleMenuList || []).map((m: any) => m.id);
      } finally {
        assignLoading.value = false;
      }
    }
  },
});

async function onAssign(row: any) {
  assignModalApi.setData({ roleId: row.id }).open();
}

function onCreate() { modalApi.setData({}).open(); }
function onEdit(row: any) { modalApi.setData(row).open(); }
function onDelete(row: any) {
  deleteRoleApi(row.id).then(() => { message.success($t('system.common.messages.deleteSuccess')); gridApi.query(); });
}
</script>

<template>
  <Page auto-content-height>
    <Modal :title="$t('system.role.modals.edit')">
      <Form />
    </Modal>
    <AssignModal :title="$t('system.role.modals.assignMenu')" class="w-[600px]">
      <Spin :spinning="assignLoading">
        <Alert type="info" show-icon class="mb-3">
          <template #message>
            <span class="text-xs">{{ $t('system.role.modals.assignMenuAlert') }}</span>
          </template>
        </Alert>
        <Tree
          v-model:checkedKeys="checkedKeys"
          :tree-data="treeData"
          checkable
          :check-strictly="true"
          default-expand-all
          :selectable="false"
          :field-names="{ key: 'key', title: 'title', children: 'children' }"
        />
      </Spin>
    </AssignModal>
    <Grid :table-title="$t('system.role.tableTitle')">
      <template #toolbar-tools>
        <Button v-if="hasAccessByCodes(['system:role:create'])" type="primary" @click="onCreate"><Plus class="mr-2 size-4" />{{ $t('system.common.actions.create') }}</Button>
      </template>
      <template #action="{ row }">
        <VbenTableAction
          :actions="[
            { text: $t('system.common.actions.edit'), auth: 'system:role:update', onClick: () => onEdit(row) },
            { text: $t('system.role.actions.assignMenu'), auth: 'system:role:update', onClick: () => onAssign(row) },
          ]"
          :dropdown-actions="[{ text: $t('system.common.actions.delete'), auth: 'system:role:delete', danger: true, popConfirm: { title: $t('system.common.actions.confirmDelete'), confirm: () => onDelete(row) } }]"
        />
      </template>
    </Grid>
  </Page>
</template>
