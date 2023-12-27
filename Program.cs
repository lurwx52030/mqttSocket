using mqttSocket.Logger;
using mqttSocket.Models;
using mqttSocket.Services.mqtt;
using mqttSocket.Middlewares;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// 下達結束指令後一定時間內強制停機
builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(5));


// Add services to the container.
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddApplicationLogger();
});

// mqtt
builder.Services.Configure<MqttOption>(builder.Configuration.GetSection("MqttOption"));
builder.Services.AddHostedService<MqttServiceHost>();
builder.Services.AddSingleton<MqttService>();


builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.All);
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

// websocket
app.UseWebSockets();

app.UseRequestLog();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/a", () => new { status = 200, message = "a" });

app.MapGet("/m/{topic}/{data}", (string topic, string data) =>
{
    MqttService.PublishData(topic, data);
    return new { status = 200, topic, data };
});

app.Run();
