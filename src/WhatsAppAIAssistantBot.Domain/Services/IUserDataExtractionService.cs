namespace WhatsAppAIAssistantBot.Domain.Services;

public interface IUserDataExtractionService
{
    Task<ExtractionResult> ExtractNameAsync(ExtractionRequest request);
    Task<ExtractionResult> ExtractEmailAsync(ExtractionRequest request);
    Task<UserDataExtractionResult> ExtractUserDataAsync(ExtractionRequest request);
}

public class ExtractionRequest
{
    public string Message { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string? ThreadId { get; set; }
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