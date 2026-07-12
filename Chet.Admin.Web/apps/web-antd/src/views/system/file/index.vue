<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page } from '@vben/common-ui';
import { Download, Plus, Trash2 } from '@vben/icons';
import { useAccess } from '@vben/access';
import { formatDateTime } from '@vben/utils';

import { Button, message, Upload } from 'ant-design-vue';
import type { UploadProps } from 'ant-design-vue';

import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import {
  deleteFileApi,
  downloadFileApi,
  getFileListApi,
  uploadFileApi,
} from '#/api/system/file';
import { $t } from '#/locales';
import { ref } from 'vue';

const { hasAccessByCodes } = useAccess();

const searchSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'keyword', label: $t('system.common.search.keyword') },
];

// 文件大小格式化
function formatFileSize(size: number) {
  if (!size) return $t('system.common.empty');
  if (size < 1024) return `${size} B`;
  if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
  return `${(size / (1024 * 1024)).toFixed(2)} MB`;
}

const columns: VxeTableGridColumns = [
  { field: 'id', title: $t('system.common.columns.id'), width: 80 },
  { field: 'fileName', title: $t('system.file.columns.fileName'), minWidth: 200 },
  {
    field: 'fileSize',
    title: $t('system.file.columns.fileSize'),
    width: 120,
    slots: { default: ({ row }) => formatFileSize(row.fileSize) },
  },
  { field: 'contentType', title: $t('system.file.columns.contentType'), width: 150 },
  {
    field: 'createdAt',
    title: $t('system.file.columns.createdAt'),
    minWidth: 180,
    slots: {
      default: ({ row }) =>
        row.createdAt ? formatDateTime(row.createdAt) : $t('system.common.empty'),
    },
  },
  {
    align: 'center',
    field: 'operation',
    fixed: 'right',
    slots: { default: 'action' },
    title: $t('system.common.columns.operation'),
    width: 100,
  },
];

const [Grid, gridApi] = useVbenVxeGrid({
  formOptions: { schema: searchSchema, submitOnChange: true },
  gridOptions: {
    columns,
    height: 'auto',
    keepSource: true,
    proxyConfig: {
      ajax: {
        query: async ({ page }, formValues) => {
          const res = await getFileListApi({
            pageNumber: page.currentPage,
            pageSize: page.pageSize,
            ...formValues,
          });
          // 后端返回 { items, total } 结构
          return {
            items: res?.items || [],
            total: res?.total || 0,
          };
        },
      },
    },
    rowConfig: { keyField: 'id' },
    toolbarConfig: { custom: true, refresh: true, search: true, zoom: true },
  } as VxeTableGridOptions,
});

const uploading = ref(false);

// 使用 customRequest 走 requestClient（自动带 Authorization header）
const customUpload: UploadProps['customRequest'] = async (options) => {
  const { file, onSuccess, onError } = options;
  uploading.value = true;
  try {
    await uploadFileApi(file as File);
    onSuccess?.({}, file);
    message.success(`${(file as File).name} ${$t('system.file.messages.uploadSuccess')}`);
    gridApi.query();
  } catch (error) {
    onError?.(error as Error);
    message.error(`${(file as File).name} ${$t('system.file.messages.uploadFailed')}`);
  } finally {
    uploading.value = false;
  }
};

async function onDownload(row: any) {
  try {
    const blob = await downloadFileApi(row.id);
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = row.fileName || `file-${row.id}`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  } catch {
    message.error($t('system.file.messages.downloadFailed'));
  }
}

function onDelete(row: any) {
  deleteFileApi(row.id).then(() => {
    message.success($t('system.common.messages.deleteSuccess'));
    gridApi.query();
  });
}
</script>

<template>
  <Page auto-content-height>
    <Grid :table-title="$t('system.file.tableTitle')">
      <template #toolbar-tools>
        <Upload
          v-if="hasAccessByCodes(['system:file:upload'])"
          :custom-request="customUpload"
          :show-upload-list="false"
          :show-button="false"
        >
          <Button type="primary" :loading="uploading">
            <Plus class="mr-2 size-4" />{{ $t('system.file.actions.upload') }}
          </Button>
        </Upload>
      </template>
      <template #action="{ row }">
        <VbenTableAction
          :actions="[
            { icon: Download, tooltip: $t('system.file.actions.download'), onClick: () => onDownload(row) },
          ]"
          :dropdown-actions="[
            { icon: Trash2, text: $t('system.common.actions.delete'), auth: 'system:file:delete', danger: true, popConfirm: { title: $t('system.common.actions.confirmDelete'), confirm: () => onDelete(row) } },
          ]"
        />
      </template>
    </Grid>
  </Page>
</template>
