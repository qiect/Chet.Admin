/**
 * 命名风格派生与替换映射表
 *
 * 核心原则：
 * 1. 用户只输入一个 PascalCase 名字（如 MyApp.Admin），自动派生所有风格
 * 2. 替换列表按 token 长度降序排列，避免短 token 破坏长 token
 * 3. 只替换完整的、确定的 token，不替换单独的 "Chet"
 */

/**
 * 从 PascalCase 输入派生所有命名风格
 * @param {string} pascalInput  用户输入，如 "MyApp.Admin"
 * @param {string} githubRepo   GitHub 仓库路径，如 "mycompany/MyApp.Admin"
 * @returns {{pascal, display, kebab, pascalNoDot, githubUser, github} & {replacements: Array<[string,string]>}}
 */
export function deriveNames(pascalInput, githubRepo) {
  const parts = pascalInput.split('.');           // ['MyApp', 'Admin']
  const pascalNoDot = parts.join('');               // MyAppAdmin
  const githubUser = githubRepo.split('/')[0] || 'yourname';

  const names = {
    pascal: pascalInput,                            // MyApp.Admin
    display: parts.join(' '),                       // MyApp Admin
    kebab: parts.join('-').toLowerCase(),            // my-app
    pascalNoDot,                                    // MyAppAdmin
    githubUser,                                     // mycompany
    github: githubRepo,                             // mycompany/MyApp.Admin
  };

  // 替换映射表（按长度降序，避免子串冲突）
  // 每项 [oldToken, newToken, description]
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

  return { ...names, replacements };
}
