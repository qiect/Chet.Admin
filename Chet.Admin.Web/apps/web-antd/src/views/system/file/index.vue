<script lang="ts" setup>
import type { VbenFormSchema } from '#/adapter/form';
import type { VxeTableGridColumns, VxeTableGridOptions } from '#/adapter/vxe-table';

import { Page } from '@vben/common-ui';
import { Plus } from '@vben/icons';
import { useAccess } from '@vben/access';

import { Button, message, Upload } from 'ant-design-vue';
import type { UploadProps } from 'ant-design-vue';

import { useVbenVxeGrid, VbenTableAction } from '#/adapter/vxe-table';
import {
  deleteFileApi,
  downloadFileApi,
  getFileListApi,
  uploadFileApi,
} from '#/api/system/file';
import { ref } from 'vue';

const { hasAccessByCodes } = useAccess();

const searchSchema: VbenFormSchema[] = [
  { component: 'Input', fieldName: 'keyword', label: '关键字' },
];

// 文件大小格式化
function formatFileSize(size: number) {
  if (!size) return '-';
  if (size < 1024) return `${size} B`;
  if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
  return `${(size / (1024 * 1024)).toFixed(2)} MB`;
}

const columns: VxeTableGridColumns = [
  { field: 'id', title: 'ID', width: 80 },
  { field: 'fileName', title: '文件名', minWidth: 200 },
  {
    field: 'fileSize',
    title: '大小',
    width: 120,
    slots: { default: ({ row }) => formatFileSize(row.fileSize) },
  },
  { field: 'contentType', title: '类型', width: 150 },
  {
    field: 'createdAt',
    title: '上传时间',
    minWidth: 180,
    slots: {
      default: ({ row }) =>
        row.createdAt ? new Date(row.createdAt).toLocaleString('zh-CN') : '-',
    },
  },
  {
    align: 'center',
    field: 'operation',
    fixed: 'right',
    slots: { default: 'action' },
    title: '操作',
    width: 180,
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
    message.success(`${(file as File).name} 上传成功`);
    gridApi.query();
  } catch (error) {
    onError?.(error as Error);
    message.error(`${(file as File).name} 上传失败`);
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
    message.error('下载失败');
  }
}

function onDelete(row: any) {
  deleteFileApi(row.id).then(() => {
    message.success('删除成功');
    gridApi.query();
  });
}
</script>

<template>
  <Page auto-content-height>
    <Grid table-title="文件列表">
      <template #toolbar-tools>
        <Upload
          v-if="hasAccessByCodes(['system:file:upload'])"
          :custom-request="customUpload"
          :show-upload-list="false"
          :show-button="false"
        >
          <Button type="primary" :loading="uploading">
            <Plus class="mr-2 size-4" />上传文件
          </Button>
        </Upload>
      </template>
      <template #action="{ row }">
        <VbenTableAction
          :actions="[
            { text: '下载', onClick: () => onDownload(row) },
          ]"
          :dropdown-actions="[
            { text: '删除', auth: 'system:file:delete', danger: true, popConfirm: { title: '确认删除？', confirm: () => onDelete(row) } },
          ]"
        />
      </template>
    </Grid>
  </Page>
</template>
