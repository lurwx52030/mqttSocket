using System.Net.WebSockets;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using Microsoft.AspNetCore.Mvc;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace mqttSocket.Controllers
{
    // [Route("[controller]")]
    public class WsController : Controller
    {
        private readonly ILogger<WsController> Logger;
        private static Tuple<string, string> mqttPayload;

        private static IMqttClient mqttClient;

        public WsController(ILogger<WsController> logger)
        {
            Logger = logger;
            mqttPayload = new Tuple<string, string>("", "");

            mqttClient = GetMqttClient();
            var mqttClientConnectOption = new MqttClientOptionsBuilder()
                                                .WithClientId("websocketServer")
                                                .WithTcpServer("localhost", 1883)
                                                .Build();

            mqttClient.ConnectAsync(mqttClientConnectOption).Wait();
        }

        [Route("/ws")]
        public async Task WsReceiver()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await ProcessWebSocket(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private IMqttClient GetMqttClient()
        {
            var mqttClient = new MqttFactory().CreateMqttClient();

            mqttClient.ApplicationMessageReceivedAsync += (MqttApplicationMessageReceivedEventArgs arg) =>
            {
                var topic = arg?.ApplicationMessage?.Topic;
                var payloadText = Encoding.UTF8.GetString(
                    arg?.ApplicationMessage?.PayloadSegment ?? Array.Empty<byte>()
                );

                mqttPayload = (topic!, payloadText).ToTuple();
                return Task.CompletedTask;
            };

            return mqttClient;
        }

        JsonNode? parseJsonMqttRequest(string? receiveData)
        {
            JsonNode? topicRequest;

            try
            {
                topicRequest = JsonSerializer.Deserialize<JsonNode>(receiveData!);
                topicRequest?["topic"]?.GetValue<string>();
            }
            catch (System.Text.Json.JsonException)
            {
                topicRequest = null;
            }
            catch (System.InvalidOperationException)
            {
                topicRequest = null;
            }

            return topicRequest;
        }


        private async Task ProcessWebSocket(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            var resultBuffer = new byte[1024];
            JsonNode? topicRequest;

            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!receiveResult.CloseStatus.HasValue)
            {
                var rb = buffer.Where(b => b != 0).ToArray();
                var receiveData = Encoding.UTF8.GetString(rb);
                // update state of topicRequest
                topicRequest = parseJsonMqttRequest(receiveData);

                if (topicRequest == null)
                {
                    resultBuffer = Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(
                            new
                            {
                                message = "The topic is undefined; below is the data you sent.",
                                data = receiveData
                            }
                        )
                    );
                }
                else if (mqttPayload.Item1 != null)
                {
                    var topics = topicRequest?["topic"]?.GetValue<string>().Split("/");

                    await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic($"{topics[0]}/#").Build());

                    var result = JsonSerializer.Serialize(
                        new { topic = mqttPayload.Item1, data = mqttPayload.Item2 },
                        new JsonSerializerOptions
                        {
                            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.All)
                        }
                    );
                    resultBuffer = Encoding.UTF8.GetBytes(result);
                }
                await webSocket.SendAsync(
                        new ArraySegment<byte>(resultBuffer),
                        receiveResult.MessageType,
                        receiveResult.EndOfMessage,
                        CancellationToken.None);

                receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None);
        }
    }
}