using WhatsAppAIAssistantBot.Domain.Entities;

namespace WhatsAppAIAssistantBot.Domain.Services;

/// <summary>
/// Service contract for managing user data persistence and retrieval operations.
/// Provides methods for creating, updating, and querying user records in the data store.
/// </summary>
public interface IUserStorageService
{
    /// <summary>
    /// Retrieves a user by their phone number identifier.
    /// </summary>
    /// <param name="phoneNumber">The WhatsApp phone number (e.g., "whatsapp:+1234567890")</param>
    /// <returns>The user entity if found, null otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when phoneNumber is null or empty</exception>
    Task<User?> GetUserByPhoneNumberAsync(string phoneNumber);
    /// <summary>
    /// Creates a new user or updates an existing user in the data store.
    /// Uses the phone number as the unique identifier for upsert operations.
    /// </summary>
    /// <param name="user">The user entity to create or update</param>
    /// <returns>The created or updated user entity</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when database operation fails</exception>
    Task<User> CreateOrUpdateUserAsync(User user);
    /// <summary>
    /// Updates a user's registration information with their name and email.
    /// This method is specifically for completing the user registration process.
    /// </summary>
    /// <param name="phoneNumber">The user's phone number identifier</param>
    /// <param name="name">The user's display name</param>
    /// <param name="email">The user's email address</param>
    /// <returns>A task representing the asynchronous update operation</returns>
    /// <exception cref="ArgumentException">Thrown when any parameter is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when user is not found or update fails</exception>
    Task UpdateUserRegistrationAsync(string phoneNumber, string name, string email);
}