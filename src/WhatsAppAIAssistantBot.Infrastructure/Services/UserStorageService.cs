using WhatsAppAIAssistantBot.Domain.Entities;
using WhatsAppAIAssistantBot.Domain.Services;
using WhatsAppAIAssistantBot.Domain.Repositories;

namespace WhatsAppAIAssistantBot.Infrastructure.Services;

public class UserStorageService : IUserStorageService
{
    private readonly IUserRepository _userRepository;

    public UserStorageService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> GetUserByPhoneNumberAsync(string phoneNumber)
    {
        return await _userRepository.GetByPhoneNumberAsync(phoneNumber);
    }

    public async Task<User> CreateOrUpdateUserAsync(User user)
    {
        var existingUser = await _userRepository.GetByPhoneNumberAsync(user.PhoneNumber);
        
        if (existingUser == null)
        {
            return await _userRepository.AddAsync(user);
        }
        else
        {
            // Update existing user properties
            existingUser.ThreadId = user.ThreadId;
            existingUser.Name = user.Name ?? existingUser.Name;
            existingUser.Email = user.Email ?? existingUser.Email;
            return await _userRepository.UpdateAsync(existingUser);
        }
    }

    public async Task UpdateUserRegistrationAsync(string phoneNumber, string name, string email)
    {
        var user = await _userRepository.GetByPhoneNumberAsync(phoneNumber);
        if (user != null)
        {
            user.Name = name;
            user.Email = email;
            await _userRepository.UpdateAsync(user);
        }
    }
}