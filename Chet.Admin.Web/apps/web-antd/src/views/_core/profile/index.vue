<script setup lang="ts">
import { computed, ref } from 'vue';

import { Profile } from '@vben/common-ui';
import { useUserStore } from '@vben/stores';

import { $t } from '#/locales';

import ProfileBaseSetting from './base-setting.vue';
import ProfilePasswordSetting from './password-setting.vue';

const userStore = useUserStore();

// 默认显示"基本设置"
const tabsValue = ref<string>('base');

const tabs = computed(() => [
  {
    label: $t('profile.tabs.baseSetting'),
    value: 'base',
  },
  {
    label: $t('profile.tabs.password'),
    value: 'password',
  },
]);
</script>

<template>
  <Profile
    v-model:model-value="tabsValue"
    :title="$t('profile.title')"
    :user-info="userStore.userInfo"
    :tabs="tabs"
  >
    <template #content>
      <ProfileBaseSetting v-if="tabsValue === 'base'" />
      <ProfilePasswordSetting v-else-if="tabsValue === 'password'" />
    </template>
  </Profile>
</template>
