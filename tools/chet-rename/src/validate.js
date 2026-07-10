/**
 * 编译校验：替换完成后跑 dotnet build 确认无断裂
 */
import { exec } from 'node:child_process';
import { promisify } from 'node:util';
import path from 'node:path';
import { promises as fs } from 'node:fs';

const execAsync = promisify(exec);

/**
 * 查找后端 slnx 文件
 * @param {string} rootDir  项目根目录
 * @returns {Promise<string|null>}
 */
export async function findSlnx(rootDir) {
  const candidates = [
    path.join(rootDir, 'Chet.Admin.Api', 'Chet.Admin.slnx'),
  ];

  // 如果已重命名，通配查找
  try {
    const apiDir = path.join(rootDir, 'Chet.Admin.Api');
    const entries = await fs.readdir(apiDir, { withFileTypes: true });
    for (const e of entries) {
      if (e.isFile() && e.name.endsWith('.slnx')) {
        return path.join(apiDir, e.name);
      }
    }
  } catch {
    // 目录可能已重命名，尝试在根目录下查找
  }

  // 兜底：递归查找第一个 .slnx
  try {
    const result = await findFirst(rootDir, '.slnx');
    if (result) return result;
  } catch {
    // ignore
  }

  return candidates[0] ?? null;
}

async function findFirst(dir, ext, depth = 0) {
  if (depth > 3) return null;
  const entries = await fs.readdir(dir, { withFileTypes: true });
  for (const e of entries) {
    if (e.isFile() && e.name.endsWith(ext)) {
      return path.join(dir, e.name);
    }
  }
  for (const e of entries) {
    if (e.isDirectory() && !['node_modules', 'bin', 'obj', '.git', 'dist'].includes(e.name)) {
      const found = await findFirst(path.join(dir, e.name), ext, depth + 1);
      if (found) return found;
    }
  }
  return null;
}

/**
 * 运行 dotnet build 校验
 * @param {string} slnxPath
 * @returns {Promise<{success: boolean, output: string}>}
 */
export async function validateDotnetBuild(slnxPath) {
  try {
    const { stdout, stderr } = await execAsync(
      `dotnet build "${slnxPath}" -c Debug --nologo -v minimal`,
      { maxBuffer: 10 * 1024 * 1024, cwd: path.dirname(slnxPath) },
    );
    const output = (stdout + stderr).trim();
    const success = output.includes('已成功生成') || output.includes('Build succeeded') || output.includes('0 个错误');
    return { success, output };
  } catch (e) {
    return { success: false, output: (e.stdout || '') + (e.stderr || '') + (e.message || '') };
  }
}
