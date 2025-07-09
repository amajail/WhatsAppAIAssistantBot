using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WhatsAppAIAssistantBot.Domain.Services;

namespace WhatsAppAIAssistantBot.Application.Services;

public class UserDataExtractionService : IUserDataExtractionService
{
    private readonly ILocalizationService _localizationService;
    private readonly IAssistantService _assistantService;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly ILogger<UserDataExtractionService> _logger;

    public UserDataExtractionService(
        ILocalizationService localizationService,
        IAssistantService assistantService,
        IChatCompletionService chatCompletionService,
        ILogger<UserDataExtractionService> logger)
    {
        _localizationService = localizationService;
        _assistantService = assistantService;
        _chatCompletionService = chatCompletionService;
        _logger = logger;
    }

    public async Task<ExtractionResult> ExtractNameAsync(ExtractionRequest request)
    {
        _logger.LogInformation("Extracting name from message using language: {LanguageCode}, threadId: {ThreadId}", 
            request.LanguageCode, request.ThreadId);

        // First try pattern matching
        var patternResult = await ExtractNameWithPatternsAsync(request.Message, request.LanguageCode);
        if (patternResult.IsSuccessful)
        {
            _logger.LogInformation("Name extracted using pattern matching: {Name}", patternResult.ExtractedValue);
            return patternResult;
        }

        // Fallback to LLM
        _logger.LogInformation("Pattern matching failed, trying LLM fallback");
        var llmResult = await ExtractNameWithLLMAsync(request);
        if (llmResult.IsSuccessful)
        {
            _logger.LogInformation("Name extracted using LLM: {Name}", llmResult.ExtractedValue);
        }
        else
        {
            _logger.LogWarning("Both pattern matching and LLM extraction failed for name");
        }

        return llmResult;
    }

    public async Task<ExtractionResult> ExtractEmailAsync(ExtractionRequest request)
    {
        _logger.LogInformation("Extracting email from message using language: {LanguageCode}, threadId: {ThreadId}", 
            request.LanguageCode, request.ThreadId);

        // First try pattern matching
        var patternResult = await ExtractEmailWithPatternsAsync(request.Message, request.LanguageCode);
        if (patternResult.IsSuccessful)
        {
            _logger.LogInformation("Email extracted using pattern matching: {Email}", patternResult.ExtractedValue);
            return patternResult;
        }

        // Fallback to LLM
        _logger.LogInformation("Pattern matching failed, trying LLM fallback");
        var llmResult = await ExtractEmailWithLLMAsync(request);
        if (llmResult.IsSuccessful)
        {
            _logger.LogInformation("Email extracted using LLM: {Email}", llmResult.ExtractedValue);
        }
        else
        {
            _logger.LogWarning("Both pattern matching and LLM extraction failed for email");
        }

        return llmResult;
    }

    public async Task<UserDataExtractionResult> ExtractUserDataAsync(ExtractionRequest request)
    {
        _logger.LogInformation("Extracting user data from message using language: {LanguageCode}, threadId: {ThreadId}", 
            request.LanguageCode, request.ThreadId);

        var result = new UserDataExtractionResult();

        // Try to extract both name and email in one go with LLM if the message seems to contain both
        if (await MessageContainsBothDataAsync(request.Message, request.LanguageCode))
        {
            _logger.LogInformation("Message appears to contain both name and email, trying combined LLM extraction");
            var combinedResult = await ExtractBothWithLLMAsync(request);
            if (combinedResult.Name?.IsSuccessful == true || combinedResult.Email?.IsSuccessful == true)
            {
                return combinedResult;
            }
        }

        // Fall back to individual extraction
        result.Name = await ExtractNameAsync(request);
        result.Email = await ExtractEmailAsync(request);

        return result;
    }

    private async Task<ExtractionResult> ExtractNameWithPatternsAsync(string message, string languageCode)
    {
        try
        {
            var patterns = await GetNamePatternsAsync(languageCode);
            var lowerMessage = message.ToLower();

            foreach (var pattern in patterns)
            {
                var lowerPattern = pattern.ToLower();
                if (lowerMessage.StartsWith(lowerPattern))
                {
                    var extractedName = message.Substring(pattern.Length).Trim();
                    if (!string.IsNullOrEmpty(extractedName) && IsValidName(extractedName))
                    {
                        return new ExtractionResult
                        {
                            ExtractedValue = extractedName,
                            Method = ExtractionMethod.PatternMatching,
                            Confidence = 0.9
                        };
                    }
                }
            }

            return new ExtractionResult
            {
                Method = ExtractionMethod.Failed,
                Confidence = 0.0,
                ErrorMessage = "No valid name pattern found"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pattern-based name extraction");
            return new ExtractionResult
            {
                Method = ExtractionMethod.Failed,
                Confidence = 0.0,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<ExtractionResult> ExtractEmailWithPatternsAsync(string message, string languageCode)
    {
        try
        {
            var patterns = await GetEmailPatternsAsync(languageCode);
            var lowerMessage = message.ToLower();

            foreach (var pattern in patterns)
            {
                var lowerPattern = pattern.ToLower();
                if (lowerMessage.StartsWith(lowerPattern))
                {
                    var extractedEmail = message.Substring(pattern.Length).Trim();
                    if (!string.IsNullOrEmpty(extractedEmail) && IsValidEmail(extractedEmail))
                    {
                        return new ExtractionResult
                        {
                            ExtractedValue = extractedEmail,
                            Method = ExtractionMethod.PatternMatching,
                            Confidence = 0.9
                        };
                    }
                }
            }

            // Also try regex pattern for email extraction
            var emailRegex = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b");
            var match = emailRegex.Match(message);
            if (match.Success && IsValidEmail(match.Value))
            {
                return new ExtractionResult
                {
                    ExtractedValue = match.Value,
                    Method = ExtractionMethod.PatternMatching,
                    Confidence = 0.8
                };
            }

            return new ExtractionResult
            {
                Method = ExtractionMethod.Failed,
                Confidence = 0.0,
                ErrorMessage = "No valid email pattern found"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pattern-based email extraction");
            return new ExtractionResult
            {
                Method = ExtractionMethod.Failed,
                Confidence = 0.0,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<ExtractionResult> ExtractNameWithLLMAsync(ExtractionRequest request)
    {
        try
        {
            var prompt = await _localizationService.GetLocalizedMessageAsync(
                LocalizationKeys.LlmExtractNamePrompt, request.LanguageCode, request.Message);

            string response;
            
            // TODO: REVIEW - Consider if using main thread for extraction is necessary or just use stateless completion
            // First try: Use main thread if available
            if (!string.IsNullOrEmpty(request.ThreadId))
            {
                _logger.LogDebug("Attempting name extraction using main thread: {ThreadId}", request.ThreadId);
                try
                {
                    response = await _assistantService.GetAssistantReplyAsync(request.ThreadId, prompt);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Main thread extraction failed, falling back to stateless completion");
                    response = await _chatCompletionService.GetCompletionAsync(prompt);
                }
            }
            else
            {
                // No thread available, use stateless completion
                _logger.LogDebug("No thread available, using stateless completion for name extraction");
                response = await _chatCompletionService.GetCompletionAsync(prompt);
            }

            if (response.Trim().Equals("NO_NAME_FOUND", StringComparison.OrdinalIgnoreCase))
            {
                return new ExtractionResult
                {
                    Method = ExtractionMethod.Failed,
                    Confidence = 0.0,
                    ErrorMessage = "LLM could not find name"
                };
            }

            var extractedName = response.Trim();
            if (IsValidName(extractedName))
            {
                return new ExtractionResult
                {
                    ExtractedValue = extractedName,
                    Method = ExtractionMethod.LLMFallback,
                    Confidence = 0.7
                };
            }

            return new ExtractionResult
            {
                Method = ExtractionMethod.Failed,
                Confidence = 0.0,
                ErrorMessage = "LLM response not a valid name"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LLM-based name extraction");
            return new ExtractionResult
            {
                Method = ExtractionMethod.Failed,
                Confidence = 0.0,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<ExtractionResult> ExtractEmailWithLLMAsync(ExtractionRequest request)
    {
        try
        {
            var prompt = await _localizationService.GetLocalizedMessageAsync(
                LocalizationKeys.LlmExtractEmailPrompt, request.LanguageCode, request.Message);

            string response;
            
            // First try: Use main thread if available
            if (!string.IsNullOrEmpty(request.ThreadId))
            {
                _logger.LogDebug("Attempting email extraction using main thread: {ThreadId}", request.ThreadId);
                try
                {
                    response = await _assistantService.GetAssistantReplyAsync(request.ThreadId, prompt);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Main thread extraction failed, falling back to stateless completion");
                    response = await _chatCompletionService.GetCompletionAsync(prompt);
                }
            }
            else
            {
                // No thread available, use stateless completion
                _logger.LogDebug("No thread available, using stateless completion for email extraction");
                response = await _chatCompletionService.GetCompletionAsync(prompt);
            }

            if (response.Trim().Equals("NO_EMAIL_FOUND", StringComparison.OrdinalIgnoreCase))
            {
                return new ExtractionResult
                {
                    Method = ExtractionMethod.Failed,
                    Confidence = 0.0,
                    ErrorMessage = "LLM could not find email"
                };
            }

            var extractedEmail = response.Trim();
            if (IsValidEmail(extractedEmail))
            {
                return new ExtractionResult
                {
                    ExtractedValue = extractedEmail,
                    Method = ExtractionMethod.LLMFallback,
                    Confidence = 0.7
                };
            }

            return new ExtractionResult
            {
                Method = ExtractionMethod.Failed,
                Confidence = 0.0,
                ErrorMessage = "LLM response not a valid email"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LLM-based email extraction");
            return new ExtractionResult
            {
                Method = ExtractionMethod.Failed,
                Confidence = 0.0,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<UserDataExtractionResult> ExtractBothWithLLMAsync(ExtractionRequest request)
    {
        try
        {
            var prompt = await _localizationService.GetLocalizedMessageAsync(
                LocalizationKeys.LlmExtractBothPrompt, request.LanguageCode, request.Message);

            string response;
            
            // First try: Use main thread if available
            if (!string.IsNullOrEmpty(request.ThreadId))
            {
                _logger.LogDebug("Attempting combined extraction using main thread: {ThreadId}", request.ThreadId);
                try
                {
                    response = await _assistantService.GetAssistantReplyAsync(request.ThreadId, prompt);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Main thread extraction failed, falling back to stateless completion");
                    response = await _chatCompletionService.GetCompletionAsync(prompt);
                }
            }
            else
            {
                // No thread available, use stateless completion
                _logger.LogDebug("No thread available, using stateless completion for combined extraction");
                response = await _chatCompletionService.GetCompletionAsync(prompt);
            }

            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response.Trim());
            
            var result = new UserDataExtractionResult();

            if (jsonResponse.TryGetProperty("name", out var nameElement) && 
                nameElement.ValueKind == JsonValueKind.String)
            {
                var name = nameElement.GetString();
                if (!string.IsNullOrEmpty(name) && !name.Equals("null", StringComparison.OrdinalIgnoreCase) && IsValidName(name))
                {
                    result.Name = new ExtractionResult
                    {
                        ExtractedValue = name,
                        Method = ExtractionMethod.LLMFallback,
                        Confidence = 0.7
                    };
                }
            }

            if (jsonResponse.TryGetProperty("email", out var emailElement) && 
                emailElement.ValueKind == JsonValueKind.String)
            {
                var email = emailElement.GetString();
                if (!string.IsNullOrEmpty(email) && !email.Equals("null", StringComparison.OrdinalIgnoreCase) && IsValidEmail(email))
                {
                    result.Email = new ExtractionResult
                    {
                        ExtractedValue = email,
                        Method = ExtractionMethod.LLMFallback,
                        Confidence = 0.7
                    };
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LLM-based combined extraction");
            return new UserDataExtractionResult
            {
                Name = new ExtractionResult
                {
                    Method = ExtractionMethod.Failed,
                    Confidence = 0.0,
                    ErrorMessage = ex.Message
                },
                Email = new ExtractionResult
                {
                    Method = ExtractionMethod.Failed,
                    Confidence = 0.0,
                    ErrorMessage = ex.Message
                }
            };
        }
    }

    private async Task<string[]> GetNamePatternsAsync(string languageCode)
    {
        var patternsJson = await _localizationService.GetLocalizedMessageAsync(
            LocalizationKeys.NamePatterns, languageCode);
        return JsonSerializer.Deserialize<string[]>(patternsJson) ?? Array.Empty<string>();
    }

    private async Task<string[]> GetEmailPatternsAsync(string languageCode)
    {
        var patternsJson = await _localizationService.GetLocalizedMessageAsync(
            LocalizationKeys.EmailPatterns, languageCode);
        return JsonSerializer.Deserialize<string[]>(patternsJson) ?? Array.Empty<string>();
    }

    private Task<bool> MessageContainsBothDataAsync(string message, string languageCode)
    {
        // Simple heuristic: check if message contains both @ symbol (likely email) and multiple words (likely name)
        var hasEmailIndicator = message.Contains("@");
        var wordCount = message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return Task.FromResult(hasEmailIndicator && wordCount >= 3);
    }

    private static bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 100)
            return false;

        // Check if it contains at least one letter
        return name.Any(char.IsLetter);
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