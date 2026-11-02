using FluentValidation.TestHelper;
using MealManager.Application.Features.Auth;
using MealManager.Application.Features.Messes;
using MealManager.Application.Features.Members;

namespace MealManager.UnitTests;

public class ValidatorTests
{
    private readonly RegisterCommandValidator _register = new();
    private readonly LoginCommandValidator _login = new();
    private readonly CreateMessCommandValidator _createMess = new();
    private readonly UpdateMemberRoleCommandValidator _role = new();

    [Theory]
    [InlineData("+8801712345678", true)]
    [InlineData("8801712345678", true)]
    [InlineData("12345", false)]
    [InlineData("abc-defg", false)]
    public void Register_PhoneFormat(string phone, bool expected)
    {
        var result = _register.TestValidate(new RegisterCommand("Alice", phone, "Passw0rd!"));
        if (expected)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Phone);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Phone);
        }
    }

    [Fact]
    public void Register_ShortPassword_Fails()
    {
        _register.TestValidate(new RegisterCommand("Alice", "+8801700000000", "short"))
            .ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void CreateMess_EmptyName_Fails()
    {
        _createMess.TestValidate(new CreateMessCommand(""))
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("mealManager", true)]
    [InlineData("member", true)]
    [InlineData("admin", false)]
    public void UpdateMemberRole_RoleWhitelist(string role, bool expected)
    {
        var result = _role.TestValidate(new UpdateMemberRoleCommand(Guid.NewGuid(), role));
        if (expected)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Role);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Role);
        }
    }

    [Fact]
    public void Login_MissingPassword_Fails()
    {
        _login.TestValidate(new LoginCommand("+8801700000000", ""))
            .ShouldHaveValidationErrorFor(x => x.Password);
    }
}
