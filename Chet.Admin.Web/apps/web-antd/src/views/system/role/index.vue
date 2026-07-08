<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page, useVbenModal } from '@vben/common-ui';
import { Plus } from '@vben/icons';
import { useAccess } from '@vben/access';

import { Button, message, Tree, Spin, Alert } from 'ant-design-vue';

import { useVbenForm } from '#/adapter/form';
import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import {
  assignRolePermissionsApi,
  assignRoleMenusApi,
  createRoleApi,
  deleteRoleApi,
  getPermissionAllApi,
  getRoleListApi,
  getRoleMenusApi,
  getRolePermissionsApi,
  updateRoleApi,
  updateDataScopeApi,
} from '#/api/system/role';
import { getMenuTreeApi } from '#/api/system/menu';
import { getDeptTreeApi } from '#/api/system/department';
import { ref, watch } from 'vue';

const { hasAccessByCodes } = useAccess();

const searchSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'keyword', label: '关键字' },
];

const dataScopeOptions = [
  { color: 'success', label: '全部数据', value: 'All' },
  { color: 'processing', label: '本部门', value: 'Dept' },
  { color: 'cyan', label: '本部门及下级', value: 'DeptAndChild' },
  { color: 'warning', label: '仅本人', value: 'Self' },
  { color: 'purple', label: '自定义', value: 'Custom' },
];

const columns: VxeTableGridColumns = [
  { field: 'id', title: 'ID', width: 80 },
  { field: 'code', title: '角色编码', minWidth: 120 },
  { field: 'name', title: '角色名称', minWidth: 150 },
  { field: 'description', title: '描述', minWidth: 200 },
  { field: 'dataScope', title: '数据权限', width: 130, cellRender: { name: 'CellTag', options: dataScopeOptions } },
  { field: 'sort', title: '排序', width: 80 },
  { field: 'isEnabled', title: '状态', width: 80, cellRender: { name: 'CellTag' } },
  { field: 'createdAt', title: '创建时间', minWidth: 180,
    slots: { default: ({ row }) => row.createdAt ? new Date(row.createdAt).toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' }) : '-' },
  },
  { align: 'center', field: 'operation', fixed: 'right', slots: { default: 'action' }, title: '操作', width: 200 },
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
  { component: 'Input', fieldName: 'code', label: '角色编码', rules: 'required' },
  { component: 'Input', fieldName: 'name', label: '角色名称', rules: 'required' },
  { component: 'Textarea', fieldName: 'description', label: '描述' },
  { component: 'InputNumber', fieldName: 'sort', label: '排序', defaultValue: 0, componentProps: { style: { width: '100%' } } },
  { component: 'Switch', fieldName: 'isEnabled', label: '启用', defaultValue: true },
  {
    component: 'Select',
    fieldName: 'dataScope',
    label: '数据权限',
    defaultValue: 'Self',
    componentProps: {
      options: [
        { label: '全部数据', value: 'All' },
        { label: '本部门数据', value: 'Dept' },
        { label: '本部门及下级部门', value: 'DeptAndChild' },
        { label: '仅本人数据', value: 'Self' },
        { label: '自定义部门', value: 'Custom' },
      ],
      placeholder: '选择数据权限',
      style: { width: '100%' },
    },
  },
  {
    component: 'TreeSelect',
    fieldName: 'customDeptIds',
    label: '自定义部门',
    dependencies: {
      triggerFields: ['dataScope'],
      if(values) { return values.dataScope === 'Custom'; },
    },
    componentProps: {
      treeData: [],
      treeCheckable: true,
      showCheckedStrategy: 'SHOW_ALL',
      placeholder: '选择部门',
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
      message.success('更新成功');
    } else {
      await createRoleApi(values);
      message.success('创建成功');
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

// ========== 统一权限分配 ==========
interface TreeNode {
  key: string;
  title: string;
  value: string;
  children?: TreeNode[];
  selectable?: boolean;
  disableCheckbox?: boolean;
}

const assignLoading = ref(false);
const assignRoleId = ref(0);
const treeData = ref<TreeNode[]>([]);
const checkedKeys = ref<string[]>([]);
const halfCheckedKeys = ref<string[]>([]);
// 存储菜单key和权限key的映射
const menuKeySet = ref<Set<string>>(new Set());
const permKeySet = ref<Set<string>>(new Set());

// 构建统一树：菜单为父节点，权限为子节点
function buildUnifiedTree(menus: any[], permissions: any[]): TreeNode[] {
  const permMap = new Map<number, any[]>();
  for (const perm of permissions) {
    const menuId = perm.menuId ?? 0;
    if (!permMap.has(menuId)) permMap.set(menuId, []);
    permMap.get(menuId)!.push(perm);
  }

  menuKeySet.value.clear();
  permKeySet.value.clear();

  function buildNode(menu: any): TreeNode {
    const menuKey = `menu-${menu.id}`;
    menuKeySet.value.add(menuKey);

    const children: TreeNode[] = [];

    // 添加该菜单下的权限作为子节点
    const menuPerms = permMap.get(menu.id) || [];
    for (const perm of menuPerms) {
      const permKey = `perm-${perm.id}`;
      permKeySet.value.add(permKey);
      children.push({
        key: permKey,
        title: `${perm.name} [${perm.code}]`,
        value: permKey,
        selectable: false,
      });
    }

    // 递归处理子菜单
    if (menu.children && menu.children.length > 0) {
      for (const child of menu.children) {
        children.push(buildNode(child));
      }
    }

    return {
      key: menuKey,
      title: menu.type === 'Button' ? `${menu.name} [按钮]` : menu.name,
      value: menuKey,
      children: children.length > 0 ? children : undefined,
      selectable: false,
    };
  }

  return menus.map(m => buildNode(m));
}

// 从checkedKeys中提取菜单ID和权限ID
function extractIds(keys: string[], halfKeys: string[]) {
  const menuIds: number[] = [];
  const permissionIds: number[] = [];

  for (const key of [...keys, ...halfKeys]) {
    if (key.startsWith('menu-')) {
      menuIds.push(Number(key.replace('menu-', '')));
    }
  }
  for (const key of keys) {
    if (key.startsWith('perm-')) {
      permissionIds.push(Number(key.replace('perm-', '')));
    }
  }

  return { menuIds, permissionIds };
}

// 从当前角色的菜单ID和权限ID生成checkedKeys
function buildCheckedKeys(menuIds: number[], permissionIds: number[]): string[] {
  const keys: string[] = [];
  for (const id of menuIds) {
    keys.push(`menu-${id}`);
  }
  for (const id of permissionIds) {
    keys.push(`perm-${id}`);
  }
  return keys;
}

const [AssignModal, assignModalApi] = useVbenModal({
  onConfirm: async () => {
    const { menuIds, permissionIds } = extractIds(checkedKeys.value, halfCheckedKeys.value);
    await Promise.all([
      assignRoleMenusApi(assignRoleId.value, menuIds),
      assignRolePermissionsApi(assignRoleId.value, permissionIds),
    ]);
    message.success('权限分配成功');
    assignModalApi.close();
  },
  async onOpenChange(isOpen) {
    if (isOpen) {
      const data = assignModalApi.getData<any>();
      assignRoleId.value = data.roleId;
      assignLoading.value = true;
      try {
        const [menus, roleMenuList, rolePermList, allPerms] = await Promise.all([
          getMenuTreeApi(),
          getRoleMenusApi(data.roleId),
          getRolePermissionsApi(data.roleId),
          getPermissionAllApi(),
        ]);

        treeData.value = buildUnifiedTree(menus || [], allPerms || []);

        const currentMenuIds = (roleMenuList || []).map((m: any) => m.id);
        const currentPermIds = (rolePermList || []).map((p: any) => p.id);
        checkedKeys.value = buildCheckedKeys(currentMenuIds, currentPermIds);
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
  deleteRoleApi(row.id).then(() => { message.success('删除成功'); gridApi.query(); });
}
</script>

<template>
  <Page auto-content-height>
    <Modal title="角色管理">
      <Form />
    </Modal>
    <AssignModal title="分配权限" class="w-[600px]">
      <Spin :spinning="assignLoading">
        <Alert type="info" show-icon class="mb-3">
          <template #message>
            <span class="text-xs">勾选菜单表示可访问该页面，勾选权限表示可执行该操作。半选状态的菜单也会被分配。</span>
          </template>
        </Alert>
        <Tree
          v-model:checkedKeys="checkedKeys"
          v-model:halfCheckedKeys="halfCheckedKeys"
          :tree-data="treeData"
          checkable
          default-expand-all
          :selectable="false"
          :field-names="{ key: 'key', title: 'title', children: 'children' }"
        />
      </Spin>
    </AssignModal>
    <Grid table-title="角色列表">
      <template #toolbar-tools>
        <Button v-if="hasAccessByCodes(['system:role:create'])" type="primary" @click="onCreate"><Plus class="mr-2 size-4" />新增</Button>
      </template>
      <template #action="{ row }">
        <VbenTableAction
          :actions="[
            { text: '编辑', auth: 'system:role:update', onClick: () => onEdit(row) },
            { text: '分配权限', auth: 'system:role:update', onClick: () => onAssign(row) },
          ]"
          :dropdown-actions="[{ text: '删除', auth: 'system:role:delete', danger: true, popConfirm: { title: '确认删除？', confirm: () => onDelete(row) } }]"
        />
      </template>
    </Grid>
  </Page>
</template>
