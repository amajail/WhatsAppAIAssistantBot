using WhatsAppAIAssistantBot.Domain.Entities;

namespace WhatsAppAIAssistantBot.Domain.Services;

public interface IUserStorageService
{
    Task<User?> GetUserByPhoneNumberAsync(string phoneNumber);
    Task<User> CreateOrUpdateUserAsync(User user);
    Task UpdateUserRegistrationAsync(string phoneNumber, string name, string email);
}