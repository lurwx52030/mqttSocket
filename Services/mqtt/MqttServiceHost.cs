using System.Text;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Server;
using mqttSocket.Models;

namespace mqttSocket.Services.mqtt;

public class MqttServiceHost : IHostedService, IDisposable
{
    public readonly MqttService MqttServer;
    private readonly IHostApplicationLifetime AppLifetime;
    private readonly ILogger Logger;
    private readonly MqttOption MqttOption;

    const string ServerClientId = "mainServer";



    public MqttServiceHost(MqttService mqttServer, ILogger<MqttServiceHost> logger, IHostApplicationLifetime appLifetime, IOptions<MqttOption> mqttOptions)
    {
        MqttServer = mqttServer;
        Logger = logger;
        AppLifetime = appLifetime;
        MqttOption = mqttOptions.Value;
    }

    public void Dispose()
    {
        MqttService.MqttServer.StopAsync();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {

        MqttServerOptionsBuilder optionsBuilder = new MqttServerOptionsBuilder();
        optionsBuilder.WithDefaultEndpoint();
        optionsBuilder.WithDefaultEndpointPort(MqttOption.Port);
        optionsBuilder.WithConnectionBacklog(MqttOption.MaxConnect);
        optionsBuilder.WithDefaultCommunicationTimeout(TimeSpan.FromMilliseconds(MqttOption.Timeout));

        MqttServerOptions options = optionsBuilder.Build();

        MqttService.MqttServer = new MqttFactory().CreateMqttServer(options);

        MqttService.MqttServer.ClientConnectedAsync += mqttServer_ClientConnectedAsync;
        MqttService.MqttServer.ClientDisconnectedAsync += mqttServerClientDisconnectedAsync;
        MqttService.MqttServer.ApplicationMessageNotConsumedAsync += mqttServer_ApplicationMessageNotConsumedAsync;

        MqttService.MqttServer.ClientSubscribedTopicAsync += mqttServer_ClientSubscribedTopicAsync;
        MqttService.MqttServer.ClientUnsubscribedTopicAsync += mqttServer_ClientUnsubscribedTopicAsync;
        MqttService.MqttServer.StartedAsync += mqttServer_StartedAsync;
        MqttService.MqttServer.StoppedAsync += mqttServer_StoppedAsync;
        MqttService.MqttServer.InterceptingPublishAsync += mqttServer_InterceptingPublishAsync;

        MqttService.MqttServer.StartAsync();

        AppLifetime.ApplicationStarted.Register(OnStarted);
        AppLifetime.ApplicationStopping.Register(OnStoping);
        AppLifetime.ApplicationStopped.Register(OnStopped);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }


    private void OnStarted()
    {
        Logger.LogInformation("OnStarted has been called.");
    }


    private void OnStoping()
    {
        Logger.LogInformation("OnStoping has been called.");
        MqttService.MqttServer.StopAsync();
    }

    private void OnStopped()
    {
        Logger.LogInformation("OnStopped has been called.");
    }

    /// <summary>
    /// on client subscribed topic event
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private Task mqttServer_ClientSubscribedTopicAsync(ClientSubscribedTopicEventArgs arg)
    {
        Logger.LogInformation("client id=[{}]", arg.ClientId);
        Logger.LogInformation("subscribed topic=[{}]", arg.TopicFilter);
        return Task.CompletedTask;
    }

    /// <summary>
    /// mqtt server close
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private Task mqttServer_StoppedAsync(EventArgs arg)
    {
        Logger.LogInformation($"mqtt server close......");
        return Task.CompletedTask;
    }

    /// <summary>
    /// InterceptingPublish event
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private Task mqttServer_InterceptingPublishAsync(InterceptingPublishEventArgs arg)
    {
        if (string.Equals(arg.ClientId, ServerClientId))
        {
            return Task.CompletedTask;
        }

        Logger.LogInformation("client id=[{}]", arg.ClientId);
        Logger.LogInformation("Topic=[{}]", arg.ApplicationMessage.Topic);
        Logger.LogInformation("message=[{}]", Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment));
        Logger.LogInformation("qos level=[{}]", arg.ApplicationMessage.QualityOfServiceLevel);
        return Task.CompletedTask;

    }

    /// <summary>
    /// mqtt broker start evnet
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private Task mqttServer_StartedAsync(EventArgs arg)
    {
        Logger.LogInformation($"mqtt broker started......");
        return Task.CompletedTask;
    }

    /// <summary>
    /// client unsubscribed event
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private Task mqttServer_ClientUnsubscribedTopicAsync(ClientUnsubscribedTopicEventArgs arg)
    {
        Logger.LogInformation("client id=[{}] unsubscribed", arg.ClientId);
        Logger.LogInformation("topic=[{}]", arg.TopicFilter);
        return Task.CompletedTask;
    }

    private Task mqttServer_ApplicationMessageNotConsumedAsync(ApplicationMessageNotConsumedEventArgs arg)
    {
        Logger.LogInformation("publisher id=[{}]", arg.SenderId);
        Logger.LogInformation("Topic=[{}]", arg.ApplicationMessage.Topic);
        Logger.LogInformation("message=[{}]", Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment));
        Logger.LogInformation("qos level=[{}]", arg.ApplicationMessage.QualityOfServiceLevel);
        return Task.CompletedTask;

    }

    /// <summary>
    /// client disconnected evnet
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private Task mqttServerClientDisconnectedAsync(ClientDisconnectedEventArgs arg)
    {
        Logger.LogInformation("client id=[{}] is disconnected", arg.ClientId);
        Logger.LogInformation("endpoint=[{}]", arg.Endpoint);
        return Task.CompletedTask;
    }

    /// <summary>
    /// ClientConnected
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private Task mqttServer_ClientConnectedAsync(ClientConnectedEventArgs arg)
    {
        Logger.LogInformation("client id=[{}] is connected", arg.ClientId);
        Logger.LogInformation("endpoint=[{}]", arg.Endpoint);
        return Task.CompletedTask;
    }
}