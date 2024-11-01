using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Carhub.Service.Users.Core.DTOs;
using Carhub.Service.Users.Core.Entities;
using Carhub.Service.Users.Core.Services;
using CarHub.Users.Tests.Integration.Base;
using CarHub.Users.Tests.Integration.Helpers;
using CarHub.Users.Tests.Integration.Models;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace CarHub.Users.Tests.Integration.Controllers;

public class AccountControllerTests(TestApplicationFactory factory) : IntegrationTestsBase(factory)
{
    #region Ping

    [Fact]
    public async Task Ping_ShouldReturnPong()
    {
        var response = await Client.GetAsync("/account/ping");
        var content = await response.Content.ReadAsStringAsync();

        content.ShouldBe("Pong");
    }

    #endregion

    #region SignIn

    [Fact]
    public async Task SignIn_ShouldReturnJwt_ForValidCredentials()
    {
        var signInDto = new SignInDto
        {
            Email = "some.user@test.com",
            Password = "WeakPassword99#"
        };

        var response = await Client.PostAsJsonAsync("/account/sign-in", signInDto);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var jwt = await response.Content.ReadFromJsonAsync<JwtDto>();
        jwt.ShouldNotBeNull();
        jwt.AccessToken.GeneratedToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SignIn_ShouldReturnUnauthorized_ForInvalidCredentials()
    {
        var signInDto = new SignInDto
        {
            Email = "wrong.user@test.com",
            Password = "WrongPassword123!"
        };

        var response = await Client.PostAsJsonAsync("/account/sign-in", signInDto);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<Error[]>())!.First();
        error.ShouldNotBeNull();
        error.Code.ShouldBe("InvalidCredentialsException");
        error.Message.ShouldBe("Invalid credentials.");
    }

    #endregion

    #region GetCurrentUserAsync

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUserDetails_ForValidToken()
    {
        await SignIn(_user);
        var response = await Client.GetAsync("/account/me");

        response.IsSuccessStatusCode.ShouldBeTrue();

        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.ShouldNotBeNull();
        user.Email.ShouldBe("some.user@test.com");
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUnauthorized_ForMissingOrInvalidToken()
    {
        var response = await Client.GetAsync("/account/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region SignUp

    [Fact]
    public async Task SignUp_ShouldReturnBadRequest_ForExistingEmail()
    {
        var signUpDto = new SignUpDto()
        {
            Email = "some.user@test.com",
            Firstname = "John",
            Lastname = "Doe",
            Password = "SuperStrongPassword123!"
        };

        var response = await Client.PostAsJsonAsync("/account/sign-up", signUpDto);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<Error[]>())!.First();
        error.ShouldNotBeNull();
        error.Code.ShouldBe("EmailInUseException");
        error.Message.ShouldBe("Email 'some.user@test.com' is already in use.");
    }

    [Fact]
    public async Task SignUp_ShouldReturnBadRequest_ForWeakPassword()
    {
        var signUpDto = new SignUpDto()
        {
            Email = "some.user2@test.com",
            Firstname = "John",
            Lastname = "Doe",
            Password = "i'm weak"
        };

        var response = await Client.PostAsJsonAsync("/account/sign-up", signUpDto);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<Error[]>())!.First();
        error.ShouldNotBeNull();
        error.Code.ShouldBe("PasswordRequirementsException");
        error.Message.ShouldBe("Password must contain at least one uppercase letter.\nPassword must contain at least one digit.\nAll the requirements must be fulfilled.");
    }

    [Fact]
    public async Task SignUp_ShouldReturnNoContent_ForValidInput()
    {
        var signUpDto = new SignUpDto()
        {
            Email = "some.user3@test.com",
            Firstname = "John",
            Lastname = "Doe",
            Password = "SuperStrongPassword123!"
        };

        var response = await Client.PostAsJsonAsync("/account/sign-up", signUpDto);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).ShouldBeEmpty();
    }

    #endregion

    #region GetUserById

    [Fact]
    public async Task GetUserById_ShouldReturnUnauthorized_ForNotLoggedUser()
    {
        var response = await Client.GetAsync($"/account/user/{Guid.Empty}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnNotFound_ForInvalidId()
    {
        await SignIn(_user);

        var response = await Client.GetAsync($"/account/user/{Guid.Empty}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnUserData_ForValidId()
    {
        await SignIn(_user);
        var user = await SeedFakeUser("get.user.by.id@test.com", "123");

        var response = await Client.GetAsync($"/account/user/{user.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserDto>();
        result.ShouldNotBeNull();
        result.Id.ShouldBe(user.Id);
        result.Email.ShouldBe(user.Email);
    }

    #endregion

    #region SignUpAdmin

    [Fact]
    public async Task SignUpAdmin_ShouldReturnUnauthorized_ForNotLoggedUser()
    {
        var signUpDto = new SignUpDto()
        {
            Email = "some.admin@test.com",
            Firstname = "John",
            Lastname = "Doe",
            Password = "SuperStrongPassword123!"
        };

        var response = await Client.PostAsJsonAsync("/account/sign-up-admin", signUpDto);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task SignUpAdmin_ShouldReturnForbidden_ForLoggedUser()
    {
        await SignIn(_user);
        var signUpDto = new SignUpDto()
        {
            Email = "some.admin@test.com",
            Firstname = "John",
            Lastname = "Doe",
            Password = "SuperStrongPassword123!"
        };

        var response = await Client.PostAsJsonAsync("/account/sign-up-admin", signUpDto);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task SignUpAdmin_ShouldReturnBadRequest_ForExistingEmail()
    {
        await SignIn(_admin);
        var signUpDto = new SignUpDto()
        {
            Email = "some.user@test.com",
            Firstname = "John",
            Lastname = "Doe",
            Password = "SuperStrongPassword123!"
        };

        var response = await Client.PostAsJsonAsync("/account/sign-up-admin", signUpDto);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<Error[]>())!.First();
        error.ShouldNotBeNull();
        error.Code.ShouldBe("EmailInUseException");
        error.Message.ShouldBe("Email 'some.user@test.com' is already in use.");
    }

    [Fact]
    public async Task SignUpAdmin_ShouldReturnBadRequest_ForWeakPassword()
    {
        await SignIn(_admin);
        var signUpDto = new SignUpDto()
        {
            Email = "some.admin1@test.com",
            Firstname = "John",
            Lastname = "Doe",
            Password = "i'm weak"
        };

        var response = await Client.PostAsJsonAsync("/account/sign-up-admin", signUpDto);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<Error[]>())!.First();
        error.ShouldNotBeNull();
        error.Code.ShouldBe("PasswordRequirementsException");
        error.Message.ShouldBe("Password must contain at least one uppercase letter.\nPassword must contain at least one digit.\nAll the requirements must be fulfilled.");
    }

    [Fact]
    public async Task SignUpAdmin_ShouldReturnNoContent_ForValidUserAndInput()
    {
        await SignIn(_admin);
        var signUpDto = new SignUpDto()
        {
            Email = "some.admin@test.com",
            Firstname = "John",
            Lastname = "Doe",
            Password = "SuperStrongPassword123!"
        };

        var response = await Client.PostAsJsonAsync("/account/sign-up-admin", signUpDto);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).ShouldBeEmpty();
    }

    #endregion

    #region ChangePassword

    [Fact]
    public async Task ChangePassword_ShouldReturnUnauthorized_ForUserNotSignedIn()
    {
        var dto = new ChangePasswordDto()
        {
            NewPassword = "123",
            OldPassword = "123"
        };

        var response = await Client.PostAsJsonAsync("/account/change-password", dto);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnBadRequest_ForInvalidCurrentPassword()
    {
        await SignIn(_user);
        var dto = new ChangePasswordDto()
        {
            NewPassword = "123",
            OldPassword = "123"
        };

        var response = await Client.PostAsJsonAsync("/account/change-password", dto);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<Error[]>())!.First();
        error.ShouldNotBeNull();
        error.Code.ShouldBe("InvalidCredentialsException");
        error.Message.ShouldBe("Invalid credentials.");
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnBadRequest_ForInvalidNewPassword()
    {
        await SignIn(_user);
        var dto = new ChangePasswordDto()
        {
            OldPassword = "WeakPassword99#",
            NewPassword = "123"
        };

        var response = await Client.PostAsJsonAsync("/account/change-password", dto);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<Error[]>())!.First();
        error.ShouldNotBeNull();
        error.Code.ShouldBe("PasswordRequirementsException");
        error.Message.ShouldBe("Password must be at least 8 characters long.\nPassword must contain at least one uppercase letter.\nPassword must contain at least one lowercase letter.\nPassword must contain at least one special character.\nAll the requirements must be fulfilled.");
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnOk_ForValidInput()
    {
        var user = await SeedFakeUser("change.password@carhub.com", "Secret");
        await SignIn(new SignInDto(){Email = user.Email, Password = "Secret"});
        var dto = new ChangePasswordDto()
        {
            OldPassword = "Secret",
            NewPassword = "SecretSecret123!"
        };

        var response = await Client.PostAsJsonAsync("/account/change-password", dto);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBeEmpty();
        await SignIn(new SignInDto(){Email = user.Email, Password = "SecretSecret123!"});
    }

    #endregion

    #region Forgot & reset password

    [Fact]
    public async Task ForgotPassword_ShouldReturnBadRequest_WhenEmailDoesNotExist()
    {
        var dto = new RequestResetPasswordTokenDto()
        {
            Email = "forgot.password1@carhub.com"
        };

        var response = await Client.PostAsJsonAsync("/account/forgot-password", dto);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<Error[]>())!.First();
        error.Code.ShouldBe("UserNotFoundException");
        error.Message.ShouldBe("User with email: 'forgot.password1@carhub.com' was not found.");
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnBadRequest_WhenTokenIsInvalid()
    {
        var dto = new ResetPasswordDto
        {
            NewPassword = "123",
            ResetToken = "invalid"
        };
        var response = await Client.PostAsJsonAsync("/account/reset-password", dto);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<Error[]>())!.First();
        error.Code.ShouldBe("InvalidResetPasswordTokenException");
        error.Message.ShouldBe("Given reset token is invalid.");

    }

    [Fact]
    public async Task ForgotPassword_ShouldGenerateResetToken_WhenEmailIsValid()
    {
        var user = await SeedFakeUser("test.usr1@carhub.com", "123");
        var dto = new RequestResetPasswordTokenDto()
        {
            Email = user.Email
        };

        var response = await Client.PostAsJsonAsync("/account/forgot-password", dto);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await DbContext.Entry(user).ReloadAsync();
        user.PasswordResetToken.ShouldNotBeNullOrEmpty();
        Assert.True(user.PasswordTokenExpires > DateTime.Now);
    }

    [Fact]
    public async Task ForgotResetPassword_ShouldChangePassword_WhenResetPasswordTokenIsValid()
    {
        var user = await SeedFakeUser("test.usr2@carhub.com", "123");
        var dto = new RequestResetPasswordTokenDto()
        {
            Email = user.Email
        };

        var response = await Client.PostAsJsonAsync("/account/forgot-password", dto);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await DbContext.Entry(user).ReloadAsync();
        user.PasswordResetToken.ShouldNotBeNullOrEmpty();
        Assert.True(user.PasswordTokenExpires > DateTime.Now);

        const string newPassword = "SuperSecretPassword123!";
        var resetDto = new ResetPasswordDto
        {
            NewPassword = newPassword,
            ResetToken = user.PasswordResetToken
        };
        var resetResponse = await Client.PostAsJsonAsync("/account/reset-password", resetDto);
        resetResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        await SignIn(new SignInDto
        {
            Email = user.Email,
            Password = newPassword
        });
    }

    #endregion

    #region Ban

    [Fact]
    public async Task BanUser_ShouldReturnUnauthorized_ForNotLoggedUser()
    {
        var userId = Guid.Empty;
        var response = await Client.PostAsync($"/account/ban/{userId}", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BanUser_ShouldReturnForbidden_ForLoggedUser()
    {
        await SignIn(_user);
        var userId = Guid.Empty;
        var response = await Client.PostAsync($"/account/ban/{userId}", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BanUser_ShouldReturnBadRequest_ForInvalidUserId()
    {
        await SignIn(_admin);
        var userId = Guid.Empty;
        var response = await Client.PostAsync($"/account/ban/{userId}", null);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<Error[]>())!.First();
        error.Code.ShouldBe("UserNotFoundException");
        error.Message.ShouldBe("User with id: '00000000000000000000000000000000' was not found.");
    }

    [Fact]
    public async Task BanUser_ShouldBanAccount_ForValidUserId()
    {
        var userToBan = await SeedFakeUser("user.toban@carhub.com", "123");
        await SignIn(_admin);
        var userId = userToBan.Id;
        userToBan.Banned.ShouldBeFalse();
        var response = await Client.PostAsync($"/account/ban/{userId}", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await DbContext.Entry(userToBan).ReloadAsync();
        userToBan.Banned.ShouldBeTrue();
    }

    #endregion

    #region Unban

    [Fact]
    public async Task UnbanUser_ShouldReturnUnauthorized_ForNotLoggedUser()
    {
        var userId = Guid.Empty;
        var response = await Client.PostAsync($"/account/unban/{userId}", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnbanUser_ShouldReturnForbidden_ForLoggedUser()
    {
        await SignIn(_user);
        var userId = Guid.Empty;
        var response = await Client.PostAsync($"/account/unban/{userId}", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnbanUser_ShouldReturnBadRequest_ForInvalidUserId()
    {
        await SignIn(_admin);
        var userId = Guid.Empty;
        var response = await Client.PostAsync($"/account/unban/{userId}", null);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<Error[]>())!.First();
        error.Code.ShouldBe("UserNotFoundException");
        error.Message.ShouldBe("User with id: '00000000000000000000000000000000' was not found.");
    }

    [Fact]
    public async Task UnbanUser_ShouldUnbanAccount_ForValidUserId()
    {
        var userToUnban = await SeedFakeUser("user.toban1@carhub.com", "123");
        userToUnban.Banned = true;
        await DbContext.SaveChangesAsync();
        await DbContext.Entry(userToUnban).ReloadAsync();
        await SignIn(_admin);
        var userId = userToUnban.Id;
        userToUnban.Banned.ShouldBeTrue();
        var response = await Client.PostAsync($"/account/unban/{userId}", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await DbContext.Entry(userToUnban).ReloadAsync();
        userToUnban.Banned.ShouldBeFalse();
    }

    #endregion

    #region Refresh token

    [Fact]
    public async Task RefreshToken_ShouldReturnBadRequest_ForInvalidInput()
    {
        var dto = new RefreshTokenDto();
        var response = await Client.PostAsJsonAsync("/account/refresh-token", dto);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnBadRequest_ForInvalidJwt()
    {
        var dto = new RefreshTokenDto
        {
            Jwt = "abc",
            RefreshToken = "abc"
        };
        var response = await Client.PostAsJsonAsync("/account/refresh-token", dto);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<Error[]>())!.First();
        error.Code.ShouldBe("InvalidJwtTokenException");
        error.Message.ShouldBe("Invalid jwt token.");
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnNewToken()
    {
        var oldJwt = await SignIn(_user);
        var dto = new RefreshTokenDto
        {
            Jwt = oldJwt.AccessToken.GeneratedToken,
            RefreshToken = oldJwt.RefreshToken.GeneratedToken
        };
        var response = await Client.PostAsJsonAsync("/account/refresh-token", dto);
        var newJwt = await response.Content.ReadFromJsonAsync<JwtDto>();
        newJwt.ShouldNotBeNull();
        newJwt.AccessToken.GeneratedToken.ShouldNotBeNullOrWhiteSpace();
        newJwt.RefreshToken.GeneratedToken.ShouldNotBeNullOrWhiteSpace();
        newJwt.RefreshToken.GeneratedToken.ShouldNotBe(oldJwt.RefreshToken.GeneratedToken);
        newJwt.AccessToken.GeneratedToken.ShouldNotBe(oldJwt.AccessToken.GeneratedToken);
    }

    #endregion

    #region New activation token

    [Fact]
    public async Task NewActivationToken_ShouldReturnBadRequest_ForInvalidEmail()
    {
        var dto = new NewActivationTokenDto
        {
            Email = "some.fake@carhub.com",
        };
        var response = await Client.PostAsJsonAsync("/account/new-activation-token", dto);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = (await response.Content.ReadFromJsonAsync<Error[]>())!.First();
        error.Code.ShouldBe("UserNotFoundException");
        error.Message.ShouldBe("User with email: 'some.fake@carhub.com' was not found.");
    }

    [Fact]
    public async Task NewActivationToken_ShouldReturnOk_ForValidEmail()
    {
        var user = await SeedFakeUser("new.token@carhub.com", "2113");
        await DbContext.Entry(user).ReloadAsync();
        var oldActivateToken = user.ActivationToken;
        var dto = new NewActivationTokenDto
        {
            Email = user.Email
        };
        var response = await Client.PostAsJsonAsync("/account/new-activation-token", dto);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await DbContext.Entry(user).ReloadAsync();
        user.ActivationToken.ShouldNotBeNullOrWhiteSpace();
        user.ActivationToken.ShouldNotBe(oldActivateToken);
    }

    #endregion

    #region Activate

    [Fact]
    public async Task ActivateAccount_ShouldReturnBadRequest_ForInvalidToken()
    {
        var dto = new ActivateAccountDto
        {
            ActivationToken = "123"
        };
        var response = await Client.PostAsJsonAsync("/account/activate", dto);
        var error = (await response.Content.ReadFromJsonAsync<Error[]>())!.First();
        error.Code.ShouldBe("InvalidActivationTokenException");
        error.Message.ShouldBe("Given activation token is invalid.");
    }

    [Fact]
    public async Task ActivateAccount_ShouldReturnBadRequest_ForExpiredToken()
    {
        var user = await SeedFakeUser("fake.activate@carhub.com", "123");
        user.ActivationToken = "123token";
        user.ActivationTokenExpires = DateTime.MinValue;
        await DbContext.SaveChangesAsync();

        var dto = new ActivateAccountDto
        {
            ActivationToken = "123token"
        };
        var response = await Client.PostAsJsonAsync("/account/activate", dto);
        var error = (await response.Content.ReadFromJsonAsync<Error[]>())!.First();
        error.Code.ShouldBe("ActivationTokenExpiredException");
        error.Message.ShouldBe("Given activation token is expired.");
    }

    [Fact]
    public async Task ActivateAccount_ShouldReturnOk_ForValidToken()
    {
        var user = await SeedFakeUser("fake.activate1@carhub.com", "123");
        user.IsActive = false;
        user.ActivationToken = "123token";
        user.ActivationTokenExpires = DateTime.MaxValue;
        await DbContext.SaveChangesAsync();

        var dto = new ActivateAccountDto
        {
            ActivationToken = "123token"
        };
        var response = await Client.PostAsJsonAsync("/account/activate", dto);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await DbContext.Entry(user).ReloadAsync();
        user.IsActive.ShouldBeTrue();
    }

    #endregion

    private async Task<JwtDto> SignIn(SignInDto signInDto)
    {
        var loginResponse = await Client.PostAsJsonAsync("/account/sign-in", signInDto);
        loginResponse.EnsureSuccessStatusCode();

        var jwt = await loginResponse.Content.ReadFromJsonAsync<JwtDto>();
        jwt.ShouldNotBeNull();
        jwt.AccessToken.GeneratedToken.ShouldNotBeNullOrWhiteSpace();

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            jwt.AccessToken.GeneratedToken);
        return jwt;
    }
    private readonly SignInDto _admin = new()
    {
        Email = "carhub.app2024@gmail.com",
        Password = "SuperSecretAdminPassword123!"
    };

    private readonly SignInDto _user = new()
    {
        Email = "some.user@test.com",
        Password = "WeakPassword99#"
    };

    private async Task<User> SeedFakeUser(string email, string password)
    {
        var passwordManager = factory.Services.GetRequiredService<IPasswordManager>();
        passwordManager.CreatePasswordHash(password, out var hash, out var salt);
        var user = new User()
        {
            Email = email,
            Role = "user",
            IsActive = true,
            PasswordHash = hash,
            PasswordSalt = salt
        };

        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();
        return user;
    }
}