using FluentValidation;

namespace Chet.Admin.DTOs.User.Validators;

/// <summary>
/// 用户更新DTO验证器
/// </summary>
public class UserUpdateDtoValidator : AbstractValidator<UserUpdateDto>
{
    /// <summary>
    /// 验证规则配置
    /// </summary>
    public UserUpdateDtoValidator()
    {
        // 所有字段均为可选，仅在有值时验证格式
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("用户名长度不能超过100个字符")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("请输入有效的邮箱地址")
            .MaximumLength(255).WithMessage("邮箱长度不能超过255个字符")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Password)
            .MinimumLength(6).WithMessage("密码至少6位")
            .When(x => !string.IsNullOrWhiteSpace(x.Password));
    }
}
