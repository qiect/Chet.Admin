<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page, useVbenModal } from '@vben/common-ui';
import { Plus } from '@vben/icons';
import { useAccess } from '@vben/access';

import { Button, message } from 'ant-design-vue';

import { useVbenForm } from '#/adapter/form';
import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import { createUserApi, deleteUserApi, getUserListApi, updateUserApi, getRoleListAllApi } from '#/api/system/user';
import { getDeptTreeApi } from '#/api/system/department';
import { h, onMounted, ref } from 'vue';
import { Tag } from 'ant-design-vue';

const { hasAccessByCodes } = useAccess();

// 部门名称映射
const deptNameMap = ref<Map<number, string>>(new Map());

async function loadDeptNameMap() {
  try {
    const tree: any[] = await getDeptTreeApi() || [];
    const map = new Map<number, string>();
    function collect(items: any[]) {
      for (const item of items) { map.set(item.id, item.name); if (item.children) collect(item.children); }
    }
    collect(tree);
    deptNameMap.value = map;
    return tree;
  } catch { return []; }
}

function buildDeptTreeSelect(items: any[]): any[] {
  return items.map((item: any) => ({
    label: item.name, value: item.id,
    children: item.children ? buildDeptTreeSelect(item.children) : undefined,
  }));
}

onMounted(() => { loadDeptNameMap(); });

const searchSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'keyword', label: '关键字' },
];

const columns: VxeTableGridColumns = [
  { field: 'id', title: 'ID', width: 70 },
  { field: 'name', title: '用户名', minWidth: 120 },
  { field: 'email', title: '邮箱', minWidth: 200 },
  { field: 'departmentId', title: '部门', minWidth: 120,
    slots: { default: ({ row }) => { const name = deptNameMap.value.get(row.departmentId); return name || '-'; } },
  },
  { field: 'roles', title: '角色', minWidth: 160,
    slots: {
      default: ({ row }) => {
        const roles = row.roles || [];
        if (!roles.length) return '-';
        return roles.map((r: any) => h(Tag, { color: 'blue', class: 'mr-1' }, () => r.name));
      },
    },
  },
  { field: 'createdAt', title: '创建时间', minWidth: 180,
    slots: { default: ({ row }) => row.createdAt ? new Date(row.createdAt).toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' }) : '-' },
  },
  { align: 'center', field: 'operation', fixed: 'right', slots: { default: 'action' }, title: '操作', width: 180 },
];

const [Grid, gridApi] = useVbenVxeGrid({
  formOptions: { schema: searchSchema, submitOnChange: true },
  gridOptions: {
    columns, height: 'auto', keepSource: true,
    proxyConfig: {
      ajax: {
        query: async ({ page }, formValues) => {
          return await getUserListApi({ pageNumber: page.currentPage, pageSize: page.pageSize, ...formValues });
        },
      },
    },
    rowConfig: { keyField: 'id' },
    toolbarConfig: { custom: true, refresh: true, search: true, zoom: true },
  } as VxeTableGridOptions,
});

// ========== 编辑用户表单 ==========
// 注意：邮箱是用户唯一凭证，编辑时不可修改
const editFormSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'name', label: '用户名', rules: 'required' },
  { component: 'Input', fieldName: 'email', label: '邮箱', rules: 'required',
    componentProps: { disabled: true, placeholder: '邮箱为唯一凭证，不可修改' },
    help: '邮箱为用户唯一登录凭证，不支持修改',
  },
  { component: 'TreeSelect', fieldName: 'departmentId', label: '所属部门',
    componentProps: { treeData: [], placeholder: '选择部门', allowClear: true, showSearch: true, treeNodeFilterProp: 'label', treeLine: true, treeDefaultExpandAll: true, dropdownStyle: { maxHeight: '400px' }, style: { width: '100%' } },
  },
  { component: 'Select', fieldName: 'roleIds', label: '角色',
    componentProps: { mode: 'multiple', options: [], placeholder: '选择角色', allowClear: true, style: { width: '100%' } },
  },
];

const [EditForm, editFormApi] = useVbenForm({ schema: editFormSchema, showDefaultActions: false });
const isEdit = ref(false);
const editingId = ref(0);

const [EditModal, editModalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await editFormApi.getValues();

    if (isEdit.value && editingId.value) {
      // 编辑时不提交 email 字段（邮箱为唯一凭证不可修改）
      await updateUserApi(editingId.value, { name: values.name, departmentId: values.departmentId, roleIds: values.roleIds });
      message.success('更新成功');
    } else {
      await createUserApi(values);
      message.success('创建成功');
    }
    editModalApi.close();
    gridApi.query();
  },
  async onOpenChange(isOpen) {
    if (isOpen) {
      editFormApi.resetForm();
      const [deptTree, roles] = await Promise.all([loadDeptNameMap(), getRoleListAllApi()]);
      const data = editModalApi.getData<Record<string, any>>();
      isEdit.value = !!data?.id;
      if (data?.id) editingId.value = data.id;

      editFormApi.updateSchema([
        { fieldName: 'departmentId', componentProps: { treeData: buildDeptTreeSelect(deptTree || []) } },
        { fieldName: 'roleIds', componentProps: { options: (roles || []).map((r: any) => ({ label: r.name, value: r.id })) } },
      ]);
      if (data) {
        // 后端返回 roles 数组（{id, name}），表单字段是 roleIds，需要转换
        const roleIds = Array.isArray(data.roles) ? data.roles.map((r: any) => r.id) : [];
        editFormApi.setValues({
          name: data.name,
          email: data.email,
          departmentId: data.departmentId,
          roleIds,
        });
      }
    }
  },
});

// ========== 新增用户表单（含密码） ==========
const createFormSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'name', label: '用户名', rules: 'required' },
  { component: 'Input', fieldName: 'email', label: '邮箱', rules: 'required' },
  { component: 'VbenInputPassword', fieldName: 'password', label: '密码', rules: 'required',
    componentProps: { placeholder: '请输入密码', passwordStrength: true },
  },
  { component: 'VbenInputPassword', fieldName: 'confirmPassword', label: '确认密码', rules: 'required',
    componentProps: { placeholder: '再次输入密码', passwordStrength: true },
  },
  { component: 'TreeSelect', fieldName: 'departmentId', label: '所属部门',
    componentProps: { treeData: [], placeholder: '选择部门', allowClear: true, showSearch: true, treeNodeFilterProp: 'label', treeLine: true, treeDefaultExpandAll: true, dropdownStyle: { maxHeight: '400px' }, style: { width: '100%' } },
  },
  { component: 'Select', fieldName: 'roleIds', label: '角色',
    componentProps: { mode: 'multiple', options: [], placeholder: '选择角色', allowClear: true, style: { width: '100%' } },
  },
];

const [CreateForm, createFormApi] = useVbenForm({ schema: createFormSchema, showDefaultActions: false });

const [CreateModal, createModalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await createFormApi.getValues();
    if (!values.password || values.password.length < 6) {
      message.warning('密码至少6位'); return;
    }
    if (values.password !== values.confirmPassword) {
      message.warning('两次密码不一致'); return;
    }
    const { confirmPassword, ...submitData } = values;
    await createUserApi(submitData);
    message.success('创建成功');
    createModalApi.close();
    gridApi.query();
  },
  async onOpenChange(isOpen) {
    if (isOpen) {
      createFormApi.resetForm();
      const [deptTree, roles] = await Promise.all([loadDeptNameMap(), getRoleListAllApi()]);
      createFormApi.updateSchema([
        { fieldName: 'departmentId', componentProps: { treeData: buildDeptTreeSelect(deptTree || []) } },
        { fieldName: 'roleIds', componentProps: { options: (roles || []).map((r: any) => ({ label: r.name, value: r.id })) } },
      ]);
    }
  },
});

// ========== 修改密码 ==========
const pwdFormSchema: VbenFormSchema[] = [
  { component: 'VbenInputPassword', fieldName: 'newPassword', label: '新密码', rules: 'required',
    componentProps: { placeholder: '请输入新密码', passwordStrength: true },
  },
  { component: 'VbenInputPassword', fieldName: 'confirmPassword', label: '确认密码', rules: 'required',
    componentProps: { placeholder: '再次输入新密码', passwordStrength: true },
  },
];

const [PwdForm, pwdFormApi] = useVbenForm({ schema: pwdFormSchema, showDefaultActions: false });
const pwdUserId = ref(0);

const [PwdModal, pwdModalApi] = useVbenModal({
  onConfirm: async () => {
    const values = await pwdFormApi.getValues();
    if (!values.newPassword || values.newPassword.length < 6) {
      message.warning('密码至少6位'); return;
    }
    if (values.newPassword !== values.confirmPassword) {
      message.warning('两次密码不一致'); return;
    }
    await updateUserApi(pwdUserId.value, { password: values.newPassword });
    message.success('密码修改成功');
    pwdModalApi.close();
  },
  onOpenChange(isOpen) {
    if (isOpen) { pwdFormApi.resetForm(); }
  },
});

function onCreate() { createModalApi.open(); }
function onEdit(row: any) { editModalApi.setData(row).open(); }
function onChangePwd(row: any) { pwdUserId.value = row.id; pwdModalApi.open(); }
function onDelete(row: any) {
  deleteUserApi(row.id).then(() => { message.success('删除成功'); gridApi.query(); });
}
</script>

<template>
  <Page auto-content-height>
    <!-- 新增用户 -->
    <CreateModal title="新增用户">
      <CreateForm />
    </CreateModal>

    <!-- 编辑用户 -->
    <EditModal title="编辑用户">
      <EditForm />
    </EditModal>

    <!-- 修改密码 -->
    <PwdModal title="修改密码">
      <PwdForm />
    </PwdModal>

    <Grid table-title="用户列表">
      <template #toolbar-tools>
        <Button v-if="hasAccessByCodes(['system:user:create'])" type="primary" @click="onCreate">
          <Plus class="mr-2 size-4" />新增
        </Button>
      </template>
      <template #action="{ row }">
        <VbenTableAction
          :actions="[
            { text: '编辑', auth: 'system:user:update', onClick: () => onEdit(row) },
            { text: '修改密码', auth: 'system:user:update', onClick: () => onChangePwd(row) },
          ]"
          :dropdown-actions="[{ text: '删除', auth: 'system:user:delete', danger: true, popConfirm: { title: '确认删除？', confirm: () => onDelete(row) } }]"
        />
      </template>
    </Grid>
  </Page>
</template>
