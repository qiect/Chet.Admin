<script lang="ts" setup>
import type { VbenFormSchema } from '@vben/common-ui';
import type { SliderCaptchaActionType } from '@vben/common-ui';

import { computed, reactive, ref } from 'vue';

import { useVbenForm, VbenButton, z, SliderCaptcha } from '@vben/common-ui';
import { $t } from '@vben/locales';

import { useAuthStore } from '#/store';

defineOptions({ name: 'Login' });

const authStore = useAuthStore();

// 滑块验证状态
const captchaVerified = ref(false);
const sliderRef = ref<SliderCaptchaActionType>();
const lockMessage = ref('');

const formSchema = computed((): VbenFormSchema[] => {
  return [
    {
      component: 'VbenInput',
      componentProps: {
        placeholder: $t('authentication.emailTip'),
      },
      defaultValue: 'admin@example.com',
      fieldName: 'email',
      label: $t('authentication.email'),
      rules: z
        .string()
        .min(1, { message: $t('authentication.emailTip') })
        .email({ message: $t('authentication.emailValidErrorTip') }),
    },
    {
      component: 'VbenInputPassword',
      componentProps: {
        placeholder: $t('authentication.password'),
      },
      defaultValue: 'Admin@123',
      fieldName: 'password',
      label: $t('authentication.password'),
      rules: z.string().min(1, { message: $t('authentication.passwordTip') }),
    },
  ];
});

const [Form, formApi] = useVbenForm(
  reactive({
    commonConfig: {
      hideLabel: true,
      hideRequiredMark: true,
    },
    schema: formSchema,
    showDefaultActions: false,
  }),
);

function handleCaptchaSuccess() {
  captchaVerified.value = true;
  lockMessage.value = '';
}

async function handleLogin() {
  const { valid } = await formApi.validate();
  if (!valid) return;

  // 校验滑块验证
  if (!captchaVerified.value) {
    lockMessage.value = '请先完成滑块验证';
    return;
  }

  lockMessage.value = '';

  const values = await formApi.getValues();
  const params: Record<string, any> = { ...values };

  try {
    await authStore.authLogin(params);
  } catch (error: any) {
    const responseData = error?.response?.data ?? error?.data ?? {};
    // 登录失败后重置滑块，要求重新验证
    captchaVerified.value = false;
    sliderRef.value?.resume();
    if (responseData.lockedUntil) {
      const lockedTime = new Date(responseData.lockedUntil);
      const now = new Date();
      const remainingMinutes = Math.ceil(
        (lockedTime.getTime() - now.getTime()) / 60000,
      );
      if (remainingMinutes > 0) {
        lockMessage.value = `账号已锁定，请 ${remainingMinutes} 分钟后重试`;
      }
    }
  }
}
</script>

<template>
  <div @keydown.enter.prevent="handleLogin">
    <div class="mb-7 sm:mx-auto sm:w-full sm:max-w-md">
      <h2
        class="mb-3 text-3xl/9 font-bold tracking-tight text-foreground lg:text-4xl"
      >
        {{ $t('authentication.welcomeBack') }} 👋🏻
      </h2>
      <p class="text-sm text-muted-foreground lg:text-md">
        {{ $t('authentication.loginSubtitle') }}
      </p>
    </div>

    <Form />

    <!-- 滑块拖动验证 -->
    <div class="mb-4 mt-1">
      <SliderCaptcha
        ref="sliderRef"
        success-text="验证通过"
        text="请按住滑块，拖动到最右边"
        @success="handleCaptchaSuccess"
      />
    </div>

    <!-- 账号锁定提示 -->
    <div v-if="lockMessage" class="mb-4 text-center text-sm text-red-500">
      {{ lockMessage }}
    </div>

    <VbenButton
      :class="{ 'cursor-wait': authStore.loginLoading }"
      :loading="authStore.loginLoading"
      class="w-full"
      @click="handleLogin"
    >
      {{ $t('common.login') }}
    </VbenButton>
  </div>
</template>
