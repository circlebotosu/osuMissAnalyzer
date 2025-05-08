using circlebot.MissAnalyser;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

DotEnv.Load(".env");

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
    
app.MapControllers();

app.Run($"http://{app.Configuration["APP_HOST"]}:{app.Configuration["APP_PORT"]}");