using WhatsAppAIAssistantBot.Domain.Entities;

namespace WhatsAppAIAssistantBot.Domain.Services;

public interface IUserRegistrationService
{
    Task<RegistrationResult> ProcessRegistrationAsync(User user, string message);
    bool IsRegistrationComplete(User user);
    UserRegistrationState GetRegistrationState(User user);
}

public class RegistrationResult
{
    public bool IsCompleted { get; set; }
    public bool RequiresResponse { get; set; }
    public string? ResponseMessage { get; set; }
    public RegistrationAction Action { get; set; }
}

public enum RegistrationAction
{
    None,
    RequestName,
    RequestEmail,
    GreetWithName,
    CompleteRegistration,
    ShowInvalidEmail
}

public enum UserRegistrationState
{
    New,
    HasName,
    Complete
}