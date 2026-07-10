/**
 * 终端彩色日志（零依赖，ANSI 转义码）
 */

const RESET = '\x1b[0m';
const BOLD = '\x1b[1m';
const DIM = '\x1b[2m';
const RED = '\x1b[31m';
const GREEN = '\x1b[32m';
const YELLOW = '\x1b[33m';
const BLUE = '\x1b[34m';
const CYAN = '\x1b[36m';
const GRAY = '\x1b[90m';

export const log = {
  info: (msg) => console.log(`${BLUE}ℹ${RESET} ${msg}`),
  success: (msg) => console.log(`${GREEN}✓${RESET} ${msg}`),
  warn: (msg) => console.log(`${YELLOW}⚠${RESET} ${msg}`),
  error: (msg) => console.log(`${RED}✗${RESET} ${msg}`),
  step: (msg) => console.log(`${CYAN}▸${RESET} ${msg}`),
  dim: (msg) => console.log(`${DIM}${msg}${RESET}`),
  raw: (msg) => console.log(msg),
};

/** 打印横幅 */
export function banner(title) {
  const line = '─'.repeat(Math.max(title.length + 4, 50));
  console.log(`\n${CYAN}${BOLD}╭${line}╮${RESET}`);
  console.log(`${CYAN}${BOLD}│${RESET}  ${BOLD}${title}${RESET}${' '.repeat(Math.max(title.length + 4, 50) - title.length - 2)}${CYAN}${BOLD}│${RESET}`);
  console.log(`${CYAN}${BOLD}╰${line}╯${RESET}\n`);
}

/** 打印映射表预览 */
export function printMappingPreview(replacements) {
  console.log(`\n  ${DIM}─── 映射预览 ───────────────────────────────────${RESET}`);
  for (const [old, newVal, desc] of replacements) {
    console.log(`  ${YELLOW}${old.padEnd(22)}${RESET} → ${GREEN}${newVal.padEnd(22)}${RESET} ${GRAY}${desc}${RESET}`);
  }
  console.log(`  ${DIM}────────────────────────────────────────────────${RESET}\n`);
}

/** 打印 dry-run 报告 */
export function printDryRunReport(stats) {
  console.log(`\n  ${YELLOW}[DRY RUN]${RESET} 将要执行的操作：\n`);
  console.log(`  ${GRAY}文件内容替换：${RESET}${stats.changedFiles} 个文件，${stats.totalMatches} 处匹配`);
  console.log(`  ${GRAY}文件重命名：  ${RESET}${stats.fileRenames} 个`);
  console.log(`  ${GRAY}文件夹重命名：${RESET}${stats.dirRenames} 个\n`);

  if (stats.details.length > 0) {
    console.log(`  ${DIM}─── 内容变更明细（前 20 个） ──${RESET}`);
    const shown = stats.details.slice(0, 20);
    for (const d of shown) {
      const rel = d.file.replace(/\\/g, '/').split('/').slice(-3).join('/');
      console.log(`    ${GREEN}${d.matchCount}${RESET} 处  ${rel}`);
    }
    if (stats.details.length > 20) {
      console.log(`    ${DIM}... 还有 ${stats.details.length - 20} 个文件${RESET}`);
    }
  }

  if (stats.fileRenamesList.length > 0) {
    console.log(`\n  ${DIM}─── 文件重命名明细 ──${RESET}`);
    for (const { from, to } of stats.fileRenamesList.slice(0, 15)) {
      const fromShort = from.replace(/\\/g, '/').split('/').slice(-3).join('/');
      const toShort = to.replace(/\\/g, '/').split('/').slice(-3).join('/');
      console.log(`    ${fromShort}`);
      console.log(`    ${GRAY}→${RESET} ${GREEN}${toShort}${RESET}`);
    }
    if (stats.fileRenamesList.length > 15) {
      console.log(`    ${DIM}... 还有 ${stats.fileRenamesList.length - 15} 个${RESET}`);
    }
  }

  if (stats.dirRenamesList.length > 0) {
    console.log(`\n  ${DIM}─── 文件夹重命名明细 ──${RESET}`);
    for (const { from, to } of stats.dirRenamesList) {
      const fromShort = from.replace(/\\/g, '/').split('/').slice(-3).join('/');
      const toShort = to.replace(/\\/g, '/').split('/').slice(-3).join('/');
      console.log(`    ${fromShort}`);
      console.log(`    ${GRAY}→${RESET} ${GREEN}${toShort}${RESET}`);
    }
  }

  console.log(`\n  ${YELLOW}[DRY RUN]${RESET} 不会执行任何写操作。\n`);
}
