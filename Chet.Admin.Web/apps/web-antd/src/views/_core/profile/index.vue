<script setup lang="ts">
import { ref } from 'vue';

import { Profile } from '@vben/common-ui';
import { useUserStore } from '@vben/stores';

import ProfileBaseSetting from './base-setting.vue';
import ProfilePasswordSetting from './password-setting.vue';

const userStore = useUserStore();

// 默认显示"基本设置"
const tabsValue = ref<string>('base');

const tabs = ref([
  {
    label: '基本设置',
    value: 'base',
  },
  {
    label: '修改密码',
    value: 'password',
  },
]);
</script>

<template>
  <Profile
    v-model:model-value="tabsValue"
    title="个人中心"
    :user-info="userStore.userInfo"
    :tabs="tabs"
  >
    <template #content>
      <ProfileBaseSetting v-if="tabsValue === 'base'" />
      <ProfilePasswordSetting v-else-if="tabsValue === 'password'" />
    </template>
  </Profile>
</template>
