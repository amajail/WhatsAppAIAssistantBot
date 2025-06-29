using Microsoft.EntityFrameworkCore;
using WhatsAppAIAssistantBot.Domain.Entities;
using WhatsAppAIAssistantBot.Domain.Services;
using WhatsAppAIAssistantBot.Infrastructure.Data;

namespace WhatsAppAIAssistantBot.Infrastructure.Services;

public class UserStorageService : IUserStorageService
{
    private readonly ApplicationDbContext _context;

    public UserStorageService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
    }

    public async Task<User> CreateOrUpdateUserAsync(User user)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == user.PhoneNumber);
        
        if (existingUser == null)
        {
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Add(user);
        }
        else
        {
            existingUser.ThreadId = user.ThreadId;
            existingUser.Name = user.Name ?? existingUser.Name;
            existingUser.Email = user.Email ?? existingUser.Email;
            existingUser.UpdatedAt = DateTime.UtcNow;
            user = existingUser;
        }

        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateUserRegistrationAsync(string phoneNumber, string name, string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user != null)
        {
            user.Name = name;
            user.Email = email;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}