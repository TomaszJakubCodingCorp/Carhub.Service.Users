// using System.Net;
// using System.Net.Http.Json;
// using Carhub.Service.Users.Core.DAL;
// using Carhub.Service.Users.Core.DTOs;
// using Carhub.Service.Users.Core.Entities;
// using Carhub.Service.Users.Core.Services;
// using CarHub.Users.Tests.Integration.Helpers;
// using CarHub.Users.Tests.Integration.Models;
// using Microsoft.AspNetCore.Mvc.Testing;
// using Microsoft.Extensions.DependencyInjection;
// using Shouldly;
//
// namespace CarHub.Users.Tests.Integration.Controllers;
//
// public sealed class UnauthorizedAccountControllerTest : IClassFixture<UnauthorizedApplicationFactory<Program>>
// {
//     private readonly HttpClient _client;
//     private readonly WebApplicationFactory<Program> _factory;
//     private readonly IPasswordManager _passwordManager;
//
//     public UnauthorizedAccountControllerTest(UnauthorizedApplicationFactory<Program> factory)
//     {
//         _factory = factory;
//         _client = _factory.CreateClient();
//         _passwordManager = new PasswordManager();
//     }
//     
//
//     [Fact]
//     public async Task SignInAsync_ShouldReturnOk_WithToken_WhenCredentialsAreValid()
//     {
//         // Arrange
//         var email = "valid1@email.com";
//         var password = "P@ssw0rd";
//         await SeedActiveUser(email, password, Guid.NewGuid());
//
//         var signInDto = new SignInDto
//         {
//             Email = email,
//             Password = password
//         };
//
//         // Act
//         var response = await _client.PostAsJsonAsync("/account/sign-in", signInDto);
//         var tokenDto = await response.Content.ReadFromJsonAsync<JwtDto>();
//
//         // Assert
//         response.StatusCode.ShouldBe(HttpStatusCode.OK);
//         tokenDto.ShouldNotBeNull();
//         tokenDto.AccessToken.ShouldNotBeEmpty();
//         tokenDto.Email.ShouldBe(email);
//     }
//
//     [Fact]
//     public async Task SignInAsync_ShouldReturnBadRequest_WhenPasswordIsInvalid()
//     {
//         // Arrange
//         var email = "valid2@email.com";
//         var password = "P@ssw0rd";
//         await SeedActiveUser(email, password, Guid.NewGuid());
//
//         var signInDto = new SignInDto
//         {
//             Email = email,
//             Password = password + "1"
//         };
//
//         // Act
//         var response = await _client.PostAsJsonAsync("/account/sign-in", signInDto);
//
//         // Assert
//         response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
//         var errors = await response.Content.ReadFromJsonAsync<Error[]>();
//         errors!.Length.ShouldBe(1);
//         errors.First().Code.ShouldBe("InvalidCredentialsException");
//         errors.First().Message.ShouldBe("Invalid credentials.");
//     }
//
//     [Fact]
//     public async Task SignInAsync_ShouldReturnBadRequest_WhenEmailIsInvalid()
//     {
//         // Arrange
//         var email = "valid3@email.com";
//         var password = "P@ssw0rd";
//         await SeedActiveUser(email, password, Guid.NewGuid());
//
//         var signInDto = new SignInDto
//         {
//             Email = email + "1",
//             Password = password
//         };
//
//         // Act
//         var response = await _client.PostAsJsonAsync("/account/sign-in", signInDto);
//
//         // Assert
//         response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
//         var errors = await response.Content.ReadFromJsonAsync<Error[]>();
//         errors!.Length.ShouldBe(1);
//         errors.First().Code.ShouldBe("InvalidCredentialsException");
//         errors.First().Message.ShouldBe("Invalid credentials.");
//     }
//
//     [Fact]
//     public async Task SignUpAsync_ShouldReturnNoContent_WhenSuccessful()
//     {
//         // Arrange
//         var signUpDto = new SignUpDto
//         {
//             Email = "newuser@example.com",
//             Password = "P@ssw0rd",
//             Firstname = "Jane",
//             Lastname = "Doe",
//             Role = "User",
//             Claims = new Dictionary<string, IEnumerable<string>>()
//         };
//
//         // Act
//         var response = await _client.PostAsJsonAsync("/account/sign-up", signUpDto);
//
//         // Assert
//         response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
//     }
//
//     [Fact]
//     public async Task SignUpAsync_ShouldLoginSuccessfully_WhenSignUpSucceed()
//     {
//         // Arrange
//         var signUpDto = new SignUpDto
//         {
//             Email = "some@example.com",
//             Password = "P@ssw0rd",
//             Firstname = "Jane",
//             Lastname = "Doe",
//             Role = "User",
//             Claims = new Dictionary<string, IEnumerable<string>>()
//         };
//         var signInDto = new SignInDto
//         {
//             Email = signUpDto.Email,
//             Password = signUpDto.Password
//         };
//
//         // Act
//         var responseSingUp = await _client.PostAsJsonAsync("/account/sign-up", signUpDto);
//         var responseSignin = await _client.PostAsJsonAsync("/account/sign-in", signInDto);
//
//         // Assert
//         responseSingUp.StatusCode.ShouldBe(HttpStatusCode.NoContent);
//
//         var tokenDto = await responseSignin.Content.ReadFromJsonAsync<JwtDto>();
//         responseSignin.StatusCode.ShouldBe(HttpStatusCode.OK);
//         tokenDto.ShouldNotBeNull();
//         tokenDto.AccessToken.ShouldNotBeEmpty();
//         tokenDto.Email.ShouldBe(signUpDto.Email);
//     }
//
//     [Fact]
//     public async Task SignUpAsync_ShouldReturnBadRequest_WhenPasswordIsInvalid()
//     {
//         // Arrange
//         var signUpDto = new SignUpDto
//         {
//             Email = "newuser@example.com",
//             Password = "pwd",
//             Firstname = "Jane",
//             Lastname = "Doe",
//             Role = "User",
//             Claims = new Dictionary<string, IEnumerable<string>>()
//         };
//
//         // Act
//         var response = await _client.PostAsJsonAsync("/account/sign-up", signUpDto);
//
//         // Assert
//         response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
//         var content = await response.Content.ReadFromJsonAsync<Error[]>();
//         content.ShouldNotBeNull();
//         content.Length.ShouldBe(1);
//         content.First().Code.ShouldBe("PasswordRequirementsException");
//     }
//
//     [Fact]
//     public async Task SignUpAsync_ShouldReturnBadRequest_WhenEmailIsInvalid()
//     {
//         // Arrange
//         var signUpDto = new SignUpDto();
//
//         // Act
//         var response = await _client.PostAsJsonAsync("/account/sign-up", signUpDto);
//
//         // Assert
//         response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
//         var content = await response.Content.ReadAsStringAsync();
//         content.ShouldNotBeNull();
//         content.ShouldContain("The Email field is not a valid e-mail address.");
//         content.ShouldContain("The Role field is required.");
//         content.ShouldContain("The Email field is required.");
//         content.ShouldContain("The Lastname field is required.");
//         content.ShouldContain("The Password field is required.");
//         content.ShouldContain("The Firstname field is required.");
//     }
//
//     private async Task SeedActiveUser(string email, string password, Guid id)
//     {
//         _passwordManager.CreatePasswordHash(password, out var passwordHash, out var passwordSalt);
//         var user = new User
//         {
//             Id = id,
//             Email = email,
//             Firstname = "John",
//             Lastname = "Doe",
//             CreatedAt = DateTime.UtcNow,
//             IsActive = true,
//             PasswordHash = passwordHash,
//             PasswordSalt = passwordSalt
//         };
//
//         var scope = _factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
//         var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
//         dbContext.Users.Add(user);
//         await dbContext.SaveChangesAsync();
//     }
// }