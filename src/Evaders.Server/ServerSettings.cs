namespace Evaders.Server
{
    using System.Net;
    using Core.Game;

    public class ServerSettings
    {
        public bool IsValid => IP != null && MaxTimeInQueueSec > 0f && MaxQueueCount > 0 && MaxUsernameLength > 0 && GameModes != null && GameModes.Length > 0;

        public IPAddress IP { get; set; } = IPAddress.Parse("0.0.0.0");
        public int MaxQueueCount { get; set; } = 5;
        public float MaxTimeInQueueSec { get; set; } = 15f;
        public int MaxUsernameLength { get; set; } = 20;
        public string Motd { get; set; } = "Welcome :)";
        public ushort Port { get; set; } = 9090;
        public string[] GameModes { get; set; } = { "Default" };
    }
}