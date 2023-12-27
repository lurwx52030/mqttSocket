namespace mqttSocket.Models;

/// <summary>
    /// mqtt server option
    /// </summary>
    public class MqttOption
    {
        public string Host { get; set; } = "";
        public int  Port { get; set; }= 61613;
        public int Timeout { get; set; } = 5000;
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public int MaxConnect { get; set; } = 100;
    }