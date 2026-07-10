/**
 * 文件系统操作：扫描、内容替换、文件/文件夹重命名
 *
 * 执行顺序（避免路径失效）：
 *   1. 扫描所有待处理文件
 *   2. 替换文件内容（此时路径还没变，引用不会断）
 *   3. 重命名文件（.csproj / .db / .slnx 等）
 *   4. 重命名文件夹（从最深的叶子目录向根目录逐层改）
 */
import { promises as fs } from 'node:fs';
import path from 'node:path';

// ─── 跳过规则 ────────────────────────────────────────────────

/** 跳过的目录名（构建产物 / 版本控制 / 依赖 / 工具自身） */
const SKIP_DIRS = new Set([
  'node_modules', 'bin', 'obj', '.git', '.vs', 'dist',
  '.turbo', '.cache', 'coverage', 'tools',
]);

/** 待处理的文件扩展名白名单 */
const INCLUDE_EXTENSIONS = new Set([
  '.cs', '.csproj', '.slnx', '.sln', '.json', '.env',
  '.vue', '.ts', '.tsx', '.js', '.mjs', '.md', '.yml', '.yaml',
  '.html', '.config', '.xml', '.sh', '.dockerfile',
]);

/** 无扩展名但需要处理的特殊文件名 */
const INCLUDE_FILENAMES = new Set([
  'Dockerfile', 'dockerfile', '.env.development', '.env.production',
  '.env.analyze', '.gitignore', '.dockerignore',
]);

/**
 * 判断文件是否应该被处理
 */
function shouldInclude(filePath) {
  const basename = path.basename(filePath);
  if (INCLUDE_FILENAMES.has(basename)) return true;
  const ext = path.extname(filePath).toLowerCase();
  return INCLUDE_EXTENSIONS.has(ext);
}

/**
 * 判断目录是否应该被跳过
 */
function shouldSkipDir(dirName) {
  return SKIP_DIRS.has(dirName);
}

// ─── 文件扫描 ────────────────────────────────────────────────

/**
 * 递归扫描目录，返回所有待处理的文件路径
 * @param {string} rootDir  扫描根目录
 * @returns {Promise<string[]>}  绝对路径列表
 */
export async function scanFiles(rootDir) {
  const results = [];

  async function walk(dir) {
    let entries;
    try {
      entries = await fs.readdir(dir, { withFileTypes: true });
    } catch {
      return; // 无权限或不存在
    }

    for (const entry of entries) {
      const fullPath = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        if (!shouldSkipDir(entry.name)) {
          await walk(fullPath);
        }
      } else if (entry.isFile() && shouldInclude(fullPath)) {
        results.push(fullPath);
      }
    }
  }

  await walk(rootDir);
  return results;
}

// ─── 内容替换 ────────────────────────────────────────────────

/**
 * 对单个文件内容执行所有替换
 * @param {string} content   原始内容
 * @param {Array<[string,string]>} replacements  替换映射表
 * @returns {{content: string, changed: boolean, matchCount: number}}
 */
export function replaceContent(content, replacements) {
  let result = content;
  let matchCount = 0;

  for (const [oldToken, newToken] of replacements) {
    if (result.includes(oldToken)) {
      const count = result.split(oldToken).length - 1;
      result = result.split(oldToken).join(newToken);
      matchCount += count;
    }
  }

  return { content: result, changed: matchCount > 0, matchCount };
}

/**
 * 批量替换文件内容
 * @param {string[]} files  文件路径列表
 * @param {Array<[string,string]>} replacements  替换映射表
 * @param {boolean} dryRun  只报告不写入
 * @returns {Promise<{changedFiles: number, totalMatches: number, details: Array}>}
 */
export async function replaceAllFiles(files, replacements, dryRun = false) {
  let changedFiles = 0;
  let totalMatches = 0;
  const details = [];

  for (const filePath of files) {
    let content;
    try {
      content = await fs.readFile(filePath, 'utf-8');
    } catch {
      continue; // 二进制文件或无权限，跳过
    }

    const { content: newContent, changed, matchCount } = replaceContent(content, replacements);
    if (!changed) continue;

    changedFiles++;
    totalMatches += matchCount;
    details.push({ file: filePath, matchCount });

    if (!dryRun) {
      await fs.writeFile(filePath, newContent, 'utf-8');
    }
  }

  return { changedFiles, totalMatches, details };
}

// ─── 文件 / 文件夹重命名 ─────────────────────────────────────

/**
 * 对文件名（basename）执行替换，保持目录路径不变
 * 目录路径的变化由 executeDirRenames 处理，避免目录不存在导致 rename 失败
 * @param {string} filePath  原始路径
 * @param {Array<[string,string]>} replacements  替换映射表
 * @returns {string}  替换文件名后的路径（目录不变）
 */
function replaceFileNameOnly(filePath, replacements) {
  const dir = path.dirname(filePath);
  const oldName = path.basename(filePath);
  let newName = oldName;
  for (const [oldToken, newToken] of replacements) {
    if (newName.includes(oldToken)) {
      newName = newName.split(oldToken).join(newToken);
    }
  }
  return newName === oldName ? filePath : path.join(dir, newName);
}

/**
 * 收集需要重命名的文件（仅文件名含目标 token，如 .csproj / .slnx / .db）
 * 注意：只改文件名，不改目录路径。目录路径由 executeDirRenames 后续处理
 * @param {string[]} files  所有文件路径
 * @param {Array<[string,string]>} replacements
 * @returns {Array<{from: string, to: string}>}
 */
export function collectFileRenames(files, replacements) {
  const renames = [];
  for (const filePath of files) {
    const newPath = replaceFileNameOnly(filePath, replacements);
    if (newPath !== filePath) {
      renames.push({ from: filePath, to: newPath });
    }
  }
  return renames;
}

/**
 * 收集需要重命名的文件夹（目录名含目标 token）
 * @param {string} rootDir  扫描根目录
 * @param {Array<[string,string]>} replacements
 * @returns {Promise<Array<{from: string, to: string}>>}  按深度降序排列（叶子优先）
 */
export async function collectDirRenames(rootDir, replacements) {
  const dirsToRename = [];

  async function walk(dir) {
    let entries;
    try {
      entries = await fs.readdir(dir, { withFileTypes: true });
    } catch {
      return;
    }

    for (const entry of entries) {
      if (!entry.isDirectory() || shouldSkipDir(entry.name)) continue;
      const fullPath = path.join(dir, entry.name);
      await walk(fullPath); // 先递归子目录（保证叶子优先收集）

      // 检查当前目录名是否含目标 token
      let newName = entry.name;
      for (const [oldToken, newToken] of replacements) {
        newName = newName.split(oldToken).join(newToken);
      }
      if (newName !== entry.name) {
        dirsToRename.push({ from: fullPath, to: path.join(dir, newName) });
      }
    }
  }

  await walk(rootDir);
  // walk 是先递归再收集，所以 dirsToRename 已经是按深度降序（叶子优先）
  return dirsToRename;
}

/**
 * 执行文件重命名
 * @param {Array<{from:string,to:string}>} renames
 * @param {boolean} dryRun
 */
export async function executeFileRenames(renames, dryRun = false) {
  if (dryRun) return renames.length;
  let count = 0;
  for (const { from, to } of renames) {
    try {
      await fs.rename(from, to);
      count++;
    } catch (e) {
      console.error(`  文件重命名失败: ${from} -> ${to}\n  ${e.message}`);
    }
  }
  return count;
}

/**
 * 执行文件夹重命名（叶子优先，避免父目录先改导致子路径失效）
 * @param {Array<{from:string,to:string}>} renames  已按深度降序排列
 * @param {boolean} dryRun
 */
export async function executeDirRenames(renames, dryRun = false) {
  if (dryRun) return renames.length;
  let count = 0;
  for (const { from, to } of renames) {
    try {
      await fs.rename(from, to);
      count++;
    } catch (e) {
      console.error(`  文件夹重命名失败: ${from} -> ${to}\n  ${e.message}`);
    }
  }
  return count;
}
