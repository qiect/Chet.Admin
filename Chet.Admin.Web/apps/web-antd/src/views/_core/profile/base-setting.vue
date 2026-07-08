<script setup lang="ts">
import type { VbenFormSchema } from '#/adapter/form';

import { computed, onMounted, ref, shallowRef } from 'vue';

import { ProfileBaseSetting } from '@vben/common-ui';
import { useUserStore } from '@vben/stores';

import { message, Upload, Avatar } from 'ant-design-vue';
import { UserOutlined } from '@ant-design/icons-vue';

import { getProfileApi, updateProfileApi } from '#/api';
import { uploadFileApi } from '#/api/system/file';

const profileBaseSettingRef = ref();
const userStore = useUserStore();

// 当前头像地址
const avatarUrl = ref('');
// 上传中状态
const uploading = ref(false);
// 当前用户信息
const profileData = shallowRef<{ id: number; name: string; email: string; avatar?: string | null }>();

const formSchema = computed((): VbenFormSchema[] => {
  return [
    {
      fieldName: 'realName',
      component: 'Input',
      label: '姓名',
      rules: 'required',
    },
    {
      fieldName: 'username',
      component: 'Input',
      label: '邮箱',
      componentProps: { disabled: true },
      help: '邮箱为登录唯一凭证，不可修改',
    },
    {
      fieldName: 'introduction',
      component: 'Textarea',
      label: '个人简介',
    },
  ];
});

onMounted(async () => {
  const data = await getProfileApi();
  profileData.value = data;
  avatarUrl.value = data?.avatar || '';
  profileBaseSettingRef.value.getFormApi().setValues({
    realName: data.name,
    username: data.email,
    introduction: data.introduction,
  });
});

// 上传前校验
function beforeUpload(file: File) {
  const isImage = /^image\/(jpeg|png|gif|webp|bmp)$/i.test(file.type);
  if (!isImage) {
    message.error('只能上传 JPG/PNG/GIF/WEBP/BMP 格式的图片');
    return false;
  }
  const isLt2M = file.size / 1024 / 1024 < 2;
  if (!isLt2M) {
    message.error('头像图片大小不能超过 2MB');
    return false;
  }
  return false; // 返回 false 阻止默认上传行为，改用手动上传
}

// 自定义上传
async function handleCustomUpload(options: { file: File }) {
  const { file } = options;
  uploading.value = true;
  try {
    const res: any = await uploadFileApi(file);
    // 后端返回 { filePath: 'uploads/xxx.jpg' }
    const filePath = res?.filePath || res?.data?.filePath;
    if (!filePath) {
      message.error('头像上传失败：未获取到文件地址');
      return;
    }
    // 更新个人资料中的头像字段
    await updateProfileApi({ avatar: filePath });
    avatarUrl.value = filePath;
    // 同步更新 userStore 中的头像，使顶部导航立即刷新
    if (userStore.userInfo) {
      userStore.setUserInfo({ ...userStore.userInfo, avatar: filePath });
    }
    message.success('头像更新成功');
  } catch (error) {
    message.error('头像上传失败');
  } finally {
    uploading.value = false;
  }
}

// 保存基本信息（姓名）
async function handleSubmit(values: Record<string, any>) {
  try {
    await updateProfileApi({ name: values.realName });
    message.success('个人信息更新成功');
  } catch {
    message.error('个人信息更新失败');
  }
}
</script>
<template>
  <div class="profile-container">
    <!-- 头像上传区域 -->
    <div class="avatar-section">
      <div class="avatar-label">头像</div>
      <div class="avatar-content">
        <Avatar
          :size="96"
          :src="avatarUrl ? avatarUrl : undefined"
          class="avatar-preview"
        >
          <template #icon><UserOutlined /></template>
        </Avatar>
        <Upload
          :show-upload-list="false"
          :before-upload="beforeUpload"
          :custom-request="handleCustomUpload"
          accept="image/jpeg,image/png,image/gif,image/webp,image/bmp"
        >
          <a-button type="primary" :loading="uploading" ghost>
            {{ uploading ? '上传中...' : '更换头像' }}
          </a-button>
        </Upload>
        <div class="avatar-tip">支持 JPG/PNG/GIF/WEBP/BMP，大小不超过 2MB</div>
      </div>
    </div>

    <!-- 基本信息表单（姓名、邮箱、简介） -->
    <ProfileBaseSetting
      ref="profileBaseSettingRef"
      :form-schema="formSchema"
      @submit="handleSubmit"
    />
  </div>
</template>

<style scoped>
.profile-container {
  margin-bottom: 16px;
}

.avatar-section {
  display: flex;
  align-items: flex-start;
  margin-bottom: 24px;
}

.avatar-label {
  width: 100px;
  text-align: right;
  padding-right: 12px;
  color: rgba(0, 0, 0, 0.85);
  line-height: 96px;
}

.avatar-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}

.avatar-preview {
  background-color: #f0f0f0;
}

.avatar-tip {
  color: rgba(0, 0, 0, 0.45);
  font-size: 12px;
}
</style>
