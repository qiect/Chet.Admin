<script lang="ts" setup>
import { ref, onMounted, onUnmounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { usePreferences } from '@vben/preferences';
import { IconifyIcon } from '@vben/icons';
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
  if (h < 6) return '夜深了';
  if (h < 9) return '早上好';
  if (h < 12) return '上午好';
  if (h < 14) return '中午好';
  if (h < 18) return '下午好';
  return '晚上好';
});

const currentTime = ref('');
const currentDate = ref('');

function updateTime() {
  const now = new Date();
  currentTime.value = now.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  const weekDays = ['星期日', '星期一', '星期二', '星期三', '星期四', '星期五', '星期六'];
  currentDate.value = now.toLocaleDateString('zh-CN', { year: 'numeric', month: 'long', day: 'numeric' }) + ' ' + weekDays[now.getDay()];
}

let timer: ReturnType<typeof setInterval>;

// SVG line chart computed
const chartWidth = 600;
const chartHeight = 200;
const chartPaddingX = 40;
const chartPaddingY = 20;

const trendPoints = computed(() => {
  const items = trendItems.value;
  if (!items.length) return '';

  const maxVal = Math.max(...items.map((i) => i.loginCount), 1);
  const plotW = chartWidth - chartPaddingX * 2;
  const plotH = chartHeight - chartPaddingY * 2;

  return items
    .map((item, idx) => {
      const x = chartPaddingX + (idx / Math.max(items.length - 1, 1)) * plotW;
      const y = chartPaddingY + plotH - (item.loginCount / maxVal) * plotH;
      return `${x},${y}`;
    })
    .join(' ');
});

const trendAreaPoints = computed(() => {
  const items = trendItems.value;
  if (!items.length) return '';

  const maxVal = Math.max(...items.map((i) => i.loginCount), 1);
  const plotW = chartWidth - chartPaddingX * 2;
  const plotH = chartHeight - chartPaddingY * 2;
  const bottomY = chartPaddingY + plotH;

  const points = items
    .map((item, idx) => {
      const x = chartPaddingX + (idx / Math.max(items.length - 1, 1)) * plotW;
      const y = chartPaddingY + plotH - (item.loginCount / maxVal) * plotH;
      return `${x},${y}`;
    })
    .join(' ');

  const lastX = chartPaddingX + plotW;
  const firstX = chartPaddingX;
  return `${firstX},${bottomY} ${points} ${lastX},${bottomY}`;
});

const trendLabels = computed(() => {
  const items = trendItems.value;
  if (!items.length) return [];
  const plotW = chartWidth - chartPaddingX * 2;
  return items.map((item, idx) => ({
    text: item.date,
    x: chartPaddingX + (idx / Math.max(items.length - 1, 1)) * plotW,
  }));
});

function formatTime(dateStr: string) {
  const d = new Date(dateStr);
  const now = new Date();
  const diff = now.getTime() - d.getTime();
  if (diff < 60000) return '刚刚';
  if (diff < 3600000) return `${Math.floor(diff / 60000)} 分钟前`;
  if (diff < 86400000) return `${Math.floor(diff / 3600000)} 小时前`;
  return `${Math.floor(diff / 86400000)} 天前`;
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
      getRecentLogsApi(5).catch(() => null),
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
  { icon: 'lucide:users', title: '用户管理', desc: '管理系统用户', path: '/system/user', color: '#6366f1' },
  { icon: 'lucide:shield', title: '角色管理', desc: '角色与权限配置', path: '/system/role', color: '#f59e0b' },
  { icon: 'lucide:menu', title: '菜单管理', desc: '菜单与路由配置', path: '/system/menu', color: '#10b981' },
  { icon: 'lucide:building', title: '部门管理', desc: '组织架构管理', path: '/system/department', color: '#3b82f6' },
  { icon: 'lucide:book-open', title: '字典管理', desc: '数据字典维护', path: '/system/dictionary', color: '#8b5cf6' },
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
          <h1 class="hero-title">{{ greeting }}，<span class="hero-highlight">管理员</span></h1>
          <p class="hero-subtitle">欢迎回到 Chet Admin 管理系统</p>
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
          <div class="stat-label">用户总数</div>
        </div>
      </div>
      <div class="stat-card" style="--accent: #f59e0b">
        <div class="stat-icon"><IconifyIcon icon="lucide:shield" width="24" /></div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.roleCount }}</div>
          <div class="stat-label">角色数量</div>
        </div>
      </div>
      <div class="stat-card" style="--accent: #10b981">
        <div class="stat-icon"><IconifyIcon icon="lucide:menu" width="24" /></div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.menuCount }}</div>
          <div class="stat-label">菜单项数</div>
        </div>
      </div>
      <div class="stat-card" style="--accent: #3b82f6">
        <div class="stat-icon"><IconifyIcon icon="lucide:building" width="24" /></div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.departmentCount }}</div>
          <div class="stat-label">部门数量</div>
        </div>
      </div>
      <div class="stat-card" style="--accent: #ef4444">
        <div class="stat-icon"><IconifyIcon icon="lucide:log-in" width="24" /></div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.todayLoginCount }}</div>
          <div class="stat-label">今日登录</div>
        </div>
      </div>
      <div class="stat-card" style="--accent: #8b5cf6">
        <div class="stat-icon"><IconifyIcon icon="lucide:activity" width="24" /></div>
        <div class="stat-info">
          <div class="stat-value">{{ stats.activeUserCount }}</div>
          <div class="stat-label">7日活跃</div>
        </div>
      </div>
    </div>

    <!-- 趋势图 + 最近操作 -->
    <div class="trend-logs-row">
      <!-- 趋势图 -->
      <div class="trend-card">
        <div class="card-header">
          <h3 class="card-title"><IconifyIcon icon="lucide:trending-up" width="18" class="card-title-icon" /> 登录趋势</h3>
          <span class="card-subtitle">近 7 日</span>
        </div>
        <div class="trend-chart-wrap">
          <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="trend-svg">
            <!-- Grid lines -->
            <line
              v-for="i in 4"
              :key="'grid-' + i"
              :x1="chartPaddingX"
              :y1="chartPaddingY + ((chartHeight - chartPaddingY * 2) / 4) * (i - 1)"
              :x2="chartWidth - chartPaddingX"
              :y2="chartPaddingY + ((chartHeight - chartPaddingY * 2) / 4) * (i - 1)"
              :stroke="isDark ? '#334155' : '#f1f5f9'"
              stroke-width="1"
            />
            <!-- Area fill -->
            <polygon
              v-if="trendAreaPoints"
              :points="trendAreaPoints"
              :fill="isDark ? 'rgba(96,165,250,0.12)' : 'rgba(59,130,246,0.08)'"
            />
            <!-- Line -->
            <polyline
              v-if="trendPoints"
              :points="trendPoints"
              fill="none"
              :stroke="isDark ? '#60a5fa' : '#3b82f6'"
              stroke-width="2.5"
              stroke-linecap="round"
              stroke-linejoin="round"
            />
            <!-- Data points -->
            <circle
              v-for="(item, idx) in trendItems"
              :key="'dot-' + idx"
              :cx="chartPaddingX + (idx / Math.max(trendItems.length - 1, 1)) * (chartWidth - chartPaddingX * 2)"
              :cy="chartPaddingY + (chartHeight - chartPaddingY * 2) - (item.loginCount / Math.max(...trendItems.map(i => i.loginCount), 1)) * (chartHeight - chartPaddingY * 2)"
              r="4"
              :fill="isDark ? '#60a5fa' : '#3b82f6'"
              :stroke="isDark ? '#1e293b' : '#ffffff'"
              stroke-width="2"
            />
            <!-- X axis labels -->
            <text
              v-for="(label, idx) in trendLabels"
              :key="'label-' + idx"
              :x="label.x"
              :y="chartHeight - 2"
              text-anchor="middle"
              :fill="isDark ? '#64748b' : '#94a3b8'"
              font-size="11"
            >
              {{ label.text }}
            </text>
          </svg>
          <div v-if="!trendItems.length" class="trend-empty">暂无趋势数据</div>
        </div>
      </div>

      <!-- 最近操作 -->
      <div class="logs-card">
        <div class="card-header">
          <h3 class="card-title"><IconifyIcon icon="lucide:scroll-text" width="18" class="card-title-icon" /> 最近操作</h3>
          <span class="card-subtitle">最近 5 条</span>
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
            <span>暂无操作记录</span>
          </div>
        </div>
      </div>
    </div>

    <!-- 快捷入口 -->
    <div class="section-header">
      <h2 class="section-title">快捷入口</h2>
      <p class="section-desc">快速访问常用功能模块</p>
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
          <div class="shortcut-title">{{ item.title }}</div>
          <div class="shortcut-desc">{{ item.desc }}</div>
        </div>
        <div class="shortcut-arrow"><IconifyIcon icon="lucide:arrow-right" width="16" /></div>
      </div>
    </div>

    <!-- 系统信息 -->
    <div class="system-info">
      <div class="info-card">
        <h3 class="info-title">系统信息</h3>
        <div class="info-grid">
          <div class="info-item"><span class="info-label">系统名称</span><span class="info-value">Chet Admin</span></div>
          <div class="info-item"><span class="info-label">框架版本</span><span class="info-value">Vben Admin v5.7</span></div>
          <div class="info-item"><span class="info-label">前端框架</span><span class="info-value">Vue 3 + TypeScript</span></div>
          <div class="info-item"><span class="info-label">UI 组件库</span><span class="info-value">Ant Design Vue</span></div>
          <div class="info-item"><span class="info-label">后端框架</span><span class="info-value">.NET Core WebAPI</span></div>
          <div class="info-item"><span class="info-label">数据库</span><span class="info-value">SQLite (EF Core)</span></div>
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
  height: 200px;
}

.trend-empty {
  text-align: center;
  padding: 40px 0;
  color: var(--text-muted);
  font-size: 14px;
}

/* ===== 最近操作 ===== */
.logs-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
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
