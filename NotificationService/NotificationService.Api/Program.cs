using NotificationService.Application.Extensions;
using NotificationService.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddMessagingInfrastructure();
builder.Services.AddTwilioProvider(builder.Configuration);
builder.Services.AddSendGridProvider(builder.Configuration);
builder.Services.AddFirebaseProvider(builder.Configuration);
builder.Services.AddAwsSnsProvider(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();