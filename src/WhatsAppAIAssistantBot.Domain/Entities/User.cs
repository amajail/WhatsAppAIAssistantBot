namespace WhatsAppAIAssistantBot.Domain.Entities;

public class User
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string ThreadId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsRegistered => !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Email);
}