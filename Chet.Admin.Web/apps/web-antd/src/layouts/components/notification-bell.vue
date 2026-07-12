<script lang="ts" setup>
import { computed, ref, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';

import { Bell } from '@vben/icons';

import { Badge, Button, Empty, Modal, Popover, Spin, Tag } from 'ant-design-vue';

import {
  getMyNotificationsApi,
  getUnreadCountApi,
  markAllAsReadApi,
  markAsReadApi,
} from '#/api/system/notification';
import { $t } from '#/locales';

const router = useRouter();

const unreadCount = ref(0);
const notifications = ref<any[]>([]);
const loading = ref(false);
const open = ref(false);

// 通知详情弹窗
const detailVisible = ref(false);
const currentNotification = ref<any>(null);

async function fetchUnreadCount() {
  try {
    const res = await getUnreadCountApi();
    unreadCount.value = res?.count ?? 0;
  } catch {
    // ignore
  }
}

async function fetchNotifications() {
  loading.value = true;
  try {
    const res = await getMyNotificationsApi({ pageNumber: 1, pageSize: 5 });
    notifications.value = res?.items || [];
  } catch {
    notifications.value = [];
  } finally {
    loading.value = false;
  }
}

async function onMarkAllRead() {
  try {
    await markAllAsReadApi();
    unreadCount.value = 0;
    notifications.value.forEach((n) => (n.isRead = true));
  } catch {
    // ignore
  }
}

async function onMarkRead(id: number) {
  try {
    await markAsReadApi(id);
    const item = notifications.value.find((n) => n.id === id);
    if (item) item.isRead = true;
    unreadCount.value = Math.max(0, unreadCount.value - 1);
  } catch {
    // ignore
  }
}

// 点击通知项：标记已读 + 展示详情
async function onClickNotification(item: any) {
  currentNotification.value = item;
  detailVisible.value = true;
  open.value = false;
  if (!item.isRead) {
    await onMarkRead(item.id);
  }
}

function onViewAll() {
  open.value = false;
  router.push('/system/notification');
}

function onOpenChange(v: boolean) {
  open.value = v;
  if (v) {
    fetchNotifications();
  }
}

let timer: ReturnType<typeof setInterval> | null = null;
onMounted(() => {
  fetchUnreadCount();
  timer = setInterval(fetchUnreadCount, 30_000);
});
onUnmounted(() => {
  if (timer) clearInterval(timer);
});

function formatTime(dateStr: string) {
  const date = new Date(dateStr);
  const now = new Date();
  const diff = Math.floor((now.getTime() - date.getTime()) / 60_000);
  if (diff < 1) return $t('common.notification.time.justNow');
  if (diff < 60) return $t('common.notification.time.minutesAgo', { count: diff });
  if (diff < 1_440) return $t('common.notification.time.hoursAgo', { count: Math.floor(diff / 60) });
  return $t('common.notification.time.daysAgo', { count: Math.floor(diff / 1_440) });
}

const typeLabelMap = computed<Record<string, string>>(() => ({
  Announcement: $t('common.notification.type.Announcement'),
  Notification: $t('common.notification.type.Notification'),
  Todo: $t('common.notification.type.Todo'),
}));
const typeColorMap: Record<string, string> = {
  Announcement: 'blue',
  Notification: 'green',
  Todo: 'orange',
};
const priorityLabelMap = computed<Record<string, string>>(() => ({
  Low: $t('common.notification.priority.Low'),
  Normal: $t('common.notification.priority.Normal'),
  High: $t('common.notification.priority.High'),
  Urgent: $t('common.notification.priority.Urgent'),
}));
const priorityColorMap: Record<string, string> = {
  Low: 'default',
  Normal: 'blue',
  High: 'orange',
  Urgent: 'red',
};
</script>

<template>
  <div class="flex items-center">
    <Popover
      :open="open"
      trigger="click"
      placement="bottomRight"
      @open-change="onOpenChange"
    >
      <template #content>
        <div class="w-80">
          <div class="mb-2 flex items-center justify-between border-b pb-2">
            <span class="text-sm font-medium">{{ $t('common.notification.title') }}</span>
            <Button
              v-if="unreadCount > 0"
              type="link"
              size="small"
              @click="onMarkAllRead"
            >
              {{ $t('common.notification.markAllRead') }}
            </Button>
          </div>
          <Spin :spinning="loading">
            <div v-if="notifications.length === 0" class="py-4">
              <Empty
                :description="$t('common.notification.empty')"
                :image="Empty.PRESENTED_IMAGE_SIMPLE"
              />
            </div>
            <div v-else class="max-h-80 overflow-y-auto">
              <div
                v-for="item in notifications"
                :key="item.id"
                class="flex cursor-pointer items-start gap-2 border-b border-gray-100 px-1 py-2 hover:bg-gray-50 dark:hover:bg-gray-800"
                @click="onClickNotification(item)"
              >
                <div
                  v-if="!item.isRead"
                  class="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-blue-500"
                />
                <div v-else class="mt-1.5 h-2 w-2 shrink-0" />
                <div class="min-w-0 flex-1">
                  <div class="truncate text-sm">{{ item.title }}</div>
                  <div class="text-xs text-gray-400">
                    {{ formatTime(item.createdAt) }}
                  </div>
                </div>
              </div>
            </div>
          </Spin>
          <div class="mt-2 border-t pt-2 text-center">
            <Button type="link" size="small" @click="onViewAll">
              {{ $t('common.notification.viewAll') }}
            </Button>
          </div>
        </div>
      </template>
      <Badge :count="unreadCount" :overflow-count="99" size="small">
        <div
          class="mr-1 flex h-8 w-8 cursor-pointer items-center justify-center rounded-md px-1 text-lg text-foreground/80 transition-colors hover:bg-accent hover:text-accent-foreground"
        >
          <Bell class="size-4" />
        </div>
      </Badge>
    </Popover>

    <Modal
      v-model:open="detailVisible"
      :title="currentNotification?.title || $t('common.notification.detailTitle')"
      :footer="null"
      width="520px"
    >
      <div v-if="currentNotification" class="space-y-3">
        <div class="flex flex-wrap items-center gap-2">
          <Tag v-if="currentNotification.type" :color="typeColorMap[currentNotification.type] || 'default'">
            {{ typeLabelMap[currentNotification.type] || currentNotification.type }}
          </Tag>
          <Tag v-if="currentNotification.priority" :color="priorityColorMap[currentNotification.priority] || 'default'">
            {{ $t('common.notification.priorityLabel') }}：{{ priorityLabelMap[currentNotification.priority] || currentNotification.priority }}
          </Tag>
          <span class="text-xs text-gray-400">
            {{ formatTime(currentNotification.createdAt) }}
          </span>
        </div>
        <div class="border-t pt-3 text-sm leading-6 whitespace-pre-wrap">
          {{ currentNotification.content || $t('common.notification.noContent') }}
        </div>
      </div>
    </Modal>
  </div>
</template>
