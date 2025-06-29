using WhatsAppAIAssistantBot.Domain.Entities;

namespace WhatsAppAIAssistantBot.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    Task<User> AddAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteAsync(string phoneNumber);
    Task<bool> ExistsAsync(string phoneNumber);
    Task<IEnumerable<User>> GetAllAsync();
    Task<IEnumerable<User>> GetRegisteredUsersAsync();
    Task<int> GetUserCountAsync();
}