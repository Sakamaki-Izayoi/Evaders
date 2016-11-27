namespace Evaders.Server
{
    using System.Net;
    using Core.Game;

    public class ServerConfiguration
    {
        public static ServerConfiguration Default => new ServerConfiguration(new GameSettings(1000f, 30, 0.5f, 10f, new CharacterData(100, 10, 20, 0.75f, 65, 250f, 100f), 5f, 50f, 1), "0.0.0.0", 5, 15f, 20, 9090, "Welcome :)");

        public bool IsValid
        {
            get
            {
                IPAddress result;
                if (!IPAddress.TryParse(IP, out result))
                    return false;
                return GameSettings.IsValid && MaxTimeInQueueSec > 0f && MaxQueueCount > 0 && MaxUsernameLength > 0;
            }
        }

        public readonly GameSettings GameSettings;
        public readonly string IP;
        public readonly int MaxQueueCount;
        public readonly float MaxTimeInQueueSec;
        public readonly int MaxUsernameLength;
        public readonly string Motd;
        public readonly ushort Port;

        public ServerConfiguration(GameSettings gameSettings, string ip, int maxQueueCount, float maxTimeInQueueSec, int maxUsernameLength, ushort port, string motd)
        {
            GameSettings = gameSettings;
            IP = ip;
            MaxQueueCount = maxQueueCount;
            MaxTimeInQueueSec = maxTimeInQueueSec;
            MaxUsernameLength = maxUsernameLength;
            Port = port;
            Motd = motd;
        }
    }
}