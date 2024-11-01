// using System.Net;
// using System.Net.Http.Json;
// using Carhub.Service.Users.Core.DAL;
// using Carhub.Service.Users.Core.DTOs;
// using Carhub.Service.Users.Core.Entities;
// using Carhub.Service.Users.Core.Services;
// using CarHub.Users.Tests.Integration.Helpers;
// using Microsoft.AspNetCore.Mvc.Testing;
// using Microsoft.Extensions.DependencyInjection;
// using Shouldly;
//
// namespace CarHub.Users.Tests.Integration.Controllers;
//
// public sealed class AuthorizedAccountControllerTests : IClassFixture<AuthorizedApplicationFactory<Program>>
// {
//     private const string Password = "12345Abc!@";
//     private readonly HttpClient _client;
//     private readonly WebApplicationFactory<Program> _factory;
//     private readonly IPasswordManager _passwordManager;
//
//     public AuthorizedAccountControllerTests(AuthorizedApplicationFactory<Program> factory)
//     {
//         _factory = factory;
//         _client = _factory.CreateClient();
//         _passwordManager = new PasswordManager();
//     }
//
//
//     [Fact]
//     public async Task GetCurrentUserAsync_ShouldReturnOk_WhenUserExists()
//     {
//         // Arrange
//         var user = await SeedActiveUser(FakePolicyEvaluator.UserId);
//
//         // Act
//         var response = await _client.GetAsync("/account/me");
//         var content = await response.Content.ReadFromJsonAsync<UserDto>();
//
//         // Assert
//         response.StatusCode.ShouldBe(HttpStatusCode.OK);
//         content.ShouldNotBeNull();
//         content.Email.ShouldBe(user.Email);
//         content.Id.ShouldBe(FakePolicyEvaluator.UserId);
//     }
//
//     [Fact]
//     public async Task GetUserByIdAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
//     {
//         // Act
//         var response = await _client.GetAsync("/account/user/nonexistent-id");
//
//         // Assert
//         response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
//     }
//
//     [Fact]
//     public async Task GetUserByIdAsync_ShouldReturnOk_WhenUserExists()
//     {
//         // Arrange
//         var user = await SeedActiveUser(Guid.NewGuid());
//         var email = user.Email;
//
//         // Act
//         var response = await _client.GetAsync($"/account/user/{user.Id}");
//         var content = await response.Content.ReadFromJsonAsync<UserDto>();
//
//         // Assert
//         response.StatusCode.ShouldBe(HttpStatusCode.OK);
//         content.ShouldNotBeNull();
//         content.Email.ShouldBe(email);
//     }
//     
//     [Fact]
//     public async Task SignUpAdmin_RegistersAdmin_WhenUserIsAdmin()
//     {
//         // Arrange
//         var signUpDto = new SignUpDto
//         {
//             Email = "admin@example.com",
//             Password = "P@ssw0rd",
//             Role = "Admin"
//         };
//
//         // Act
//         var response = await _client.PostAsJsonAsync("/account/sign-up-admin", signUpDto);
//
//         // Assert
//         response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
//     }
//
//
//     private async Task<User> SeedActiveUser(Guid id)
//     {
//         var scope = _factory.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
//         var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
//         var user = dbContext.Users.FirstOrDefault(x => x.Id == id);
//         if (user is not null)
//             return user;
//
//         _passwordManager.CreatePasswordHash(Password, out var passwordHash, out var passwordSalt);
//         user = new User
//         {
//             Id = id,
//             Email = "valid@login.com",
//             Firstname = "John",
//             Lastname = "Doe",
//             CreatedAt = DateTime.UtcNow,
//             IsActive = true,
//             PasswordHash = passwordHash,
//             PasswordSalt = passwordSalt
//         };
//
//         dbContext.Users.Add(user);
//         await dbContext.SaveChangesAsync();
//
//         return user;
//     }
// }