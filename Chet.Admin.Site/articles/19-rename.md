# Chet.Admin 实用技巧：一键把项目重命名成你的品牌 ✏️

> 《Chet.Admin 全栈实战》系列第 19 篇

---

## 前言

很多同学 fork 开源项目做二次开发时，都会遇到一个尴尬：

- ❌ 代码里到处都是 `Chet.Admin`、`ChetAdmin`、`chet-admin`
- ❌ namespace、csproj、文件夹名、Docker 镜像名全要改
- ❌ 手动改一遍要 2 小时，漏改一处就编译挂掉
- ❌ 改完发现 README、Swagger 标题、Redis 前缀还藏着旧名字

**Chet.Admin** 内置了一个零依赖重命名工具 `chet-rename`，**5 秒搞定**所有改名工作。

这一篇就带你拆解它的实现思路和使用方法。🔧

---

## 一、为什么需要重命名

### 1.1 fork 二开场景

开源项目 fork 之后，通常要改造成自己的产品：

| 场景 | 旧名 | 新名 |
| ---- | ---- | ---- |
| 公司内部系统 | `Chet.Admin` | `CompanyX.Admin` |
| 商业产品 | `Chet.Admin` | `AcmeAdmin` |
| 个人项目 | `Chet.Admin` | `MyBlog.Admin` |

改名涉及的位置极多：

- **C# namespace**：`namespace Chet.Admin.Services`
- **csproj 文件名**：`Chet.Admin.Services.csproj`
- **解决方案文件**：`Chet.Admin.slnx`
- **文件夹**：`Chet.Admin.Api/`、`Chet.Admin.Web/`
- **Docker 镜像名**：`chet-admin-api`
- **Redis 前缀**：`ChetAdmin:`
- **数据库文件**：`Chet.Admin.db`
- **GitHub 仓库路径**：`qiect/Chet.Admin`
- **前端 namespace**：`chet-admin`
- **展示名**：`Chet Admin`

**手动改？想都别想**。

### 1.2 自动化的挑战

看似简单的"全文替换"，实际坑不少：

1. **token 长度冲突**：`ChetAdmin` 是 `Chet.Admin` 的子串，先替换 `Chet` 会破坏 `Chet.Admin`
2. **路径失效**：先改父文件夹，子路径就找不到文件了
3. **跳过规则**：`node_modules`、`bin`、`obj`、`.git` 不能动
4. **扩展名过滤**：只改代码文件，图片二进制不能碰
5. **编译验证**：改完跑一下 `dotnet build` 确认没断裂

`chet-rename` 一步步把这些坑都填平了。

---

## 二、工具位置与依赖

工具位于项目根目录 `tools/chet-rename/`：

```
tools/chet-rename/
├── bin.mjs              # 入口脚本
├── package.json
└── src/
    ├── names.js         # 命名风格派生
    ├── fs.js            # 文件系统操作
    ├── validate.js      # 编译校验
    └── log.js           # 彩色日志
```

### 2.1 零依赖

看一眼 `package.json`：

```json
{
  "name": "chet-rename",
  "version": "1.0.0",
  "description": "Chet.Admin 项目重命名工具，一键替换所有命名风格",
  "type": "module",
  "bin": {
    "chet-rename": "bin.mjs"
  },
  "engines": {
    "node": ">=18.0.0"
  },
  "scripts": {
    "start": "node bin.mjs",
    "dry-run": "node bin.mjs --dry-run"
  }
}
```

- **`"type": "module"`**：用原生 ESM
- **没有任何 dependencies**：纯 Node.js 内置模块
- **Node 18+**：用到了 `fs/promises`、`readline` 等内置能力

装个 Node 就能跑，不用 `npm install`。⚡

---

## 三、命名风格派生：names.js

用户只输入一个 **PascalCase** 名字，工具自动派生所有命名风格。

### 3.1 输入输出

```javascript
deriveNames('MyApp.Admin', 'mycompany/MyApp.Admin');
```

派生结果：

| 字段 | 值 |
| ---- | ---- |
| `pascal` | `MyApp.Admin` |
| `display` | `MyApp Admin` |
| `kebab` | `myapp-admin` |
| `pascalNoDot` | `MyAppAdmin` |
| `githubUser` | `mycompany` |
| `github` | `mycompany/MyApp.Admin` |

### 3.2 替换映射表

核心是一张 **`replacements` 映射表**，按 token 长度**降序**排列：

```javascript
const replacements = [
  // --- 最长 / 最具体的 token 优先 ---
  ['ChetWebApiTemplate', names.pascalNoDot, '历史遗留值（文档中）'],
  ['qiect/Chet.Admin', names.github, 'GitHub 仓库路径'],
  ['Chet.Admin.db', `${names.pascal}.db`, '数据库文件名'],
  ['chet-admin-api', `${names.kebab}-api`, 'docker 镜像名'],

  // --- 中等长度 token ---
  ['chet-webapi', `${names.kebab}-api`, 'docker 容器名'],
  ['Chet.Admin', names.pascal, 'namespace / csproj / 路径'],
  ['Chet Admin', names.display, '展示名'],
  ['ChetAdmin:', `${names.pascalNoDot}:`, 'Redis InstanceName'],
  ['chet-redis', `${names.kebab}-redis`, 'docker 容器名'],
  ['chet-admin', names.kebab, '前端 namespace'],

  // --- 短 token 最后（此时长的已被替换完，不会误伤）---
  ['ChetAdmin', names.pascalNoDot, 'Redis 前缀（无冒号）'],
  ['qiect', names.githubUser, 'GitHub 用户名'],
];
```

### 3.3 为什么按长度降序

假设不排序，先替换短 token `ChetAdmin` → `MyAppAdmin`：

- 文件里的 `ChetAdmin:User` 会变成 `MyAppAdmin:User` ✅
- 但是 `Chet.Admin.Services` 里的 `ChetAdmin` 不存在，不会被替换（注意这里是 `Chet.Admin` 带点）

再考虑 `qiect/Chet.Admin`：
- 如果先替换 `qiect` → `mycompany`，得到 `mycompany/Chet.Admin`
- 再替换 `Chet.Admin` → `MyApp.Admin`，得到 `mycompany/MyApp.Admin` ✅

但如果反过来：
- 先替换 `Chet.Admin` → `MyApp.Admin`，原句变成 `qiect/MyApp.Admin`
- 这时 `qiect/Chet.Admin` 已经不存在了，无法匹配 ❌

**长 token 先替换**，保证精准命中。

---

## 四、文件系统操作：fs.js

### 4.1 跳过规则

不是所有文件都该改，工具内置了两张白名单：

```javascript
// 跳过的目录
const SKIP_DIRS = new Set([
  'node_modules', 'bin', 'obj', '.git', '.vs', 'dist',
  '.turbo', '.cache', 'coverage', 'tools',
]);

// 待处理的文件扩展名
const INCLUDE_EXTENSIONS = new Set([
  '.cs', '.csproj', '.slnx', '.sln', '.json', '.env',
  '.vue', '.ts', '.tsx', '.js', '.mjs', '.md', '.yml', '.yaml',
  '.html', '.config', '.xml', '.sh', '.dockerfile',
]);

// 无扩展名但需要处理的特殊文件名
const INCLUDE_FILENAMES = new Set([
  'Dockerfile', 'dockerfile', '.env.development', '.env.production',
  '.env.analyze', '.gitignore', '.dockerignore',
]);
```

- **`SKIP_DIRS`**：构建产物、版本控制、依赖目录全跳过
- **`tools` 自己也跳过**：避免改工具自己
- **`INCLUDE_EXTENSIONS`**：所有代码与配置文件
- **`INCLUDE_FILENAMES`**：Dockerfile 这类无后缀的关键文件

### 4.2 执行顺序（关键！）

工具按这个顺序执行，**每一步都为下一步铺路**：

```
1. 扫描所有待处理文件       → 此时路径还没变
2. 替换文件内容            → 路径没变，引用不会断
3. 重命名文件              → .csproj / .db / .slnx
4. 重命名文件夹（叶子优先）  → 从最深的目录向根逐层改
```

为什么是这个顺序？

- **先改内容再改路径**：内容里的引用还是旧路径，但旧路径此刻还存在，能正确写入
- **文件改名先于文件夹改名**：文件夹改了，旧文件路径就失效了
- **文件夹改名从叶子开始**：先改 `Chet.Admin.Api/Chet.Admin.Api/Controllers/` 这种深层目录，再改根目录

### 4.3 文件夹重命名：叶子优先

看 `collectDirRenames` 的实现：

```javascript
export async function collectDirRenames(rootDir, replacements) {
  const dirsToRename = [];

  async function walk(dir) {
    // ...
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
```

**先递归再收集**，这样得到的列表天然就是"叶子目录在前面"，执行重命名时不会因为父目录先改了导致子路径失效。

---

## 五、使用方法

### 5.1 第一步：fork 仓库

```bash
# 在 GitHub 上 fork Chet.Admin 到自己账号
# 然后 clone 下来
git clone https://github.com/yourname/Chet.Admin.git
cd Chet.Admin
```

> ⚠️ **不要在原始 `Chet.Admin` 仓库上执行**，脚本会把项目自身改名。正确用法是在复制出的新仓库中运行。

### 5.2 第二步：预览（dry-run）

**强烈建议先跑一次 `--dry-run`**：

```bash
node tools/chet-rename/bin.mjs --dry-run --name MyApp.Admin --github mycompany/MyApp.Admin
```

`dry-run` 模式不会写入任何文件，只打印：

- 命名映射预览表
- 将要修改的文件数量与匹配数
- 将要重命名的文件 / 文件夹清单

<!-- dry-run 输出截图 -->
![dry-run 输出](/screenshots/dry-run-output.svg)

输出示例：

```
╭── Chet.Admin Project Renamer  v1.0 ───────────────────╮

  ─── 映射预览 ───────────────────────────────────
  ChetWebApiTemplate     → MyAppAdmin              历史遗留值（文档中）
  qiect/Chet.Admin       → mycompany/MyApp.Admin   GitHub 仓库路径
  Chet.Admin.db          → MyApp.Admin.db          数据库文件名
  chet-admin-api         → myapp-admin-api         docker 镜像名
  chet-webapi            → myapp-admin-api         docker 容器名
  Chet.Admin             → MyApp.Admin             namespace / csproj / 路径
  Chet Admin             → MyApp Admin             展示名
  ChetAdmin:             → MyAppAdmin:             Redis InstanceName
  chet-redis             → myapp-admin-redis       docker 容器名
  chet-admin             → myapp-admin             前端 namespace
  ChetAdmin              → MyAppAdmin              Redis 前缀（无冒号）
  qiect                  → mycompany               GitHub 用户名
  ────────────────────────────────────────────────

▸ 扫描项目目录: /path/to/project
ℹ 找到 312 个待检查文件
▸ 替换文件内容...
✓ [DRY RUN] 287 个文件改动，1542 处匹配
...
```

确认无误后再执行下一步。

### 5.3 第三步：交互式执行（推荐）

```bash
node tools/chet-rename/bin.mjs
```

按提示输入：

```
? 新项目名称 (PascalCase，可含点，如 MyApp.Admin): MyApp.Admin
? GitHub 仓库地址 (如 user/MyApp.Admin) (默认: mycompany/MyApp.Admin): mycompany/MyApp.Admin

  项目名 (PascalCase)    : MyApp.Admin
  展示名                 : MyApp Admin
  前端 namespace         : myapp-admin
  Redis 前缀             : MyAppAdmin:
  GitHub 仓库            : mycompany/MyApp.Admin

? 确认以上信息？(y/N): y
```

回车确认后，工具会自动完成：

1. ✅ 替换所有文件内容（namespace、配置、文档等）
2. ✅ 重命名文件（`.csproj`、`.slnx`、`.db`）
3. ✅ 重命名文件夹（从最深层向根目录逐层改）
4. ✅ 自动运行 `dotnet build` 校验

### 5.4 第四步：非交互式（CI 友好）

```bash
node tools/chet-rename/bin.mjs --name MyApp.Admin --github mycompany/MyApp.Admin
```

一行命令搞定，适合在 CI/CD 中自动初始化项目。

---

## 六、日志输出：log.js

工具自带终端彩色日志，**零依赖用 ANSI 转义码**实现：

```javascript
const RESET = '\x1b[0m';
const BOLD = '\x1b[1m';
const RED = '\x1b[31m';
const GREEN = '\x1b[32m';
const YELLOW = '\x1b[33m';
const CYAN = '\x1b[36m';

export const log = {
  info: (msg) => console.log(`${BLUE}ℹ${RESET} ${msg}`),
  success: (msg) => console.log(`${GREEN}✓${RESET} ${msg}`),
  warn: (msg) => console.log(`${YELLOW}⚠${RESET} ${msg}`),
  error: (msg) => console.log(`${RED}✗${RESET} ${msg}`),
  step: (msg) => console.log(`${CYAN}▸${RESET} ${msg}`),
};
```

执行过程会看到：

```
▸ 扫描项目目录: /path/to/MyApp.Admin
ℹ 找到 312 个待检查文件
▸ 替换文件内容...
✓ 287 个文件改动，1542 处匹配
▸ 收集文件重命名...
ℹ 11 个文件需要重命名
▸ 收集文件夹重命名...
ℹ 8 个文件夹需要重命名
▸ 执行文件重命名...
✓ 11 个文件已重命名
▸ 执行文件夹重命名（叶子优先）...
✓ 8 个文件夹已重命名
▸ 编译校验...
✓ 编译通过，0 错误
```

清清楚楚，每一步都看得见。👀

---

## 七、编译校验：validate.js

改名完了怎么知道有没有改坏？**跑一次 `dotnet build`**。

```javascript
export async function validateDotnetBuild(slnxPath) {
  try {
    const { stdout, stderr } = await execAsync(
      `dotnet build "${slnxPath}" -c Debug --nologo -v minimal`,
      { maxBuffer: 10 * 1024 * 1024, cwd: path.dirname(slnxPath) },
    );
    const output = (stdout + stderr).trim();
    const success = output.includes('已成功生成')
      || output.includes('Build succeeded')
      || output.includes('0 个错误');
    return { success, output };
  } catch (e) {
    return { success: false, output: (e.stdout || '') + (e.stderr || '') + (e.message || '') };
  }
}
```

- 自动查找 `.slnx` 文件
- 用 `child_process.exec` 调 `dotnet build`
- 兼容中英文输出（`已成功生成` / `Build succeeded`）
- 失败时打印前 30 行错误日志

校验失败时会提示：

```
✗ 编译失败，请检查以下输出：
...
⚠ 可以运行 git checkout . 回滚，或手动检查问题
```

---

## 八、验证清单

跑完工具后，按这份清单检查：

### 8.1 后端编译

```bash
dotnet build MyApp.Admin.Api/MyApp.Admin.slnx
```

应看到 `已成功生成` + `0 个错误`。

### 8.2 后端启动

```bash
cd MyApp.Admin.Api/MyApp.Admin.Api
dotnet run
```

访问 `http://localhost:5000/swagger`，确认：

- ✅ Swagger 标题显示为新项目名
- ✅ JWT Issuer/Audience 已替换
- ✅ 数据库文件名为 `MyApp.Admin.db`

### 8.3 前端启动

```bash
cd MyApp.Admin.Web
pnpm install
pnpm dev:antd
```

访问 `http://localhost:5666/`，确认：

- ✅ 浏览器标签标题为新项目展示名
- ✅ 登录页 Logo 旁文字为新项目展示名
- ✅ 登录后 Dashboard 欢迎语包含新项目展示名

默认账号：`admin@example.com / Admin@123`。

### 8.4 Docker 镜像

```bash
docker compose -f MyApp.Admin.Api/docker-compose.yml config
```

确认容器名、镜像名已替换为 `myapp-admin-api`、`myapp-admin-redis`。

---

## 九、Git 远程仓库迁移

fork 的仓库要改成自己的远程仓库：

### 9.1 在 GitHub/Gitee 创建新仓库

假设你的新仓库是 `mycompany/MyApp.Admin`。

### 9.2 修改远程地址

```bash
# 查看当前远程
git remote -v
# origin  https://github.com/qiect/Chet.Admin.git (fetch)
# origin  https://github.com/qiect/Chet.Admin.git (push)

# 改成自己的远程
git remote set-url origin https://github.com/mycompany/MyApp.Admin.git

# 确认
git remote -v
# origin  https://github.com/mycompany/MyApp.Admin.git (fetch)
# origin  https://github.com/mycompany/MyApp.Admin.git (push)
```

### 9.3 推送

```bash
git add -A
git commit -m "Rename to MyApp.Admin"
git push -u origin main
```

### 9.4 同时配置 GitHub 和 Gitee

```bash
# 添加 Gitee 远程
git remote add gitee https://gitee.com/mycompany/MyApp.Admin.git

# 推送到两边
git push origin main
git push gitee main
```

### 9.5 保留 upstream（可选）

如果想以后还能拉取 Chet.Admin 的更新：

```bash
# 把原仓库设为 upstream
git remote add upstream https://github.com/qiect/Chet.Admin.git

# 以后合并上游更新
git fetch upstream
git merge upstream/main
```

> ⚠️ 合并上游更新时可能有冲突（因为命名空间全变了），需要手动解决。

<!-- Git 远程配置示意图 -->
![Git 远程配置](/screenshots/git-remote.svg)

---

## 十、常见问题

### Q1: 执行后 `dotnet build` 报错找不到 csproj？

通常是文件夹重命名顺序异常。运行回滚：

```bash
git checkout .
git clean -fd
```

然后重试。如仍失败，请到 GitHub 提 issue 并附上 `--dry-run` 输出。

### Q2: 前端启动后白屏或样式异常？

检查 `apps/web-antd/.env` 中的 `VITE_APP_NAMESPACE` 是否已替换为新值（如 `myapp-admin`）。**清空浏览器 localStorage** 后重试（Vben 会缓存旧 namespace）。

### Q3: Swagger 标题还是旧的？

后端编译缓存。删掉 `bin/` 和 `obj/` 目录后重新 `dotnet build`。

### Q4: 登录失败返回 401？

JWT Issuer/Audience 已替换为新项目名，前端只携带 token 无需改动。如仍 401，**确认后端已重启**并清空浏览器 localStorage。

### Q5: 想换一个名字重新来过？

```bash
git checkout .
git clean -fd
```

这会丢弃所有改动并删除未跟踪的文件，恢复到上次 commit 的状态。然后重新运行脚本。

### Q6: 改完名字后 Redis 缓存怎么处理？

Redis 里旧 namespace 的 key 不会自动清理，建议：

```bash
# 连接 Redis
redis-cli

# 删除旧 namespace 的所有 key
redis-cli --scan --pattern 'ChetAdmin:*' | xargs redis-cli del
```

新 key 会用新 namespace（如 `MyAppAdmin:*`）。

---

## 十一、设计回顾

### 11.1 为什么用 Node.js 而不是 C#

理论上用 C# 写个 Roslyn 分析器更"正规"，但：

- **Node.js 跨平台无依赖**：装个 Node 就能跑，不用配 .NET SDK
- **正则替换足够用**：项目命名风格固定，不需要语义分析
- **前端开发者也能改**：fork 项目的人多半懂 JS
- **代码量小**：核心 4 个文件，500 行代码搞定

### 11.2 为什么不用 sed/PowerShell 批量替换

| 方案 | 问题 |
| ---- | ---- |
| `sed -i` | 跨平台不一致（BSD sed 和 GNU sed 参数不同） |
| PowerShell | Windows 限定，跨平台麻烦 |
| VS Code 全局替换 | 文件夹改名搞不定，手动太累 |

自研脚本**一次开发，到处运行**。

### 11.3 扩展性

工具的 `replacements` 数组是数据驱动的，如果以后新增命名风格：

```javascript
// names.js 里加一行
['Chet.Admin.Tests', `${names.pascal}.Tests`, '测试项目命名'],
```

其他逻辑一行不用改。

---

## 下篇预告

下一篇是**系列完结篇**：如何把项目用 Docker + Nginx 一键部署上线。

> 📌 「Chet.Admin 部署指南：Docker + Nginx 一键上线 🐳」

---

## 开源地址

- **GitHub**：https://github.com/qiect/Chet.Admin
- **Gitee**：https://gitee.com/qiect/Chet.Admin

觉得有帮助的话，**点个 Star ⭐** 支持一下吧！你的 Star 是我持续更新的动力～

---

> 🔗 GitHub：https://github.com/qiect/Chet.Admin
> 🔗 Gitee：https://gitee.com/qiect/Chet.Admin
> ⭐ 觉得不错的话，点个 Star 支持一下吧！

`#ChetAdmin` `#全栈开发` `#.NET10` `#Node.js` `#项目重命名` `#开源工具`
