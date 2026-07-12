<script lang="ts" setup>
import { ref, onMounted, onUnmounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { usePreferences } from '@vben/preferences';
import { IconifyIcon } from '@vben/icons';
import { $t } from '#/locales';
import { getDashboardStatsApi, getDashboardTrendApi, getRecentLogsApi } from '#/api/system/dashboard';

const router = useRouter();
const { isDark } = usePreferences();

const stats = ref({
  userCount: 0,
  roleCount: 0,
  menuCount: 0,
  departmentCount: 0,
  todayLoginCount: 0,
  activeUserCount: 0,
});
const trendItems = ref<{ date: string; loginCount: number; registerCount: number }[]>([]);
const recentLogs = ref<{ id: number; userName: string; action: string; module: string; description: string; operatedAt: string }[]>([]);
const loading = ref(true);

const greeting = computed(() => {
  const h = new Date().getHours();
  if (h < 6) return $t('dashboard.greeting.lateNight');
  if (h < 9) return $t('dashboard.greeting.morning');
  if (h < 12) return $t('dashboard.greeting.forenoon');
  if (h < 14) return $t('dashboard.greeting.noon');
  if (h < 18) return $t('dashboard.greeting.afternoon');
  return $t('dashboard.greeting.evening');
});

const currentTime = ref('');
const currentDate = ref('');

function updateTime() {
  const now = new Date();
  currentTime.value = now.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  const weekDays = [
    $t('dashboard.weekdays.sunday'),
    $t('dashboard.weekdays.monday'),
    $t('dashboard.weekdays.tuesday'),
    $t('dashboard.weekdays.wednesday'),
    $t('dashboard.weekdays.thursday'),
    $t('dashboard.weekdays.friday'),
    $t('dashboard.weekdays.saturday'),
  ];
  currentDate.value = now.toLocaleDateString(undefined, { year: 'numeric', month: 'long', day: 'numeric' }) + ' ' + weekDays[now.getDay()];
}

let timer: ReturnType<typeof setInterval>;

// SVG line chart computed
const chartWidth = 640;
const chartHeight = 220;
const chartPaddingX = 48;  // 左侧留出纵坐标标签空间
const chartPaddingY = 24;
const chartBottomPadding = 32; // 底部留出 X 轴标签空间

// 计算图表最大值（向上取整到合适的刻度，让纵坐标更"友好"）
const chartMaxVal = computed(() => {
  const items = trendItems.value;
  if (!items.length) return 10;
  const rawMax = Math.max(...items.map((i) => i.loginCount), 1);
  // 向上取整到 5 的倍数，避免数据点贴顶
  return Math.max(5, Math.ceil(rawMax / 5) * 5);
});

const plotW = computed(() => chartWidth - chartPaddingX - 16);
const plotH = computed(() => chartHeight - chartPaddingY - chartBottomPadding);

// 纵坐标刻度（4 档：0, 25%, 50%, 75%, 100%）
const yAxisTicks = computed(() => {
  const max = chartMaxVal.value;
  const ph = plotH.value;
  const steps = 4;
  return Array.from({ length: steps + 1 }, (_, i) => {
    const ratio = i / steps;
    const value = Math.round(max * ratio);
    const y = chartPaddingY + ph - ratio * ph;
    return { value, y };
  });
});

const trendPoints = computed(() => {
  const items = trendItems.value;
  if (!items.length) return '';
  const max = chartMaxVal.value;
  const pw = plotW.value;
  const ph = plotH.value;

  return items
    .map((item, idx) => {
      const x = chartPaddingX + (idx / Math.max(items.length - 1, 1)) * pw;
      const y = chartPaddingY + ph - (item.loginCount / max) * ph;
      return `${x},${y}`;
    })
    .join(' ');
});

const trendAreaPoints = computed(() => {
  const items = trendItems.value;
  if (!items.length) return '';
  const max = chartMaxVal.value;
  const pw = plotW.value;
  const ph = plotH.value;
  const bottomY = chartPaddingY + ph;

  const points = items
    .map((item, idx) => {
      const x = chartPaddingX + (idx / Math.max(items.length - 1, 1)) * pw;
      const y = chartPaddingY + ph - (item.loginCount / max) * ph;
      return `${x},${y}`;
    })
    .join(' ');

  const lastX = chartPaddingX + pw;
  const firstX = chartPaddingX;
  return `${firstX},${bottomY} ${points} ${lastX},${bottomY}`;
});

// 数据点坐标，供 hover 交互使用
const trendDataPoints = computed(() => {
  const items = trendItems.value;
  if (!items.length) return [];
  const max = chartMaxVal.value;
  const pw = plotW.value;
  const ph = plotH.value;
  return items.map((item, idx) => {
    const x = chartPaddingX + (idx / Math.max(items.length - 1, 1)) * pw;
    const y = chartPaddingY + ph - (item.loginCount / max) * ph;
    return { x, y, ...item };
  });
});

const trendLabels = computed(() => {
  const items = trendItems.value;
  if (!items.length) return [];
  const pw = plotW.value;
  return items.map((item, idx) => ({
    text: item.date,
    x: chartPaddingX + (idx / Math.max(items.length - 1, 1)) * pw,
  }));
});

function formatTime(dateStr: string) {
  const d = new Date(dateStr);
  const now = new Date();
  const diff = now.getTime() - d.getTime();
  if (diff < 60000) return $t('dashboard.relativeTime.justNow');
  if (diff < 3600000) return $t('dashboard.relativeTime.minutesAgo', { n: Math.floor(diff / 60000) });
  if (diff < 86400000) return $t('dashboard.relativeTime.hoursAgo', { n: Math.floor(diff / 3600000) });
  return $t('dashboard.relativeTime.daysAgo', { n: Math.floor(diff / 86400000) });
}

const actionIconMap: Record<string, string> = {
  Login: 'lucide:log-in',
  Logout: 'lucide:log-out',
  Create: 'lucide:plus-circle',
  Update: 'lucide:edit',
  Delete: 'lucide:trash-2',
};

function getActionIcon(action: string) {
  return actionIconMap[action] || 'lucide:activity';
}

const actionColorMap: Record<string, string> = {
  Login: '#10b981',
  Logout: '#6b7280',
  Create: '#3b82f6',
  Update: '#f59e0b',
  Delete: '#ef4444',
};

function getActionColor(action: string) {
  return actionColorMap[action] || '#6366f1';
}

onMounted(async () => {
  updateTime();
  timer = setInterval(updateTime, 1000);
  try {
    const [statsRes, trendRes, logsRes] = await Promise.all([
      getDashboardStatsApi().catch(() => null),
      getDashboardTrendApi(7).catch(() => null),
      getRecentLogsApi(10).catch(() => null),
    ]);
    if (statsRes) {
      stats.value = statsRes;
    }
    if (trendRes?.items) {
      trendItems.value = trendRes.items;
    }
    if (logsRes) {
      recentLogs.value = logsRes;
    }
  } catch {
    /* */
  } finally {
    loading.value = false;
  }
});

onUnmounted(() => {
  clearInterval(timer);
});

const shortcuts = [
  { icon: 'lucide:users', titleKey: 'dashboard.shortcuts.user.title', descKey: 'dashboard.shortcuts.user.desc', path: '/system/user', color: '#6366f1' },
  { icon: 'lucide:shield', titleKey: 'dashboard.shortcuts.role.title', descKey: 'dashboard.shortcuts.role.desc', path: '/system/role', color: '#f59e0b' },
  { icon: 'lucide:menu', titleKey: 'dashboard.shortcuts.menu.title', descKey: 'dashboard.shortcuts.menu.desc', path: '/system/menu', color: '#10b981' },
  { icon: 'lucide:building', titleKey: 'dashboard.shortcuts.department.title', descKey: 'dashboard.shortcuts.department.desc', path: '/system/department', color: '#3b82f6' },
  { icon: 'lucide:book-open', titleKey: 'dashboard.shortcuts.dictionary.title', descKey: 'dashboard.shortcuts.dictionary.desc', path: '/system/dictionary', color: '#8b5cf6' },
  { icon: 'lucide:bell', titleKey: 'dashboard.shortcuts.notification.title', descKey: 'dashboard.shortcuts.notification.desc', path: '/system/notification', color: '#ef4444' },
];

function goPage(path: string) {
  router.push(path);
}
</script>

<template>
  <div class="dashboard-root" :class="{ 'is-dark': isDark }">
    <!-- 英雄区域 -->
    <div class="hero-section">
      <div class="hero-bg">
        <div class="hero-orb hero-orb-1"></div>
        <div class="hero-orb hero-orb-2"></div>
        <div class="hero-orb hero-orb-3"></div>
      </div>
      <div class="hero-content">
        <div class="hero-greeting">
          <IconifyIcon icon="lucide:hand-metal" class="hero-wave-icon" />
          <h1 class="hero-title">{{ greeting }}，<span class="hero-highlight">{{ $t('dashboard.hero.admin') }}</span></h1>
          <p class="hero-subtitle">{{ $t('dashboard.hero.welcome') }}</p>
        </div>
        <div class="hero-time">
          <div class="time-display">{{ currentTime }}</div>
          <div class="date-display">{{ currentDate }}</div>
        </div>
      </div>
    </div>

    <!-- 统计卡片 -->
    <div class="stats-grid">
      <div class="stat-card" style="--accent: #6366f1">
        <div class="stat-icon"><IconifyIcon icon="lucide:users" width="24" /></div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.userCount }}</div>
          <div class="stat-label">{{ $t('dashboard.stat.userCount') }}</div>
        </div>
      </div>
      <div class="stat-card" style="--accent: #f59e0b">
        <div class="stat-icon"><IconifyIcon icon="lucide:shield" width="24" /></div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.roleCount }}</div>
          <div class="stat-label">{{ $t('dashboard.stat.roleCount') }}</div>
        </div>
      </div>
      <div class="stat-card" style="--accent: #10b981">
        <div class="stat-icon"><IconifyIcon icon="lucide:menu" width="24" /></div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.menuCount }}</div>
          <div class="stat-label">{{ $t('dashboard.stat.menuCount') }}</div>
        </div>
      </div>
      <div class="stat-card" style="--accent: #3b82f6">
        <div class="stat-icon"><IconifyIcon icon="lucide:building" width="24" /></div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.departmentCount }}</div>
          <div class="stat-label">{{ $t('dashboard.stat.departmentCount') }}</div>
        </div>
      </div>
      <div class="stat-card" style="--accent: #ef4444">
        <div class="stat-icon"><IconifyIcon icon="lucide:log-in" width="24" /></div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.todayLoginCount }}</div>
          <div class="stat-label">{{ $t('dashboard.stat.todayLogin') }}</div>
        </div>
      </div>
      <div class="stat-card" style="--accent: #8b5cf6">
        <div class="stat-icon"><IconifyIcon icon="lucide:activity" width="24" /></div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.activeUserCount }}</div>
          <div class="stat-label">{{ $t('dashboard.stat.active7d') }}</div>
        </div>
      </div>
    </div>

    <!-- 趋势图 + 最近操作 -->
    <div class="trend-logs-row">
      <!-- 趋势图 -->
      <div class="trend-card">
        <div class="card-header">
          <h3 class="card-title"><IconifyIcon icon="lucide:trending-up" width="18" class="card-title-icon" /> {{ $t('dashboard.trend.title') }}</h3>
          <span class="card-subtitle">{{ $t('dashboard.trend.recent7days') }}</span>
        </div>
        <div class="trend-chart-wrap">
          <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="trend-svg">
            <defs>
              <!-- 曲线渐变 -->
              <linearGradient :id="`trend-line-${isDark ? 'dark' : 'light'}`" x1="0" y1="0" x2="1" y2="0">
                <stop offset="0%" :stop-color="isDark ? '#818cf8' : '#6366f1'" />
                <stop offset="100%" :stop-color="isDark ? '#22d3ee' : '#0ea5e9'" />
              </linearGradient>
              <!-- 面积填充渐变 -->
              <linearGradient :id="`trend-area-${isDark ? 'dark' : 'light'}`" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" :stop-color="isDark ? 'rgba(129,140,248,0.35)' : 'rgba(99,102,241,0.28)'" />
                <stop offset="100%" :stop-color="isDark ? 'rgba(129,140,248,0.02)' : 'rgba(99,102,241,0.02)'" />
              </linearGradient>
            </defs>

            <!-- 纵坐标网格线 + 刻度标签 -->
            <g class="y-axis">
              <template v-for="(tick, idx) in yAxisTicks" :key="`y-tick-${idx}`">
                <line
                  :x1="chartPaddingX"
                  :y1="tick.y"
                  :x2="chartPaddingX + plotW"
                  :y2="tick.y"
                  :stroke="isDark ? 'rgba(51,65,85,0.5)' : 'rgba(241,245,249,0.8)'"
                  stroke-width="1"
                  :stroke-dasharray="idx === 0 ? '0' : '3 4'"
                />
                <text
                  :x="chartPaddingX - 8"
                  :y="tick.y + 3"
                  text-anchor="end"
                  :fill="isDark ? '#64748b' : '#94a3b8'"
                  font-size="10"
                  font-weight="500"
                >
                  {{ tick.value }}
                </text>
              </template>
            </g>

            <!-- Y 轴主线 -->
            <line
              :x1="chartPaddingX"
              :y1="chartPaddingY"
              :x2="chartPaddingX"
              :y2="chartPaddingY + plotH"
              :stroke="isDark ? '#334155' : '#e2e8f0'"
              stroke-width="1.5"
            />

            <!-- Area fill -->
            <polygon
              v-if="trendAreaPoints"
              :points="trendAreaPoints"
              :fill="`url(#trend-area-${isDark ? 'dark' : 'light'})`"
            />

            <!-- Line -->
            <polyline
              v-if="trendPoints"
              :points="trendPoints"
              fill="none"
              :stroke="`url(#trend-line-${isDark ? 'dark' : 'light'})`"
              stroke-width="2.5"
              stroke-linecap="round"
              stroke-linejoin="round"
            />

            <!-- Data points with hover tooltip -->
            <g v-for="(pt, idx) in trendDataPoints" :key="`dot-${idx}`" class="trend-dot-group">
              <!-- 透明热区 -->
              <circle :cx="pt.x" :cy="pt.y" r="14" fill="transparent" />
              <!-- 可见点 -->
              <circle
                :cx="pt.x"
                :cy="pt.y"
                r="4"
                :fill="isDark ? '#22d3ee' : '#0ea5e9'"
                :stroke="isDark ? '#1e293b' : '#ffffff'"
                stroke-width="2"
                class="trend-dot"
              />
              <!-- 悬浮提示 -->
              <g class="trend-tooltip">
                <rect
                  :x="pt.x - 32"
                  :y="pt.y - 32"
                  width="64"
                  height="22"
                  rx="6"
                  :fill="isDark ? '#1e293b' : '#1f2937'"
                  opacity="0.95"
                />
                <text
                  :x="pt.x"
                  :y="pt.y - 17"
                  text-anchor="middle"
                  fill="#ffffff"
                  font-size="11"
                  font-weight="600"
                >
                  {{ $t('dashboard.trend.times', { n: pt.loginCount }) }}
                </text>
              </g>
              <!-- 垂直辅助线 -->
              <line
                :x1="pt.x"
                :y1="pt.y"
                :x2="pt.x"
                :y2="chartPaddingY + plotH"
                :stroke="isDark ? '#22d3ee' : '#0ea5e9'"
                stroke-width="1"
                stroke-dasharray="2 3"
                opacity="0"
                class="trend-guide-line"
              />
            </g>

            <!-- X axis labels -->
            <text
              v-for="(label, idx) in trendLabels"
              :key="`label-${idx}`"
              :x="label.x"
              :y="chartHeight - 8"
              text-anchor="middle"
              :fill="isDark ? '#64748b' : '#94a3b8'"
              font-size="11"
            >
              {{ label.text }}
            </text>
          </svg>
          <div v-if="!trendItems.length" class="trend-empty">{{ $t('dashboard.trend.empty') }}</div>
        </div>
      </div>

      <!-- 最近操作 -->
      <div class="logs-card">
        <div class="card-header">
          <h3 class="card-title"><IconifyIcon icon="lucide:scroll-text" width="18" class="card-title-icon" /> {{ $t('dashboard.logs.title') }}</h3>
          <span class="card-subtitle">{{ $t('dashboard.logs.recent10') }}</span>
        </div>
        <div class="logs-list">
          <div v-for="log in recentLogs" :key="log.id" class="log-item">
            <div class="log-icon" :style="{ background: getActionColor(log.action) + '15', color: getActionColor(log.action) }">
              <IconifyIcon :icon="getActionIcon(log.action)" width="14" />
            </div>
            <div class="log-content">
              <div class="log-main">
                <span class="log-user">{{ log.userName }}</span>
                <span class="log-action">{{ log.action }}</span>
                <span class="log-module">{{ log.module }}</span>
              </div>
              <div class="log-desc">{{ log.description }}</div>
            </div>
            <div class="log-time">{{ formatTime(log.operatedAt) }}</div>
          </div>
          <div v-if="!recentLogs.length" class="logs-empty">
            <IconifyIcon icon="lucide:inbox" width="32" />
            <span>{{ $t('dashboard.logs.empty') }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- 快捷入口 -->
    <div class="section-header">
      <h2 class="section-title">{{ $t('dashboard.shortcuts.sectionTitle') }}</h2>
      <p class="section-desc">{{ $t('dashboard.shortcuts.sectionDesc') }}</p>
    </div>
    <div class="shortcuts-grid">
      <div
        v-for="item in shortcuts"
        :key="item.path"
        class="shortcut-card"
        :style="{ '--shortcut-color': item.color }"
        @click="goPage(item.path)"
      >
        <div class="shortcut-icon"><IconifyIcon :icon="item.icon" width="22" /></div>
        <div class="shortcut-info">
          <div class="shortcut-title">{{ $t(item.titleKey) }}</div>
          <div class="shortcut-desc">{{ $t(item.descKey) }}</div>
        </div>
        <div class="shortcut-arrow"><IconifyIcon icon="lucide:arrow-right" width="16" /></div>
      </div>
    </div>

    <!-- 系统信息 -->
    <div class="system-info">
      <div class="info-card">
        <h3 class="info-title">{{ $t('dashboard.systemInfo.title') }}</h3>
        <div class="info-grid">
          <div class="info-item"><span class="info-label">{{ $t('dashboard.systemInfo.systemName') }}</span><span class="info-value">Chet Admin</span></div>
          <div class="info-item"><span class="info-label">{{ $t('dashboard.systemInfo.frameworkVersion') }}</span><span class="info-value">Vben Admin v5.7</span></div>
          <div class="info-item"><span class="info-label">{{ $t('dashboard.systemInfo.frontendFramework') }}</span><span class="info-value">Vue 3 + TypeScript</span></div>
          <div class="info-item"><span class="info-label">{{ $t('dashboard.systemInfo.uiLibrary') }}</span><span class="info-value">Ant Design Vue</span></div>
          <div class="info-item"><span class="info-label">{{ $t('dashboard.systemInfo.backendFramework') }}</span><span class="info-value">.NET Core WebAPI</span></div>
          <div class="info-item"><span class="info-label">{{ $t('dashboard.systemInfo.database') }}</span><span class="info-value">SQLite (EF Core)</span></div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ===== 亮色变量 ===== */
.dashboard-root {
  --bg-page: #f0f2f5;
  --bg-card: #ffffff;
  --bg-card-hover: #ffffff;
  --text-primary: #1f2937;
  --text-secondary: #6b7280;
  --text-muted: #9ca3af;
  --border-card: #f3f4f6;
  --border-card-hover: #e5e7eb;
  --stat-icon-bg-opacity: 0.1;
}

/* ===== 暗色变量 ===== */
.dashboard-root.is-dark {
  --bg-page: #0f172a;
  --bg-card: #1e293b;
  --bg-card-hover: #1e293b;
  --text-primary: #f1f5f9;
  --text-secondary: #94a3b8;
  --text-muted: #64748b;
  --border-card: #334155;
  --border-card-hover: #475569;
  --stat-icon-bg-opacity: 0.15;
}

.dashboard-root {
  min-height: 100%;
  padding: 0;
  background: var(--bg-page);
}

/* ===== 英雄区域 ===== */
.hero-section {
  position: relative;
  overflow: hidden;
  padding: 48px 40px 40px;
  background: linear-gradient(135deg, #1e1b4b 0%, #312e81 40%, #4338ca 100%);
  color: #fff;
}

.hero-bg {
  position: absolute;
  inset: 0;
  overflow: hidden;
}
.hero-orb {
  position: absolute;
  border-radius: 50%;
  filter: blur(80px);
  opacity: 0.3;
}
.hero-orb-1 {
  width: 400px;
  height: 400px;
  background: #818cf8;
  top: -150px;
  right: -50px;
  animation: orbF1 8s ease-in-out infinite;
}
.hero-orb-2 {
  width: 300px;
  height: 300px;
  background: #a78bfa;
  bottom: -100px;
  left: 10%;
  animation: orbF2 10s ease-in-out infinite;
}
.hero-orb-3 {
  width: 200px;
  height: 200px;
  background: #6366f1;
  top: 20%;
  left: 50%;
  animation: orbF3 12s ease-in-out infinite;
}

@keyframes orbF1 {
  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }
  50% {
    transform: translate(-40px, 30px) scale(1.1);
  }
}
@keyframes orbF2 {
  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }
  50% {
    transform: translate(30px, -40px) scale(1.15);
  }
}
@keyframes orbF3 {
  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }
  50% {
    transform: translate(-20px, 20px) scale(0.9);
  }
}

.hero-content {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.hero-greeting {
  .hero-wave-icon {
    font-size: 36px;
    color: #a5b4fc;
    display: inline-block;
    animation: wave 2.5s ease-in-out infinite;
    transform-origin: 70% 70%;
  }
  .hero-title {
    font-size: 32px;
    font-weight: 700;
    margin: 12px 0 8px;
    letter-spacing: -0.5px;
  }
  .hero-highlight {
    background: linear-gradient(135deg, #a5b4fc, #818cf8);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    background-clip: text;
  }
  .hero-subtitle {
    font-size: 16px;
    color: rgba(255, 255, 255, 0.65);
    margin: 0;
    font-weight: 400;
  }
}

@keyframes wave {
  0% {
    transform: rotate(0deg);
  }
  10% {
    transform: rotate(14deg);
  }
  20% {
    transform: rotate(-8deg);
  }
  30% {
    transform: rotate(14deg);
  }
  40% {
    transform: rotate(-4deg);
  }
  50%,
  100% {
    transform: rotate(0deg);
  }
}

.hero-time {
  text-align: right;
  .time-display {
    font-size: 56px;
    font-weight: 200;
    letter-spacing: 4px;
    font-variant-numeric: tabular-nums;
    line-height: 1;
  }
  .date-display {
    font-size: 14px;
    color: rgba(255, 255, 255, 0.5);
    margin-top: 8px;
  }
}

/* ===== 统计卡片 ===== */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(6, 1fr);
  gap: 20px;
  padding: 0 40px;
  margin-top: -24px;
  position: relative;
  z-index: 2;
}

.stat-card {
  background: var(--bg-card);
  border-radius: 16px;
  padding: 24px;
  display: flex;
  align-items: center;
  gap: 16px;
  box-shadow:
    0 1px 3px rgba(0, 0, 0, 0.06),
    0 8px 24px rgba(0, 0, 0, 0.04);
  transition:
    transform 0.3s ease,
    box-shadow 0.3s ease;
  cursor: default;
}
.stat-card:hover {
  transform: translateY(-4px);
  box-shadow:
    0 4px 12px rgba(0, 0, 0, 0.08),
    0 16px 40px rgba(0, 0, 0, 0.06);
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  background: color-mix(in srgb, var(--accent) var(--stat-icon-bg-opacity-percent, 10%), transparent);
  color: var(--accent);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.stat-info {
  .stat-value {
    font-size: 28px;
    font-weight: 700;
    color: var(--text-primary);
    line-height: 1.2;
    font-variant-numeric: tabular-nums;
  }
  .stat-label {
    font-size: 13px;
    color: var(--text-muted);
    margin-top: 2px;
  }
}

/* ===== 趋势图 + 最近操作 ===== */
.trend-logs-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
  padding: 32px 40px 0;
}

.trend-card,
.logs-card {
  background: var(--bg-card);
  border-radius: 16px;
  padding: 24px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.card-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
  display: flex;
  align-items: center;
  gap: 8px;
}

.card-title-icon {
  color: #3b82f6;
}

.card-subtitle {
  font-size: 12px;
  color: var(--text-muted);
}

.trend-chart-wrap {
  position: relative;
}

.trend-svg {
  width: 100%;
  height: 220px;
  overflow: visible;
}

/* 数据点 hover 交互 */
.trend-dot-group {
  cursor: pointer;
}
.trend-dot-group .trend-dot {
  transition: r 0.2s ease, filter 0.2s ease;
  filter: drop-shadow(0 0 0 transparent);
}
.trend-dot-group .trend-tooltip,
.trend-dot-group .trend-guide-line {
  opacity: 0;
  transition: opacity 0.18s ease;
  pointer-events: none;
}
.trend-dot-group:hover .trend-dot {
  r: 5.5;
  filter: drop-shadow(0 0 6px currentColor);
}
.trend-dot-group:hover .trend-tooltip,
.trend-dot-group:hover .trend-guide-line {
  opacity: 1;
}

.trend-empty {
  text-align: center;
  padding: 40px 0;
  color: var(--text-muted);
  font-size: 14px;
}

/* ===== 最近操作 ===== */
/* 限制高度与趋势图对齐，超出条目内部滚动，避免撑开整行高度 */
.logs-card {
  display: flex;
  flex-direction: column;
  max-height: 320px;
  overflow: hidden;
}

.logs-list {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 12px;
  overflow-y: auto;
  min-height: 0;
  margin-right: -8px;
  padding-right: 8px;
}

/* 自定义滚动条 - 细线风格，与卡片设计语言一致 */
.logs-list::-webkit-scrollbar {
  width: 4px;
}
.logs-list::-webkit-scrollbar-track {
  background: transparent;
}
.logs-list::-webkit-scrollbar-thumb {
  background: var(--border-card-hover);
  border-radius: 2px;
  transition: background 0.2s ease;
}
.logs-list::-webkit-scrollbar-thumb:hover {
  background: var(--text-muted);
}

.log-item {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 10px 12px;
  border-radius: 10px;
  transition: background 0.2s;
}

.log-item:hover {
  background: var(--border-card);
}

.log-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.log-content {
  flex: 1;
  min-width: 0;
}

.log-main {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
}

.log-user {
  font-weight: 600;
  color: var(--text-primary);
}

.log-action {
  padding: 1px 6px;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 500;
  background: var(--border-card);
  color: var(--text-secondary);
}

.log-module {
  color: var(--text-muted);
  font-size: 12px;
}

.log-desc {
  font-size: 12px;
  color: var(--text-muted);
  margin-top: 2px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.log-time {
  font-size: 11px;
  color: var(--text-muted);
  white-space: nowrap;
  flex-shrink: 0;
  margin-top: 2px;
}

.logs-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 32px 0;
  color: var(--text-muted);
  font-size: 13px;
}

/* ===== 快捷入口 ===== */
.section-header {
  padding: 32px 40px 16px;
}
.section-title {
  font-size: 20px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0;
}
.section-desc {
  font-size: 13px;
  color: var(--text-muted);
  margin: 4px 0 0;
}

.shortcuts-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
  padding: 0 40px;
}

.shortcut-card {
  background: var(--bg-card);
  border-radius: 14px;
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  cursor: pointer;
  transition: all 0.25s ease;
  border: 1px solid var(--border-card);
  position: relative;
  overflow: hidden;
}
.shortcut-card::before {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(135deg, color-mix(in srgb, var(--shortcut-color) 5%, transparent), transparent);
  opacity: 0;
  transition: opacity 0.25s;
}
.shortcut-card:hover {
  border-color: color-mix(in srgb, var(--shortcut-color) 30%, transparent);
  transform: translateY(-2px);
  box-shadow: 0 4px 16px color-mix(in srgb, var(--shortcut-color) 12%, transparent);
}
.shortcut-card:hover::before {
  opacity: 1;
}
.shortcut-card:hover .shortcut-arrow {
  opacity: 1;
  transform: translateX(0);
  color: var(--shortcut-color);
}

.shortcut-icon {
  width: 44px;
  height: 44px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 10px;
  background: color-mix(in srgb, var(--shortcut-color) var(--stat-icon-bg-opacity-percent, 10%), transparent);
  color: var(--shortcut-color);
  flex-shrink: 0;
  position: relative;
  z-index: 1;
}

.shortcut-info {
  flex: 1;
  position: relative;
  z-index: 1;
  .shortcut-title {
    font-size: 15px;
    font-weight: 600;
    color: var(--text-primary);
  }
  .shortcut-desc {
    font-size: 12px;
    color: var(--text-muted);
    margin-top: 2px;
  }
}

.shortcut-arrow {
  opacity: 0;
  transform: translateX(-8px);
  transition: all 0.25s ease;
  color: var(--text-muted);
  position: relative;
  z-index: 1;
}

/* ===== 系统信息 ===== */
.system-info {
  padding: 32px 40px 40px;
}
.info-card {
  background: var(--bg-card);
  border-radius: 16px;
  padding: 28px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}
.info-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 0 0 20px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--border-card);
}
.info-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px 32px;
}
.info-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
  .info-label {
    font-size: 12px;
    color: var(--text-muted);
  }
  .info-value {
    font-size: 14px;
    color: var(--text-secondary);
    font-weight: 500;
  }
}

/* ===== 响应式 ===== */
@media (max-width: 1200px) {
  .stats-grid {
    grid-template-columns: repeat(3, 1fr);
  }
  .shortcuts-grid {
    grid-template-columns: repeat(2, 1fr);
  }
  .trend-logs-row {
    grid-template-columns: 1fr;
  }
}
@media (max-width: 768px) {
  .hero-section {
    padding: 32px 20px 28px;
  }
  .hero-content {
    flex-direction: column;
    align-items: flex-start;
    gap: 16px;
  }
  .hero-time {
    text-align: left;
    .time-display {
      font-size: 36px;
    }
  }
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
    padding: 0 20px;
  }
  .trend-logs-row {
    grid-template-columns: 1fr;
    padding: 24px 20px 0;
  }
  .shortcuts-grid {
    grid-template-columns: 1fr;
    padding: 0 20px;
  }
  .section-header {
    padding: 24px 20px 12px;
  }
  .system-info {
    padding: 24px 20px 32px;
  }
  .info-grid {
    grid-template-columns: 1fr;
  }
}
</style>
