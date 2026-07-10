#!/usr/bin/env node
/**
 * chet-rename — Chet.Admin 项目重命名工具
 *
 * 用法：
 *   node bin.mjs                 交互式执行
 *   node bin.mjs --dry-run       只预览不写入
 *   node bin.mjs --name MyApp.Admin --github user/MyApp.Admin   非交互式
 */
import readline from 'node:readline';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { promises as fs } from 'node:fs';

import { deriveNames } from './src/names.js';
import {
  scanFiles, replaceAllFiles,
  collectFileRenames, collectDirRenames,
  executeFileRenames, executeDirRenames,
} from './src/fs.js';
import { findSlnx, validateDotnetBuild } from './src/validate.js';
import { log, banner, printMappingPreview, printDryRunReport } from './src/log.js';

// ─── 参数解析 ────────────────────────────────────────────────

const args = process.argv.slice(2);
const dryRun = args.includes('--dry-run');
const nameIdx = args.indexOf('--name');
const githubIdx = args.indexOf('--github');

const cliName = nameIdx >= 0 ? args[nameIdx + 1] : null;
const cliGithub = githubIdx >= 0 ? args[githubIdx + 1] : null;

// ─── 交互式提问 ──────────────────────────────────────────────

function ask(rl, question, defaultValue = '') {
  return new Promise((resolve) => {
    const hint = defaultValue ? ` ${'(默认: ' + defaultValue + ')'}` : '';
    rl.question(`? ${question}${hint}: `, (answer) => {
      resolve((answer || '').trim() || defaultValue);
    });
  });
}

async function collectInput() {
  // 非交互模式
  if (cliName) {
    const pascal = cliName;
    const github = cliGithub || `${pascal.split('.')[0].toLowerCase()}/${pascal}`;
    return { pascal, github };
  }

  // 交互模式
  const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
  });

  try {
    const pascal = await ask(rl, '新项目名称 (PascalCase，可含点，如 MyApp.Admin)');
    if (!pascal) {
      log.error('项目名称不能为空');
      process.exit(1);
    }

    const parts = pascal.split('.');
    const defaultDisplay = parts.join(' ');
    const defaultKebab = parts.join('-').toLowerCase();
    const defaultGithub = `${parts[0].toLowerCase()}/${pascal}`;
    const defaultRedis = parts.join('');

    const github = await ask(rl, 'GitHub 仓库地址 (如 user/MyApp.Admin)', defaultGithub);

    // 展示派生值，让用户确认
    console.log(`\n  ${'项目名 (PascalCase)'.padEnd(22)}: ${pascal}`);
    console.log(`  ${'展示名'.padEnd(22)}: ${defaultDisplay}`);
    console.log(`  ${'前端 namespace'.padEnd(22)}: ${defaultKebab}`);
    console.log(`  ${'Redis 前缀'.padEnd(22)}: ${defaultRedis}:`);
    console.log(`  ${'GitHub 仓库'.padEnd(22)}: ${github}\n`);

    const confirm = await ask(rl, '确认以上信息？(y/N)', 'N');
    if (confirm.toLowerCase() !== 'y') {
      log.warn('已取消');
      process.exit(0);
    }

    return { pascal, github };
  } finally {
    rl.close();
  }
}

// ─── 主流程 ──────────────────────────────────────────────────

async function main() {
  banner('Chet.Admin Project Renamer  v1.0');

  const { pascal, github } = await collectInput();
  const names = deriveNames(pascal, github);

  printMappingPreview(names.replacements);

  // 项目根目录：chet-rename 的上两级
  const scriptDir = path.dirname(fileURLToPath(import.meta.url));
  const rootDir = path.resolve(scriptDir, '..', '..');

  log.step(`扫描项目目录: ${rootDir}`);
  const files = await scanFiles(rootDir);
  log.info(`找到 ${files.length} 个待检查文件`);

  // Step 1: 替换文件内容
  log.step('替换文件内容...');
  const replaceResult = await replaceAllFiles(files, names.replacements, dryRun);
  log.success(`${dryRun ? '[DRY RUN] ' : ''}${replaceResult.changedFiles} 个文件改动，${replaceResult.totalMatches} 处匹配`);

  // Step 2: 收集文件重命名
  log.step('收集文件重命名...');
  const fileRenames = collectFileRenames(files, names.replacements);
  log.info(`${fileRenames.length} 个文件需要重命名`);

  // Step 3: 收集文件夹重命名
  log.step('收集文件夹重命名...');
  const dirRenames = await collectDirRenames(rootDir, names.replacements);
  log.info(`${dirRenames.length} 个文件夹需要重命名`);

  // dry-run: 打印报告后退出
  if (dryRun) {
    printDryRunReport({
      changedFiles: replaceResult.changedFiles,
      totalMatches: replaceResult.totalMatches,
      details: replaceResult.details,
      fileRenames: fileRenames.length,
      fileRenamesList: fileRenames,
      dirRenames: dirRenames.length,
      dirRenamesList: dirRenames,
    });

    // dry-run 下也校验当前编译状态（替换未写入，检查的是原始项目）
    log.dim('(dry-run 模式不执行编译校验)');
    return;
  }

  // Step 4: 执行文件重命名
  if (fileRenames.length > 0) {
    log.step('执行文件重命名...');
    const fileCount = await executeFileRenames(fileRenames);
    log.success(`${fileCount} 个文件已重命名`);
  }

  // Step 5: 执行文件夹重命名（叶子优先）
  if (dirRenames.length > 0) {
    log.step('执行文件夹重命名（叶子优先）...');
    const dirCount = await executeDirRenames(dirRenames);
    log.success(`${dirCount} 个文件夹已重命名`);
  }

  // Step 6: 编译校验
  log.step('编译校验...');
  const slnxPath = await findSlnx(rootDir);
  if (!slnxPath) {
    log.warn('未找到 .slnx 文件，跳过编译校验');
  } else {
    log.dim(`  校验: ${slnxPath}`);
    const result = await validateDotnetBuild(slnxPath);
    if (result.success) {
      log.success('编译通过，0 错误');
    } else {
      log.error('编译失败，请检查以下输出：');
      console.log(result.output.split('\n').slice(0, 30).join('\n'));
      log.warn('可以运行 git checkout . 回滚，或手动检查问题');
    }
  }

  // 完成
  banner(`完成！新项目已就绪：${pascal}`);
  console.log('  下一步：');
  console.log(`    cd ${path.basename(rootDir)}`);
  console.log(`    dotnet build  # 后端编译`);
  console.log(`    cd Chet.Admin.Web && pnpm install && pnpm dev:antd  # 前端启动`);
  console.log('');
}

main().catch((e) => {
  log.error(e.message);
  console.error(e.stack);
  process.exit(1);
});
