namespace WhatsAppAIAssistantBot.Domain.Services;

public interface IUserDataExtractionService
{
    Task<ExtractionResult> ExtractNameAsync(string message, string languageCode);
    Task<ExtractionResult> ExtractEmailAsync(string message, string languageCode);
    Task<UserDataExtractionResult> ExtractUserDataAsync(string message, string languageCode);
}

public class ExtractionResult
{
    public string? ExtractedValue { get; set; }
    public bool IsSuccessful => !string.IsNullOrEmpty(ExtractedValue);
    public ExtractionMethod Method { get; set; }
    public double Confidence { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UserDataExtractionResult
{
    public ExtractionResult? Name { get; set; }
    public ExtractionResult? Email { get; set; }
    public bool HasAnyData => (Name?.IsSuccessful == true) || (Email?.IsSuccessful == true);
}

public enum ExtractionMethod
{
    PatternMatching,
    LLMFallback,
    Failed
}