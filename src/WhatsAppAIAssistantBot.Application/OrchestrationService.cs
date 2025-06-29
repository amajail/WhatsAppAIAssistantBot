using WhatsAppAIAssistantBot.Infrastructure;
using WhatsAppAIAssistantBot.Domain.Services;
using WhatsAppAIAssistantBot.Domain.Entities;

namespace WhatsAppAIAssistantBot.Application;

public interface IOrchestrationService
{
    Task HandleMessageAsync(string userId, string message);
}

public class OrchestrationService(ISemanticKernelService sk,
                                  IAssistantService assistant,
                                  ITwilioMessenger twilioMessenger,
                                  IUserStorageService userStorageService) : IOrchestrationService
{
    private readonly ISemanticKernelService _sk = sk;
    private readonly IAssistantService _assistant = assistant;
    private readonly IUserStorageService _userStorageService = userStorageService;

    public async Task HandleMessageAsync(string userId, string message)
    {
        // Get or create thread ID (this will also create user if needed)
        var threadId = await _assistant.CreateOrGetThreadAsync(userId);
        
        // Get user from database (should exist now)
        var user = await _userStorageService.GetUserByPhoneNumberAsync(userId);
        
        if (user != null && !user.IsRegistered)
        {
            await HandleUserRegistrationAsync(user, message);
            return;
        }

        // if (message.ToLower().Contains("time"))
        // {
        //     return await _sk.RunLocalSkillAsync(message);
        // }

        var reply = await _assistant.GetAssistantReplyAsync(threadId, message);
        await twilioMessenger.SendMessageAsync(userId, reply);
    }

    private async Task HandleUserRegistrationAsync(User user, string message)
    {
        if (string.IsNullOrEmpty(user.Name))
        {
            if (message.ToLower().StartsWith("name:") || message.ToLower().StartsWith("my name is"))
            {
                var name = ExtractNameFromMessage(message);
                if (!string.IsNullOrEmpty(name))
                {
                    await _userStorageService.UpdateUserRegistrationAsync(user.PhoneNumber, name, user.Email ?? string.Empty);
                    await twilioMessenger.SendMessageAsync(user.PhoneNumber, $"Hello {name}! Please provide your email address by typing 'Email: your@email.com'");
                    return;
                }
            }
            await twilioMessenger.SendMessageAsync(user.PhoneNumber, "Welcome! Please tell me your name by typing 'Name: Your Name' or 'My name is Your Name'");
            return;
        }

        if (string.IsNullOrEmpty(user.Email))
        {
            if (message.ToLower().StartsWith("email:"))
            {
                var email = ExtractEmailFromMessage(message);
                if (!string.IsNullOrEmpty(email) && IsValidEmail(email))
                {
                    await _userStorageService.UpdateUserRegistrationAsync(user.PhoneNumber, user.Name, email);
                    await twilioMessenger.SendMessageAsync(user.PhoneNumber, $"Thank you {user.Name}! Your registration is complete. How can I help you today?");
                    return;
                }
            }
            await twilioMessenger.SendMessageAsync(user.PhoneNumber, "Please provide your email address by typing 'Email: your@email.com'");
            return;
        }
    }

    private static string ExtractNameFromMessage(string message)
    {
        if (message.ToLower().StartsWith("name:"))
        {
            return message.Substring(5).Trim();
        }
        if (message.ToLower().StartsWith("my name is"))
        {
            return message.Substring(10).Trim();
        }
        return string.Empty;
    }

    private static string ExtractEmailFromMessage(string message)
    {
        if (message.ToLower().StartsWith("email:"))
        {
            return message.Substring(6).Trim();
        }
        return string.Empty;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
