using FluentValidation;

namespace Chet.Admin.DTOs.User.Validators;

/// <summary>
/// 用户注册DTO验证器
/// </summary>
public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    /// <summary>
    /// 验证规则配置
    /// </summary>
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("用户名不能为空")
            .MaximumLength(100).WithMessage("用户名长度不能超过100个字符");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱不能为空")
            .EmailAddress().WithMessage("请输入有效的邮箱地址")
            .MaximumLength(255).WithMessage("邮箱长度不能超过255个字符");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空")
            .MinimumLength(6).WithMessage("密码长度不能少于6个字符");
    }
}
