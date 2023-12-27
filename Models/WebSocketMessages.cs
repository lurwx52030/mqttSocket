namespace mqttSocket.Models;


public record WebSocketMqttMessage(string topic, List<double> data);
// public class WebSocketMessage
// {
//     public string topic { get; set; }
//     public List<int> data { get; set; }
// }