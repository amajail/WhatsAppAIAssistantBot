using Microsoft.EntityFrameworkCore;
using WhatsAppAIAssistantBot.Domain.Entities;
using WhatsAppAIAssistantBot.Infrastructure.Data;
using WhatsAppAIAssistantBot.Infrastructure.Repositories;
using Xunit;

namespace WhatsAppAIAssistantBot.Tests;

public class UserRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserAndReturnWithId()
    {
        // Arrange
        var user = new User
        {
            PhoneNumber = "whatsapp:+1234567890",
            ThreadId = "thread_123",
            Name = "John Doe",
            Email = "john@example.com"
        };

        // Act
        var result = await _repository.AddAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.PhoneNumber, result.PhoneNumber);
        Assert.Equal(user.ThreadId, result.ThreadId);
        Assert.True(result.CreatedAt > DateTime.MinValue);
        Assert.True(result.UpdatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task GetByPhoneNumberAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var user = new User
        {
            PhoneNumber = "whatsapp:+1234567890",
            ThreadId = "thread_123",
            Name = "John Doe",
            Email = "john@example.com"
        };
        await _repository.AddAsync(user);

        // Act
        var result = await _repository.GetByPhoneNumberAsync(user.PhoneNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.PhoneNumber, result.PhoneNumber);
        Assert.Equal(user.Name, result.Name);
    }

    [Fact]
    public async Task GetByPhoneNumberAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByPhoneNumberAsync("whatsapp:+9999999999");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUserAndReturnModified()
    {
        // Arrange
        var user = new User
        {
            PhoneNumber = "whatsapp:+1234567890",
            ThreadId = "thread_123",
            Name = "John Doe",
            Email = "john@example.com"
        };
        await _repository.AddAsync(user);

        // Act
        user.Name = "Jane Doe";
        user.Email = "jane@example.com";
        var result = await _repository.UpdateAsync(user);

        // Assert
        Assert.Equal("Jane Doe", result.Name);
        Assert.Equal("jane@example.com", result.Email);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenUserExists()
    {
        // Arrange
        var user = new User
        {
            PhoneNumber = "whatsapp:+1234567890",
            ThreadId = "thread_123"
        };
        await _repository.AddAsync(user);

        // Act
        var exists = await _repository.ExistsAsync(user.PhoneNumber);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Act
        var exists = await _repository.ExistsAsync("whatsapp:+9999999999");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetRegisteredUsersAsync_ShouldReturnOnlyRegisteredUsers()
    {
        // Arrange
        var registeredUser = new User
        {
            PhoneNumber = "whatsapp:+1111111111",
            ThreadId = "thread_1",
            Name = "John Doe",
            Email = "john@example.com"
        };
        var unregisteredUser = new User
        {
            PhoneNumber = "whatsapp:+2222222222",
            ThreadId = "thread_2"
        };

        await _repository.AddAsync(registeredUser);
        await _repository.AddAsync(unregisteredUser);

        // Act
        var registeredUsers = await _repository.GetRegisteredUsersAsync();

        // Assert
        Assert.Single(registeredUsers);
        Assert.Equal(registeredUser.PhoneNumber, registeredUsers.First().PhoneNumber);
    }

    [Fact]
    public async Task GetUserCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var user1 = new User { PhoneNumber = "whatsapp:+1111111111", ThreadId = "thread_1" };
        var user2 = new User { PhoneNumber = "whatsapp:+2222222222", ThreadId = "thread_2" };

        await _repository.AddAsync(user1);
        await _repository.AddAsync(user2);

        // Act
        var count = await _repository.GetUserCountAsync();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUser()
    {
        // Arrange
        var user = new User
        {
            PhoneNumber = "whatsapp:+1234567890",
            ThreadId = "thread_123"
        };
        await _repository.AddAsync(user);

        // Act
        await _repository.DeleteAsync(user.PhoneNumber);

        // Assert
        var deletedUser = await _repository.GetByPhoneNumberAsync(user.PhoneNumber);
        Assert.Null(deletedUser);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}