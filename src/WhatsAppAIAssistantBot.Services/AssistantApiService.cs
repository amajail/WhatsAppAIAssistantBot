namespace WhatsAppAIAssistantBot.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

public class AssistantApiService : IAssistantApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey = "OPENAI_API_KEY";
    private readonly string _assistantId = "YOUR_ASSISTANT_ID";

    public AssistantApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<string> GetAssistantReplyAsync(string threadId, string userMessage)
    {
        await _httpClient.PostAsJsonAsync($"https://api.openai.com/v1/threads/{threadId}/messages",
            new { role = "user", content = userMessage });

        var runRes = await _httpClient.PostAsJsonAsync($"https://api.openai.com/v1/threads/{threadId}/runs",
            new { assistant_id = _assistantId });

        var run = JsonNode.Parse(await runRes.Content.ReadAsStringAsync());
        var runId = run?["id"]?.ToString();

        while (true)
        {
            await Task.Delay(1000);
            var runStatusRes = await _httpClient.GetAsync($"https://api.openai.com/v1/threads/{threadId}/runs/{runId}");
            var status = JsonNode.Parse(await runStatusRes.Content.ReadAsStringAsync())?["status"]?.ToString();
            if (status == "completed") break;
        }

        var msgRes = await _httpClient.GetAsync($"https://api.openai.com/v1/threads/{threadId}/messages");
        var json = JsonNode.Parse(await msgRes.Content.ReadAsStringAsync());
        return json?["data"]?[0]?["content"]?[0]?["text"]?["value"]?.ToString();
    }

    public async Task<string> CreateOrGetThreadAsync(string userId)
    {
        var res = await _httpClient.PostAsync("https://api.openai.com/v1/threads", null);
        var json = JsonNode.Parse(await res.Content.ReadAsStringAsync());
        return json?["id"]?.ToString();
    }
}

public interface IAssistantApiService
{
    Task<string> GetAssistantReplyAsync(string threadId, string userMessage);
}