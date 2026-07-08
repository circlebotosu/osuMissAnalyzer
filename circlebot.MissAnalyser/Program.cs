using circlebot.MissAnalyser;
using circlebot.MissAnalyser.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

DotEnv.Load(".env");

builder.Services.AddControllers();
builder.Services
    .AddExceptionHandler<ExceptionLoggingHandler>()
    .AddProblemDetails();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.SetMinimumLevel(
#if DEBUG
    LogLevel.Trace
#else
    LogLevel.Information
#endif
);


var app = builder.Build();

app.UseExceptionHandler(_ => { });

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
    
app.MapControllers();

app.Run($"http://{app.Configuration["APP_HOST"]}:{app.Configuration["APP_PORT"]}");