<script lang="ts" setup>
import { ref, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';

import { Bell } from '@vben/icons';

import { Badge, Button, Empty, Modal, Popover, Spin, Tag } from 'ant-design-vue';

import {
  getMyNotificationsApi,
  getUnreadCountApi,
  markAllAsReadApi,
  markAsReadApi,
} from '#/api/system/notification';

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
  if (diff < 1) return '刚刚';
  if (diff < 60) return `${diff}分钟前`;
  if (diff < 1_440) return `${Math.floor(diff / 60)}小时前`;
  return `${Math.floor(diff / 1_440)}天前`;
}

const typeLabelMap: Record<string, string> = {
  Announcement: '公告',
  Notification: '通知',
  Todo: '待办',
};
const typeColorMap: Record<string, string> = {
  Announcement: 'blue',
  Notification: 'green',
  Todo: 'orange',
};
const priorityLabelMap: Record<string, string> = {
  Low: '低',
  Normal: '普通',
  High: '高',
  Urgent: '紧急',
};
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
            <span class="text-sm font-medium">通知</span>
            <Button
              v-if="unreadCount > 0"
              type="link"
              size="small"
              @click="onMarkAllRead"
            >
              全部已读
            </Button>
          </div>
          <Spin :spinning="loading">
            <div v-if="notifications.length === 0" class="py-4">
              <Empty
                description="暂无通知"
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
              查看全部
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
      :title="currentNotification?.title || '通知详情'"
      :footer="null"
      width="520px"
    >
      <div v-if="currentNotification" class="space-y-3">
        <div class="flex flex-wrap items-center gap-2">
          <Tag v-if="currentNotification.type" :color="typeColorMap[currentNotification.type] || 'default'">
            {{ typeLabelMap[currentNotification.type] || currentNotification.type }}
          </Tag>
          <Tag v-if="currentNotification.priority" :color="priorityColorMap[currentNotification.priority] || 'default'">
            优先级：{{ priorityLabelMap[currentNotification.priority] || currentNotification.priority }}
          </Tag>
          <span class="text-xs text-gray-400">
            {{ formatTime(currentNotification.createdAt) }}
          </span>
        </div>
        <div class="border-t pt-3 text-sm leading-6 whitespace-pre-wrap">
          {{ currentNotification.content || '（无内容）' }}
        </div>
      </div>
    </Modal>
  </div>
</template>
