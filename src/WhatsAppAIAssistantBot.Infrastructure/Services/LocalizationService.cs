using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WhatsAppAIAssistantBot.Domain.Models;
using WhatsAppAIAssistantBot.Domain.Services;

namespace WhatsAppAIAssistantBot.Infrastructure.Services;

public class LocalizationService : ILocalizationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalizationService> _logger;
    private readonly Dictionary<string, Dictionary<string, string>> _messages;
    private readonly SupportedLanguage _defaultLanguage;

    public LocalizationService(IConfiguration configuration, ILogger<LocalizationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _messages = new Dictionary<string, Dictionary<string, string>>();
        _defaultLanguage = SupportedLanguageExtensions.FromCode(
            _configuration["Localization:DefaultLanguage"] ?? "es"
        );
        
        LoadLanguageResources();
    }

    public async Task<string> GetLocalizedMessageAsync(string key, SupportedLanguage language, params object[] parameters)
    {
        return await GetLocalizedMessageAsync(key, language.ToCode(), parameters);
    }

    public async Task<string> GetLocalizedMessageAsync(string key, string languageCode, params object[] parameters)
    {
        await Task.CompletedTask; // Make method async for future extensibility
        
        var normalizedLanguageCode = languageCode?.ToLower() ?? _defaultLanguage.ToCode();
        
        // Try to get message in requested language
        if (_messages.TryGetValue(normalizedLanguageCode, out var languageMessages))
        {
            if (languageMessages.TryGetValue(key, out var message))
            {
                return FormatMessage(message, parameters);
            }
        }
        
        // Fallback to default language
        var defaultCode = _defaultLanguage.ToCode();
        if (normalizedLanguageCode != defaultCode && _messages.TryGetValue(defaultCode, out var defaultMessages))
        {
            if (defaultMessages.TryGetValue(key, out var defaultMessage))
            {
                _logger.LogWarning("Message key '{Key}' not found for language '{Language}', using default language '{DefaultLanguage}'", 
                    key, languageCode, defaultCode);
                return FormatMessage(defaultMessage, parameters);
            }
        }
        
        // Last resort: return the key itself
        _logger.LogError("Message key '{Key}' not found in any language", key);
        return key;
    }

    public async Task<SupportedLanguage> GetDefaultLanguageAsync()
    {
        await Task.CompletedTask;
        return _defaultLanguage;
    }

    public async Task<bool> IsLanguageSupportedAsync(string languageCode)
    {
        await Task.CompletedTask;
        return _messages.ContainsKey(languageCode?.ToLower() ?? string.Empty);
    }

    public async Task<Dictionary<string, string>> GetAllMessagesAsync(SupportedLanguage language)
    {
        await Task.CompletedTask;
        var languageCode = language.ToCode();
        
        if (_messages.TryGetValue(languageCode, out var messages))
        {
            return new Dictionary<string, string>(messages);
        }
        
        return new Dictionary<string, string>();
    }

    private void LoadLanguageResources()
    {
        var supportedLanguages = new[] { "es", "en" };
        
        foreach (var languageCode in supportedLanguages)
        {
            try
            {
                var resourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", $"{languageCode}.json");
                _logger.LogDebug("Trying resource path: {ResourcePath}", resourcePath);
                
                // If running from source, try relative path
                if (!File.Exists(resourcePath))
                {
                    var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                    resourcePath = Path.Combine(assemblyDirectory ?? string.Empty, "Resources", $"{languageCode}.json");
                    _logger.LogDebug("Trying assembly resource path: {ResourcePath}", resourcePath);
                }
                
                // Try project structure path
                if (!File.Exists(resourcePath))
                {
                    var currentDirectory = Directory.GetCurrentDirectory();
                    resourcePath = Path.Combine(currentDirectory, "src", "WhatsAppAIAssistantBot.Infrastructure", "Resources", $"{languageCode}.json");
                    _logger.LogDebug("Trying project structure path: {ResourcePath}", resourcePath);
                }
                
                // Try Infrastructure project relative path
                if (!File.Exists(resourcePath))
                {
                    var currentDirectory = Directory.GetCurrentDirectory();
                    resourcePath = Path.Combine(currentDirectory, "Resources", $"{languageCode}.json");
                    _logger.LogDebug("Trying relative path: {ResourcePath}", resourcePath);
                }
                
                // Try bin directory relative path (for production)
                if (!File.Exists(resourcePath))
                {
                    var currentDirectory = Directory.GetCurrentDirectory();
                    resourcePath = Path.Combine(currentDirectory, "bin", "Debug", "net8.0", "Resources", $"{languageCode}.json");
                    _logger.LogDebug("Trying bin debug path: {ResourcePath}", resourcePath);
                }
                
                // Try bin release directory relative path (for production)
                if (!File.Exists(resourcePath))
                {
                    var currentDirectory = Directory.GetCurrentDirectory();
                    resourcePath = Path.Combine(currentDirectory, "bin", "Release", "net8.0", "Resources", $"{languageCode}.json");
                    _logger.LogDebug("Trying bin release path: {ResourcePath}", resourcePath);
                }
                
                if (File.Exists(resourcePath))
                {
                    var jsonContent = File.ReadAllText(resourcePath);
                    var messages = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                    
                    if (messages != null)
                    {
                        _messages[languageCode] = messages;
                        _logger.LogInformation("Loaded {Count} messages for language '{Language}'", messages.Count, languageCode);
                    }
                }
                else
                {
                    var currentDir = Directory.GetCurrentDirectory();
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    _logger.LogWarning("Language resource file not found for '{Language}'. Searched paths included: {ResourcePath}. CurrentDir: {CurrentDir}, BaseDir: {BaseDir}. Using fallback messages.", 
                        languageCode, resourcePath, currentDir, baseDir);
                    
                    // Create fallback messages
                    _messages[languageCode] = CreateFallbackMessages(languageCode);
                    _logger.LogInformation("Created {Count} fallback messages for language '{Language}'", 
                        _messages[languageCode].Count, languageCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading language resources for '{Language}'", languageCode);
                
                // Create fallback messages
                _messages[languageCode] = CreateFallbackMessages(languageCode);
            }
        }
    }

    private static Dictionary<string, string> CreateFallbackMessages(string languageCode)
    {
        return languageCode switch
        {
            "es" => new Dictionary<string, string>
            {
                { LocalizationKeys.WelcomeMessage, "¡Bienvenido! Por favor dime tu nombre." },
                { LocalizationKeys.RequestName, "Por favor dime tu nombre." },
                { LocalizationKeys.RequestEmail, "Por favor proporciona tu email." },
                { LocalizationKeys.RegistrationComplete, "¡Gracias! Tu registro está completo." },
                { LocalizationKeys.GeneralError, "Ha ocurrido un error." },
                { LocalizationKeys.ContextTemplate, "[CONTEXTO DEL USUARIO: Nombre: {0}, Email: {1}, Idioma: {2}]\n\nMensaje del usuario: {3}" },
                { LocalizationKeys.ContextTemplateMinimal, "[CONTEXTO DEL USUARIO: Nombre: {0}]\n\nMensaje del usuario: {1}" },
                { LocalizationKeys.ContextTemplateFull, "[CONTEXTO DEL USUARIO: Nombre: {0}, Email: {1}, Idioma: {2}, Miembro desde: {3}, Zona horaria: {4}]\n\nMensaje del usuario: {5}" },
                { LocalizationKeys.PersonalQuestionPatterns, "[\"mi nombre\", \"como me llamo\", \"cual es mi\", \"mi email\", \"mi correo\", \"quien soy\", \"mis datos\", \"mi información\"]" },
                { LocalizationKeys.NameQuestionPatterns, "[\"mi nombre\", \"como me llamo\", \"quien soy\", \"mi nombre es\"]" },
                { LocalizationKeys.EmailQuestionPatterns, "[\"mi email\", \"mi correo\", \"mi dirección de email\", \"como contactarme\"]" }
            },
            "en" => new Dictionary<string, string>
            {
                { LocalizationKeys.WelcomeMessage, "Welcome! Please tell me your name." },
                { LocalizationKeys.RequestName, "Please tell me your name." },
                { LocalizationKeys.RequestEmail, "Please provide your email." },
                { LocalizationKeys.RegistrationComplete, "Thank you! Your registration is complete." },
                { LocalizationKeys.GeneralError, "An error occurred." },
                { LocalizationKeys.ContextTemplate, "[USER CONTEXT: Name: {0}, Email: {1}, Language: {2}]\n\nUser message: {3}" },
                { LocalizationKeys.ContextTemplateMinimal, "[USER CONTEXT: Name: {0}]\n\nUser message: {1}" },
                { LocalizationKeys.ContextTemplateFull, "[USER CONTEXT: Name: {0}, Email: {1}, Language: {2}, Member since: {3}, Timezone: {4}]\n\nUser message: {5}" },
                { LocalizationKeys.PersonalQuestionPatterns, "[\"my name\", \"what's my\", \"what is my\", \"my email\", \"who am i\", \"my info\", \"my information\", \"about me\"]" },
                { LocalizationKeys.NameQuestionPatterns, "[\"my name\", \"what's my name\", \"who am i\", \"what do you call me\"]" },
                { LocalizationKeys.EmailQuestionPatterns, "[\"my email\", \"my email address\", \"how to contact me\", \"reach me\"]" }
            },
            _ => new Dictionary<string, string>()
        };
    }

    private static string FormatMessage(string message, params object[] parameters)
    {
        try
        {
            return parameters.Length > 0 ? string.Format(message, parameters) : message;
        }
        catch (FormatException)
        {
            return message; // Return unformatted message if formatting fails
        }
    }
}