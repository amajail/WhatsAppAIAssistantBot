using BotTester.Models;
using BotTester.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddLogging();
builder.Services.AddHttpClient<BotApiClient>();
builder.Services.AddScoped<BotApiClient>();

var app = builder.Build();

// Configure webhook endpoint to receive mock Twilio responses
app.MapPost("/mock-twilio", ([FromForm] TwilioWebhookModel model, ILogger<Program> logger) =>
{
    // logger.LogInformation("Received webhook response from bot:");
    // logger.LogInformation("From: {From}", model.From);
    // logger.LogInformation("To: {To}", model.To);
    // logger.LogInformation("Body: {Body}", model.Body);
    // logger.LogInformation("MessageSid: {MessageSid}", model.MessageSid);
    
    Console.WriteLine($"\n🤖 Bot Response:");
    Console.WriteLine($"From: {model.From}");
    Console.WriteLine($"To: {model.To}");
    Console.WriteLine($"Message: {model.Body}");
    Console.WriteLine($"MessageSid: {model.MessageSid}");
    Console.WriteLine(new string('-', 50));
    
    return Results.Ok("Message received");
}).DisableAntiforgery();

// Start webhook server in background
var webhookPort = builder.Configuration.GetValue<int>("Webhook:Port", 5001);
var webhookPath = builder.Configuration.GetValue<string>("Webhook:Path", "/mock-twilio");

_ = Task.Run(async () =>
{
    try
    {
        await app.RunAsync($"http://localhost:{webhookPort}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error starting webhook server: {ex.Message}");
    }
});

// Wait for webhook server to start
await Task.Delay(2000);

Console.WriteLine("🚀 WhatsApp Bot Tester Console");
Console.WriteLine("===============================");
Console.WriteLine($"Webhook server running on: http://localhost:{webhookPort}{webhookPath}");
Console.WriteLine("Bot API URL: " + builder.Configuration["BotApi:BaseUrl"] + builder.Configuration["BotApi:WhatsAppEndpoint"]);
Console.WriteLine("Test Phone: " + builder.Configuration["TestUser:PhoneNumber"]);
Console.WriteLine("Bot Phone: " + builder.Configuration["TestUser:BotNumber"]);
Console.WriteLine("\nCommands:");
Console.WriteLine("- Type any message to send to bot");
Console.WriteLine("- Type 'quit' or 'exit' to close");
Console.WriteLine("- Type 'help' for available bot commands");
Console.WriteLine("- Type 'clear' to clear console");
Console.WriteLine("\nExample messages:");
Console.WriteLine("- Hello");
Console.WriteLine("- /slots");
Console.WriteLine("- /book 2024-01-15T14:00:00");
Console.WriteLine("- /lang en");
Console.WriteLine("- /help");
Console.WriteLine(new string('=', 50));

// Create service scope for dependency injection
using var scope = app.Services.CreateScope();
var botApiClient = scope.ServiceProvider.GetRequiredService<BotApiClient>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

// Command loop
while (true)
{
    Console.Write("\n💬 Enter message: ");
    var input = Console.ReadLine();
    
    if (string.IsNullOrWhiteSpace(input))
        continue;
    
    // Handle console commands
    switch (input.ToLower().Trim())
    {
        case "quit":
        case "exit":
            Console.WriteLine("👋 Goodbye!");
            return;
            
        case "clear":
            Console.Clear();
            continue;
            
        case "help":
            Console.WriteLine("\n🤖 Available Bot Commands:");
            Console.WriteLine("- /slots - Get available calendar slots");
            Console.WriteLine("- /book <datetime> - Book an appointment");
            Console.WriteLine("- /lang <en|es> - Change language");
            Console.WriteLine("- /help - Get bot help");
            Console.WriteLine("- /ayuda - Spanish help");
            Console.WriteLine("- /idioma <en|es> - Spanish language change");
            continue;
    }
    
    // Send message to bot
    Console.WriteLine($"📤 Sending to bot: {input}");
    
    try
    {
        var success = await botApiClient.SendMessageAsync(input);
        
        if (success)
        {
            Console.WriteLine("✅ Message sent successfully");
            Console.WriteLine("⏳ Waiting for bot response...");
            // Small delay to allow any logger output to complete
            await Task.Delay(100);
        }
        else
        {
            Console.WriteLine("❌ Failed to send message");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error sending message");
        Console.WriteLine($"❌ Error: {ex.Message}");
    }
}