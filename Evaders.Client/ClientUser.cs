namespace Evaders.Client
{
    using Core.Game;
    using Newtonsoft.Json;

    public class ClientUser : IUser
    {
        [JsonProperty]
        public bool Connected { get; }
        [JsonProperty]
        public long Identifier { get; }
        [JsonProperty]
        public string Username { get; }

        [JsonConstructor]
        public ClientUser(bool connected, long identifier, string username)
        {
            Connected = connected;
            Identifier = identifier;
            Username = username;
        }
    }
}