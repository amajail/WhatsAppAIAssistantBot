using WhatsAppAIAssistantBot.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // <-- Add this line
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ITwilioMessenger, TwilioMessenger>();
builder.Services.AddScoped<ISemanticKernelService, SemanticKernelService>();
builder.Services.AddHttpClient<IAssistantApiService, AssistantApiService>();
builder.Services.AddScoped<IOrchestrationService, OrchestrationService>(); 

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers(); // <-- Add this line

app.Run();