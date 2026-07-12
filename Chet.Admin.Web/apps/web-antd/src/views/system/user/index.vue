<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page, useVbenModal } from '@vben/common-ui';
import { LockKeyhole, Pencil, Plus, Trash2 } from '@vben/icons';
import { useAccess } from '@vben/access';
import { formatDateTime } from '@vben/utils';

import { Button, message } from 'ant-design-vue';

import { useVbenForm } from '#/adapter/form';
import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import { createUserApi, deleteUserApi, getUserListApi, updateUserApi, getRoleListAllApi } from '#/api/system/user';
import { getDeptTreeApi } from '#/api/system/department';
import { $t } from '#/locales';
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
  { component: 'Input', fieldName: 'keyword', label: $t('system.common.search.keyword') },
];

const columns: VxeTableGridColumns = [
  { field: 'id', title: $t('system.common.columns.id'), width: 70 },
  { field: 'name', title: $t('system.user.columns.username'), minWidth: 120 },
  { field: 'email', title: $t('system.user.columns.email'), minWidth: 200 },
  { field: 'departmentId', title: $t('system.user.columns.department'), minWidth: 120,
    slots: { default: ({ row }) => { const name = deptNameMap.value.get(row.departmentId); return name || $t('system.common.empty'); } },
  },
  { field: 'roles', title: $t('system.user.columns.roles'), minWidth: 160,
    slots: {
      default: ({ row }) => {
        const roles = row.roles || [];
        if (!roles.length) return $t('system.common.empty');
        return roles.map((r: any) => h(Tag, { color: 'blue', class: 'mr-1' }, () => r.name));
      },
    },
  },
  { field: 'createdAt', title: $t('system.common.columns.createdAt'), minWidth: 180,
    slots: { default: ({ row }) => row.createdAt ? formatDateTime(row.createdAt) : $t('system.common.empty') },
  },
  { align: 'center', field: 'operation', fixed: 'right', slots: { default: 'action' }, title: $t('system.common.columns.operation'), width: 120 },
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
  { component: 'Input', fieldName: 'name', label: $t('system.user.form.username'), rules: 'required' },
  { component: 'Input', fieldName: 'email', label: $t('system.user.form.email'), rules: 'required',
    componentProps: { disabled: true, placeholder: $t('system.user.form.emailPlaceholder') },
    help: $t('system.user.form.emailHelp'),
  },
  { component: 'TreeSelect', fieldName: 'departmentId', label: $t('system.user.form.department'),
    componentProps: { treeData: [], placeholder: $t('system.user.form.departmentPlaceholder'), allowClear: true, showSearch: true, treeNodeFilterProp: 'label', treeLine: true, treeDefaultExpandAll: true, dropdownStyle: { maxHeight: '400px' }, style: { width: '100%' } },
  },
  { component: 'Select', fieldName: 'roleIds', label: $t('system.user.form.roles'),
    componentProps: { mode: 'multiple', options: [], placeholder: $t('system.user.form.rolesPlaceholder'), allowClear: true, style: { width: '100%' } },
  },
];

const [EditForm, editFormApi] = useVbenForm({
  schema: editFormSchema,
  showDefaultActions: false,
  commonConfig: {
    labelWidth: 140, labelClass: 'whitespace-nowrap',
  },
});
const isEdit = ref(false);
const editingId = ref(0);

const [EditModal, editModalApi] = useVbenModal({
  draggable: true,
  onConfirm: async () => {
    const values = await editFormApi.getValues();

    if (isEdit.value && editingId.value) {
      // 编辑时不提交 email 字段（邮箱为唯一凭证不可修改）
      await updateUserApi(editingId.value, { name: values.name, departmentId: values.departmentId, roleIds: values.roleIds });
      message.success($t('system.common.messages.updateSuccess'));
    } else {
      await createUserApi(values);
      message.success($t('system.common.messages.createSuccess'));
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
  { component: 'Input', fieldName: 'name', label: $t('system.user.form.username'), rules: 'required' },
  { component: 'Input', fieldName: 'email', label: $t('system.user.form.email'), rules: 'required' },
  { component: 'VbenInputPassword', fieldName: 'password', label: $t('system.user.form.password'), rules: 'required',
    componentProps: { placeholder: $t('system.user.form.passwordPlaceholder'), passwordStrength: true },
  },
  { component: 'VbenInputPassword', fieldName: 'confirmPassword', label: $t('system.user.form.confirmPassword'), rules: 'required',
    componentProps: { placeholder: $t('system.user.form.confirmPasswordPlaceholder'), passwordStrength: true },
  },
  { component: 'TreeSelect', fieldName: 'departmentId', label: $t('system.user.form.department'),
    componentProps: { treeData: [], placeholder: $t('system.user.form.departmentPlaceholder'), allowClear: true, showSearch: true, treeNodeFilterProp: 'label', treeLine: true, treeDefaultExpandAll: true, dropdownStyle: { maxHeight: '400px' }, style: { width: '100%' } },
  },
  { component: 'Select', fieldName: 'roleIds', label: $t('system.user.form.roles'),
    componentProps: { mode: 'multiple', options: [], placeholder: $t('system.user.form.rolesPlaceholder'), allowClear: true, style: { width: '100%' } },
  },
];

const [CreateForm, createFormApi] = useVbenForm({
  schema: createFormSchema,
  showDefaultActions: false,
  commonConfig: {
    labelWidth: 140, labelClass: 'whitespace-nowrap',
  },
});

const [CreateModal, createModalApi] = useVbenModal({
  draggable: true,
  onConfirm: async () => {
    const values = await createFormApi.getValues();
    if (!values.password || values.password.length < 6) {
      message.warning($t('system.user.messages.passwordMinLength')); return;
    }
    if (values.password !== values.confirmPassword) {
      message.warning($t('system.user.messages.passwordMismatch')); return;
    }
    const { confirmPassword, ...submitData } = values;
    await createUserApi(submitData);
    message.success($t('system.common.messages.createSuccess'));
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
  { component: 'VbenInputPassword', fieldName: 'newPassword', label: $t('system.user.form.newPassword'), rules: 'required',
    componentProps: { placeholder: $t('system.user.form.newPasswordPlaceholder'), passwordStrength: true },
  },
  { component: 'VbenInputPassword', fieldName: 'confirmPassword', label: $t('system.user.form.confirmPassword'), rules: 'required',
    componentProps: { placeholder: $t('system.user.form.confirmNewPasswordPlaceholder'), passwordStrength: true },
  },
];

const [PwdForm, pwdFormApi] = useVbenForm({
  schema: pwdFormSchema,
  showDefaultActions: false,
  commonConfig: {
    labelWidth: 140, labelClass: 'whitespace-nowrap',
  },
});
const pwdUserId = ref(0);

const [PwdModal, pwdModalApi] = useVbenModal({
  draggable: true,
  onConfirm: async () => {
    const values = await pwdFormApi.getValues();
    if (!values.newPassword || values.newPassword.length < 6) {
      message.warning($t('system.user.messages.passwordMinLength')); return;
    }
    if (values.newPassword !== values.confirmPassword) {
      message.warning($t('system.user.messages.passwordMismatch')); return;
    }
    await updateUserApi(pwdUserId.value, { password: values.newPassword });
    message.success($t('system.user.messages.passwordChanged'));
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
  deleteUserApi(row.id).then(() => { message.success($t('system.common.messages.deleteSuccess')); gridApi.query(); });
}
</script>

<template>
  <Page auto-content-height>
    <!-- 新增用户 -->
    <CreateModal :title="$t('system.user.modals.create')">
      <CreateForm />
    </CreateModal>

    <!-- 编辑用户 -->
    <EditModal :title="$t('system.user.modals.edit')">
      <EditForm />
    </EditModal>

    <!-- 修改密码 -->
    <PwdModal :title="$t('system.user.modals.changePassword')">
      <PwdForm />
    </PwdModal>

    <Grid :table-title="$t('system.user.tableTitle')">
      <template #toolbar-tools>
        <Button v-if="hasAccessByCodes(['system:user:create'])" type="primary" @click="onCreate">
          <Plus class="mr-2 size-4" />{{ $t('system.common.actions.create') }}
        </Button>
      </template>
      <template #action="{ row }">
        <VbenTableAction
          :actions="[
            { icon: Pencil, auth: 'system:user:update', tooltip: $t('system.common.actions.edit'), onClick: () => onEdit(row) },
            { icon: LockKeyhole, auth: 'system:user:update', tooltip: $t('system.user.actions.changePassword'), onClick: () => onChangePwd(row) },
          ]"
          :dropdown-actions="[{ icon: Trash2, text: $t('system.common.actions.delete'), auth: 'system:user:delete', danger: true, popConfirm: { title: $t('system.common.actions.confirmDelete'), confirm: () => onDelete(row) } }]"
        />
      </template>
    </Grid>
  </Page>
</template>
