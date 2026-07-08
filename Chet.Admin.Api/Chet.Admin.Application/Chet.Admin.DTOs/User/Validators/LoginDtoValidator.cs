using FluentValidation;

namespace Chet.Admin.DTOs.User.Validators;

/// <summary>
/// 用户登录DTO验证器
/// </summary>
public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    /// <summary>
    /// 验证规则配置
    /// </summary>
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("邮箱不能为空")
            .EmailAddress().WithMessage("请输入有效的邮箱地址");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("密码不能为空");
    }
}
