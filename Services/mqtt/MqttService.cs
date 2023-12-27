using System.Text;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace mqttSocket.Services.mqtt;

public class MqttService
{

    /// <summary>
    ///  mqttnet  Server
    /// </summary>
    public static MqttServer MqttServer { get; set; }

    public static void PublishData(string topic, string data)
    {
        var message = new MqttApplicationMessage
        {
            Topic = topic,
            PayloadSegment = Encoding.Default.GetBytes(data),
            QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
            Retain = true
        };

        MqttServer.InjectApplicationMessage(new InjectedMqttApplicationMessage(message)
        {
            SenderClientId = "mainServer"
        }).GetAwaiter().GetResult();
    }
}