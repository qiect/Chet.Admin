<script setup lang="ts">
import { onMounted, ref, unref, watch } from 'vue';

import { SUPPORT_LANGUAGES } from '@vben/constants';
import { $t, loadLocaleMessages } from '@vben/locales';
import { useTimezoneStore } from '@vben/stores';

import InputItem from '../input-item.vue';
import SelectItem from '../select-item.vue';
import SwitchItem from '../switch-item.vue';

defineOptions({
  name: 'PreferenceGeneralConfig',
});

const appLocale = defineModel<string>('appLocale');
const appTimezone = defineModel<string>('appTimezone');
const appDynamicTitle = defineModel<boolean>('appDynamicTitle');
const appWatermark = defineModel<boolean>('appWatermark');
const appWatermarkContent = defineModel<string>('appWatermarkContent');
const appEnableCheckUpdates = defineModel<boolean>('appEnableCheckUpdates');
const appEnableCopyPreferences = defineModel<boolean>(
  'appEnableCopyPreferences',
);
const timezoneStore = useTimezoneStore();

const timezoneOptionsRef = ref<
  {
    label: string;
    value: string;
  }[]
>([]);

onMounted(async () => {
  timezoneOptionsRef.value = await timezoneStore.getTimezoneOptions();
  // 获取当前时区，例如：Asia/Shanghai
  const timezoneValue = unref(timezoneStore.timezone);
  if (timezoneValue) {
    appTimezone.value = timezoneValue;
  }
});

// 语言切换时立即加载语言包，使切换即时生效
watch(
  appLocale,
  (val) => {
    if (val) {
      loadLocaleMessages(val as any);
    }
  },
);

// 时区切换时同步到 timezoneStore，使 dayjs 默认时区立即更新
watch(
  appTimezone,
  (val) => {
    if (val && val !== unref(timezoneStore.timezone)) {
      timezoneStore.setTimezone(val);
    }
  },
);
</script>

<template>
  <SelectItem v-model="appLocale" :items="SUPPORT_LANGUAGES">
    {{ $t('preferences.language') }}
  </SelectItem>
  <SelectItem v-model="appTimezone" :items="timezoneOptionsRef">
    {{ $t('preferences.timezone') }}
  </SelectItem>
  <SwitchItem v-model="appDynamicTitle">
    {{ $t('preferences.dynamicTitle') }}
  </SwitchItem>
  <SwitchItem
    v-model="appWatermark"
    @update:model-value="
      (val) => {
        if (!val) appWatermarkContent = '';
      }
    "
  >
    {{ $t('preferences.watermark') }}
  </SwitchItem>
  <InputItem
    v-if="appWatermark"
    v-model="appWatermarkContent"
    :placeholder="$t('preferences.watermarkContent')"
  >
    {{ $t('preferences.watermarkContent') }}
  </InputItem>
  <SwitchItem v-model="appEnableCheckUpdates">
    {{ $t('preferences.checkUpdates') }}
  </SwitchItem>
  <SwitchItem v-model="appEnableCopyPreferences">
    {{ $t('preferences.enableCopyPreferences') }}
  </SwitchItem>
</template>
