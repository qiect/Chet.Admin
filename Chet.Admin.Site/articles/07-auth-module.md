# Chet.Admin 模块详解①：认证登录全流程拆解 🔑

> 《Chet.Admin 全栈实战》系列第 7 篇

---

## 前言

**登录** 是每个系统的入口，也是最容易被低估的模块。

一个登录接口写好容易，但要做好下面这些事，得花点心思：

- 🔐 密码不能明文存，BCrypt 工作因子选多少合适？
- 🔐 失败几次锁定？锁定多久？
- 🔐 Token 过期了怎么无感刷新？被偷了怎么办？
- 🔐 密码用了 90 天，怎么强制用户改？
- 🔐 验证码什么时候弹？滑块还是图形？

**Chet.Admin** 的认证模块把这些问题都覆盖了，今天咱们一个个拆开看。

---

## 一、注册流程：从校验到入库 📝

### 1.1 RegisterDto：注册入参

注册只需要 3 个字段：

```csharp
public class RegisterDto
{
    /// 用户显示名称
    public required string Name { get; set; }

    /// 邮箱地址（登录凭证，唯一）
    public required string Email { get; set; }

    /// 登录密码（建议 12+ 字符）
    public required string Password { get; set; }
}
```

> 💡 **设计取舍**：注册字段尽量少，降低用户门槛。**邮箱作为唯一登录凭证**，后续不可修改（防止账号被换邮箱顶替）。

### 1.2 密码强度校验：BCrypt + 6 位下限

密码哈希由 `PasswordService` 完成，用的是 **BCrypt** 算法：

```csharp
public class PasswordService : IPasswordService
{
    /// 工作因子：2^12 = 4096 轮迭代，约 250ms
    private const int WorkFactor = 12;

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        // 强制下限：6 位
        if (password.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters", nameof(password));

        return global::BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return global::BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // 异常不外泄，统一返回 false
            return false;
        }
    }
}
```

**为什么选 BCrypt + 工作因子 12？**

| 工作因子 | 耗时 | 推荐场景 |
| ---- | ---- | ---- |
| 10 | ~100ms | 2010 年标准 |
| 11 | ~200ms | 一般应用 |
| **12** | **~250ms** | **Chet.Admin 默认（2024 推荐）** |
| 14 | ~1000ms | 高安全要求 |

> 🎯 BCrypt 的精髓：**内置盐值** + **自适应成本**。每年评估是否调高 WorkFactor，无需改业务代码就能抗硬件升级。

### 1.3 AuthService.RegisterAsync

注册业务流程：

```csharp
public async Task RegisterAsync(RegisterDto registerDto)
{
    _logger.LogInformation("User registration attempt: {Email}", registerDto.Email);

    // 1. 检查邮箱是否已存在
    var existingUser = await _unitOfWork.Users.GetByEmailAsync(registerDto.Email);
    if (existingUser != null)
    {
        throw new BadRequestException("Email already exists");
    }

    using var transaction = await _unitOfWork.BeginTransactionAsync();
    try
    {
        // 2. DTO 映射为实体
        var user = _mapper.Map<UserEntity>(registerDto);

        // 3. 密码哈希（不能用明文）
        user.PasswordHash = _passwordService.Hash(registerDto.Password);

        // 4. 记录密码修改时间（用于密码过期判断）
        user.PasswordChangedAt = DateTime.UtcNow;
        user.MustChangePassword = false;

        // 5. 入库
        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.CommitAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Registration failed for: {Email}", registerDto.Email);
        await _unitOfWork.RollbackAsync();
        throw;
    }
}
```

<!-- 注册流程图 -->
![注册流程](/screenshots/register-flow.svg)

---

## 二、登录流程：验证码 → 锁定 → 签发令牌 🚪

### 2.1 LoginDto：登录入参

```csharp
public class LoginDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }

    /// 验证码ID（失败 3 次后必填）
    public string? CaptchaId { get; set; }

    /// 验证码内容
    public string? CaptchaCode { get; set; }
}
```

### 2.2 LoginResponseDto：返回结构

登录响应里**不只有 Token**，还有几个关键状态字段：

```csharp
public class LoginResponseDto
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    /// 是否需要验证码（失败次数 >= 3 时为 true）
    public bool RequireCaptcha { get; set; }

    /// 锁定截止时间
    public DateTime? LockedUntil { get; set; }

    /// 是否需要强制修改密码（密码过期时为 true）
    public bool MustChangePassword { get; set; }
}
```

> 💡 这 4 个字段配合前端，能给出非常精准的提示：是验证码不对、账号锁定了、还是密码过期要改。

### 2.3 AuthController.Login：核心登录逻辑

控制器层做了**账号锁定判断**和**密码过期策略**：

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
{
    var user = await _unitOfWork.Users.GetByEmailAsync(loginDto.Email);

    // 1. 检查账号锁定状态
    if (user != null && user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
    {
        var remaining = user.LockedUntil.Value - DateTime.UtcNow;
        return Ok(ApiResponse.Error($"账号已锁定，请在 {Math.Ceiling(remaining.TotalMinutes)} 分钟后重试",
            StatusCodes.Status400BadRequest));
    }

    // 2. 锁定时间已过，重置失败计数
    if (user != null && user.LockedUntil.HasValue && user.LockedUntil.Value <= DateTime.UtcNow)
    {
        user.LoginFailCount = 0;
        user.LockedUntil = null;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    try
    {
        // 3. 调用 AuthService 校验密码并签发令牌
        var token = await _authService.LoginAsync(loginDto);

        // 4. 登录成功，重置失败计数
        if (user != null)
        {
            user.LoginFailCount = 0;
            user.LockedUntil = null;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            // 5. 标记用户在线（用于在线用户列表 + 强制下线）
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            _onlineUserService.UserOnline(user.Id, user.Name, clientIp);
        }

        // 6. 检查密码过期策略
        var passwordPolicy = _appSettings?.PasswordPolicy;
        var mustChangePassword = user?.MustChangePassword ?? false;

        if (!mustChangePassword && passwordPolicy?.ExpirationDays > 0 && user?.PasswordChangedAt.HasValue == true)
        {
            var daysSinceChange = (DateTime.UtcNow - user.PasswordChangedAt.Value).TotalDays;
            if (daysSinceChange > passwordPolicy.ExpirationDays)
            {
                mustChangePassword = true;
            }
        }

        return Ok(ApiResponse.Ok(new LoginResponseDto
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            RequireCaptcha = false,
            MustChangePassword = mustChangePassword
        }, "Login successful"));
    }
    catch (UnauthorizedAccessException)
    {
        // 7. 登录失败，增加失败计数
        var accountLocked = false;
        if (user != null)
        {
            user.LoginFailCount++;

            // 5 次失败锁定 15 分钟
            if (user.LoginFailCount >= MaxLoginFailCount)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                accountLocked = true;
                _logger.LogWarning("Account locked for user: {Email} until {LockedUntil}",
                    user.Email, user.LockedUntil);
            }
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }

        var failMessage = accountLocked
            ? $"密码错误次数过多，账户已锁定 {LockoutMinutes} 分钟"
            : "邮箱或密码错误";

        return Ok(ApiResponse.Error(failMessage, StatusCodes.Status400BadRequest));
    }
}
```

**关键常量**：

```csharp
private const int MaxLoginFailCount = 5;  // 最多失败 5 次
private const int LockoutMinutes = 15;   // 锁定 15 分钟
```

### 2.4 AuthService.LoginAsync：签发双令牌

业务层负责**密码比对 + 签发令牌对**：

```csharp
public async Task<JwtTokenDto> LoginAsync(LoginDto loginDto)
{
    _logger.LogInformation("User login attempt: {Email}", loginDto.Email);

    var user = await _unitOfWork.Users.GetByEmailAsync(loginDto.Email);

    // 密码比对（BCrypt 内置恒定时间比较，防时序攻击）
    if (user == null || !_passwordService.Verify(loginDto.Password, user.PasswordHash))
    {
        _logger.LogWarning("Invalid login attempt: {Email}", loginDto.Email);
        throw new UnauthorizedAccessException("Invalid email or password");
    }

    using var transaction = await _unitOfWork.BeginTransactionAsync();
    try
    {
        // 1. 生成 Access Token（短期，30 分钟）
        var accessToken = await _jwtService.GenerateAccessTokenAsync(user);

        // 2. 生成 Refresh Token（长期，7 天）
        var refreshToken = _jwtService.GenerateRefreshToken();

        // 3. 持久化 Refresh Token 到用户表
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow
            .AddDays(_appSettings.Jwt?.RefreshTokenExpirationDays ?? 7);

        _unitOfWork.Users.Update(user);
        await _unitOfWork.CommitAsync();

        return new JwtTokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Login failed for: {Email}", loginDto.Email);
        await _unitOfWork.RollbackAsync();
        throw;
    }
}
```

<!-- 登录时序图 -->
![登录时序](/screenshots/login-sequence.svg)

---

## 三、验证码服务：图形 + 滑块双重 🛡️

### 3.1 后端：图形验证码

`CaptchaService` 用 **MemoryCache** 存验证码，5 分钟过期：

```csharp
public class CaptchaService
{
    private readonly IMemoryCache _cache;
    // 排除易混字符 0/O、1/I/l
    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";

    /// 生成验证码
    public (string Id, string Code) Generate()
    {
        var id = Guid.NewGuid().ToString("N");
        var code = new string(Enumerable.Range(0, 4)
            .Select(_ => Chars[_random.Next(Chars.Length)]).ToArray());

        _cache.Set($"captcha:{id}", code, TimeSpan.FromMinutes(5));
        return (id, code);
    }

    /// 验证：用完即删（防重放）
    public bool Validate(string id, string code)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(code)) return false;

        var cacheKey = $"captcha:{id}";
        if (_cache.TryGetValue(cacheKey, out string? cachedCode))
        {
            _cache.Remove(cacheKey); // 一次性使用
            return string.Equals(cachedCode, code, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
```

控制器返回的是 **SVG 图形**（避免用图片库依赖）：

```csharp
[HttpGet("captcha")]
[AllowAnonymous]
public IActionResult GetCaptcha()
{
    var (id, code) = _captchaService.Generate();
    var svg = GenerateCaptchaSvg(code); // 生成带噪点的 SVG
    return Ok(ApiResponse.Ok(new CaptchaDto { Id = id, Svg = svg }, "Captcha generated successfully"));
}
```

### 3.2 前端：滑块验证

Chet.Admin 的登录页用了一个**滑块拖动验证**，比传统的"看图打字"体验好：

```vue
<template>
  <div @keydown.enter.prevent="handleLogin">
    <Form />

    <!-- 滑块拖动验证 -->
    <div class="mb-4 mt-1">
      <SliderCaptcha
        ref="sliderRef"
        success-text="验证通过"
        text="请按住滑块，拖动到最右边"
        @success="handleCaptchaSuccess"
      />
    </div>

    <!-- 账号锁定提示 -->
    <div v-if="lockMessage" class="mb-4 text-center text-sm text-red-500">
      {{ lockMessage }}
    </div>

    <VbenButton :loading="authStore.loginLoading" class="w-full" @click="handleLogin">
      {{ $t('common.login') }}
    </VbenButton>
  </div>
</template>
```

**交互流程**：

```typescript
const captchaVerified = ref(false);
const lockMessage = ref('');

function handleCaptchaSuccess() {
  captchaVerified.value = true;
  lockMessage.value = '';
}

async function handleLogin() {
  const { valid } = await formApi.validate();
  if (!valid) return;

  // 关键：滑块没拖过不让提交
  if (!captchaVerified.value) {
    lockMessage.value = '请先完成滑块验证';
    return;
  }

  try {
    await authStore.authLogin(params);
  } catch (error: any) {
    // 失败后重置滑块，要求重新验证
    captchaVerified.value = false;
    sliderRef.value?.resume();

    const responseData = error?.response?.data ?? error?.data ?? {};
    if (responseData.lockedUntil) {
      const remainingMinutes = Math.ceil(
        (new Date(responseData.lockedUntil).getTime() - Date.now()) / 60000
      );
      if (remainingMinutes > 0) {
        lockMessage.value = `账号已锁定，请 ${remainingMinutes} 分钟后重试`;
      }
    }
  }
}
```

> 🎯 **滑块验证 + 后端锁定** 双保险：滑块挡住脚本暴破，后端锁定挡住人工多次试错。

---

## 四、刷新令牌流程：Token Rotation 🔄

### 4.1 为什么要 Token Rotation

**问题**：Refresh Token 长期有效，万一被偷了，攻击者可以一直用它换新 Access Token。

**方案**：每次刷新后**更换 Refresh Token**，旧的失效。这样就算被偷，也只能用一次。

### 4.2 JwtService.RefreshTokenAsync

完整流程：

```csharp
public async Task<JwtTokenDto> RefreshTokenAsync(string accessToken, string refreshToken)
{
    ClaimsPrincipal principal;
    try
    {
        // 1. 从过期的 Access Token 解析用户身份（不校验过期时间）
        principal = GetPrincipalFromExpiredToken(accessToken);
    }
    catch (SecurityTokenException)
    {
        throw new SecurityTokenException("Invalid access token");
    }

    // 2. 提取用户 ID
    var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (string.IsNullOrEmpty(subClaim) || !int.TryParse(subClaim, out var userId) || userId <= 0)
    {
        throw new SecurityTokenException("Invalid access token");
    }

    // 3. 查用户
    var user = await _userRepository.GetByIdAsync(userId);
    if (user == null)
    {
        throw new SecurityTokenException("User not found");
    }

    // 4. 三重校验：Token 必须匹配数据库里的
    if (user.RefreshToken != refreshToken)
    {
        throw new SecurityTokenException("Invalid refresh token");
    }

    // 5. 校验未过期
    if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
    {
        throw new SecurityTokenException("Refresh token expired");
    }

    // 6. 关键：生成全新的令牌对
    var newAccessToken = await GenerateAccessTokenAsync(user);
    var newRefreshToken = GenerateRefreshToken();  // 新的随机串

    // 7. 覆盖旧 Refresh Token（旧的自动失效）
    user.RefreshToken = newRefreshToken;
    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
        _appSettings.Jwt?.RefreshTokenExpirationDays > 0 ? _appSettings.Jwt.RefreshTokenExpirationDays : 7);

    _userRepository.Update(user);
    await _userRepository.SaveChangesAsync();

    return new JwtTokenDto
    {
        AccessToken = newAccessToken,
        RefreshToken = newRefreshToken
    };
}
```

### 4.3 RefreshTokenDto：刷新入参

```csharp
public class RefreshTokenDto
{
    /// 当前（可能已过期）的 Access Token
    public required string AccessToken { get; set; }

    /// 刷新令牌
    public required string RefreshToken { get; set; }
}
```

<!-- Token Rotation 流程 -->
![Token Rotation](/screenshots/token-rotation.svg)

### 4.4 Refresh Token 怎么生成

用**密码学安全的随机数生成器**（CSPRNG），不是 `Random`：

```csharp
public string GenerateRefreshToken()
{
    var randomNumber = new byte[32];
    using (var rng = RandomNumberGenerator.Create())
    {
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber); // 44 字符
    }
}
```

> ⚠️ **重要**：`Random` 是伪随机，可预测；`RandomNumberGenerator` 才是密码学安全。**生成 Token 必须用后者**。

---

## 五、密码过期策略 ⏰

### 5.1 UserEntity 里的相关字段

```csharp
public class UserEntity : BaseEntity
{
    // ... 其他字段

    /// 密码最后修改时间
    public DateTime? PasswordChangedAt { get; set; }

    /// 是否需要强制修改密码
    public bool MustChangePassword { get; set; } = false;
}
```

### 5.2 登录时的过期判断

回到前面 `Login` 方法的关键片段：

```csharp
var passwordPolicy = _appSettings?.PasswordPolicy;
var mustChangePassword = user?.MustChangePassword ?? false;

// 如果策略里配了 ExpirationDays，且用户改过密码
if (!mustChangePassword && passwordPolicy?.ExpirationDays > 0 && user?.PasswordChangedAt.HasValue == true)
{
    var daysSinceChange = (DateTime.UtcNow - user.PasswordChangedAt.Value).TotalDays;
    if (daysSinceChange > passwordPolicy.ExpirationDays)
    {
        mustChangePassword = true;  // 标记为需要强制改密
    }
}
```

> 💡 配置文件里设置 `PasswordPolicy:ExpirationDays = 90`，就实现了"**90 天强制改密**"。

### 5.3 强制改密接口

密码过期后，前端会引导用户跳转到改密页面。**专用接口**：

```csharp
[HttpPut("force-change-password")]
[Authorize]
public async Task<IActionResult> ForceChangePassword([FromBody] ForceChangePasswordDto dto)
{
    var userId = GetUserId();
    await _userService.ChangePasswordAsync(userId, dto.OldPassword, dto.NewPassword);
    return Ok(ApiResponse.Ok(null, "Password changed successfully"));
}
```

`ForceChangePasswordDto` 强制 6 位下限：

```csharp
public class ForceChangePasswordDto
{
    [Required(ErrorMessage = "旧密码不能为空")]
    public required string OldPassword { get; set; }

    [Required(ErrorMessage = "新密码不能为空")]
    [MinLength(6, ErrorMessage = "新密码至少6位")]
    public required string NewPassword { get; set; }
}
```

---

## 六、前端登录页交互 🎨

### 6.1 登录表单 schema

```typescript
const formSchema = computed((): VbenFormSchema[] => {
  return [
    {
      component: 'VbenInput',
      componentProps: { placeholder: $t('authentication.emailTip') },
      defaultValue: 'admin@example.com',
      fieldName: 'email',
      label: $t('authentication.email'),
      rules: z.string()
        .min(1, { message: $t('authentication.emailTip') })
        .email({ message: $t('authentication.emailValidErrorTip') }),
    },
    {
      component: 'VbenInputPassword',
      componentProps: { placeholder: $t('authentication.password') },
      defaultValue: 'Admin@123',
      fieldName: 'password',
      label: $t('authentication.password'),
      rules: z.string().min(1, { message: $t('authentication.passwordTip') }),
    },
  ];
});
```

> 📌 默认值填的是 `admin@example.com / Admin@123`，方便本地开发调试。

### 6.2 authStore：Pinia 状态管理

`useAuthStore` 负责调用登录 API、保存 Token、拉取用户信息：

```typescript
export const useAuthStore = defineStore('auth', () => {
  const accessStore = useAccessStore();
  const userStore = useUserStore();
  const router = useRouter();
  const loginLoading = ref(false);

  async function authLogin(params: Recordable<any>, onSuccess?: () => Promise<void> | void) {
    let userInfo: null | UserInfo = null;
    try {
      loginLoading.value = true;

      // 1. 调登录 API
      const { accessToken, refreshToken } = await loginApi(params);

      if (accessToken) {
        // 2. 保存双 Token
        accessStore.setAccessToken(accessToken);
        accessStore.setRefreshToken(refreshToken);

        // 3. 拉取用户信息（含 roles 和 permissions）
        userInfo = await fetchUserInfo();

        // 4. 跳转到首页
        await router.push(userInfo.homePath || preferences.app.defaultHomePath);

        // 5. 弹欢迎提示
        if (userInfo?.realName) {
          notification.success({
            description: `${$t('authentication.loginSuccessDesc')}:${userInfo?.realName}`,
            message: $t('authentication.loginSuccess'),
          });
        }
      }
    } finally {
      loginLoading.value = false;
    }
    return { userInfo };
  }

  /// 获取用户信息：包含 permissions
  async function fetchUserInfo(): Promise<UserInfo> {
    const { userInfo, permissions } = await getUserInfoApi();
    userStore.setUserInfo(userInfo);
    accessStore.setAccessCodes(permissions); // 关键：保存权限码
    return userInfo;
  }

  async function logout(redirect: boolean = true) {
    try { await logoutApi(); } catch { /* 忽略 */ }
    resetAllStores();
    accessStore.setLoginExpired(false);
    await router.replace({
      path: LOGIN_PATH,
      query: redirect ? { redirect: encodeURIComponent(router.currentRoute.value.fullPath) } : {},
    });
  }

  return { authLogin, fetchUserInfo, loginLoading, logout };
});
```

<!-- 前端登录流程 -->
![前端登录流程](/screenshots/frontend-login.svg)

---

## 七、登出与用户信息接口 🚪

### 7.1 Logout

JWT 是**无状态**的，登出主要做两件事：

```csharp
[HttpPost("logout")]
[Authorize]
public async Task<IActionResult> Logout()
{
    var userId = GetUserId();

    // 关键：从在线用户列表移除（影响强制下线判断）
    _onlineUserService.UserOffline(userId);

    // JWT 是无状态的，客户端清除 Token 即可
    // 如需服务端使 Token 失效，可加入黑名单机制
    return Ok(ApiResponse.Ok(null, "Logout successful"));
}
```

### 7.2 GetUserInfo：登录后第一件事

前端登录拿到 Token 后，立刻调这个接口拉用户信息：

```csharp
[HttpGet("user-info")]
[Authorize]
public async Task<IActionResult> GetUserInfo()
{
    var userId = GetUserId();
    var userInfo = await _authService.GetUserInfoAsync(userId);
    return Ok(ApiResponse.Ok(userInfo, "User info retrieved successfully"));
}
```

返回的 `UserInfoDto` 包含**角色和权限码**：

```csharp
public class UserInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? Avatar { get; set; }

    /// 角色编码列表（如 ["admin", "user"]）
    public List<string> Roles { get; set; } = [];

    /// 权限码列表（如 ["system:user:create", ...]）
    public List<string> Permissions { get; set; } = [];
}
```

---

## 八、安全机制总结表 🛡️

| 威胁 | 防护措施 | 实现位置 |
| ---- | ---- | ---- |
| 暴力破解 | 失败 5 次锁 15 分钟 | AuthController.Login |
| 脚本攻击 | 滑块验证码 | login.vue |
| 密码泄露 | BCrypt 哈希 + 工作因子 12 | PasswordService |
| Token 被偷 | Refresh Token Rotation | JwtService.RefreshTokenAsync |
| Token 被吊销 | ValidFrom 早于吊销时间则拒 | JwtConfiguration.OnTokenValidated |
| 弱密码长期使用 | 90 天过期强制改 | AuthController.Login + PasswordPolicy |
| 时序攻击 | BCrypt 内置恒定时间比较 | PasswordService.Verify |
| 重放攻击 | 验证码一次性使用 | CaptchaService.Validate |

---

## 九、配置一览 ⚙️

`appsettings.json` 里和认证相关的配置：

```json
{
  "Jwt": {
    "Enabled": true,
    "SecretKey": "your-256-bit-secret-key-...",
    "Issuer": "Chet.Admin",
    "Audience": "Chet.Admin.Client",
    "AccessTokenExpirationMinutes": 30,
    "RefreshTokenExpirationDays": 7
  },
  "PasswordPolicy": {
    "ExpirationDays": 90,
    "MinLength": 6
  }
}
```

| 配置项 | 默认 | 说明 |
| ---- | ---- | ---- |
| AccessTokenExpirationMinutes | 30 | Access Token 有效期 |
| RefreshTokenExpirationDays | 7 | Refresh Token 有效期 |
| PasswordPolicy:ExpirationDays | 90 | 密码多少天过期 |

---

## 下篇预告

下一篇我们看 **用户管理 + 个人中心**：用户 CRUD、分配角色、数据权限过滤、个人资料修改、头像上传，前端 editingId 模式怎么用 👤

---

## 开源地址

- **GitHub**：https://github.com/qiect/Chet.Admin
- **Gitee**：https://gitee.com/qiect/Chet.Admin

觉得有帮助的话，**点个 Star ⭐** 支持一下吧！你的 Star 是我持续更新的动力～

---

## 互动

你项目里的登录做了几道防线？Token Rotation 用了吗？评论区聊聊～👇

---

> 🔗 GitHub：https://github.com/qiect/Chet.Admin
> 🔗 Gitee：https://gitee.com/qiect/Chet.Admin
> ⭐ 觉得不错的话，点个 Star 支持一下吧！

`#ChetAdmin` `#认证登录` `#JWT` `#TokenRotation` `#BCrypt` `#.NET10` `#Vue3`
