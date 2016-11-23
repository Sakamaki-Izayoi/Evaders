namespace Evaders.CommonNetworking.CommonPayloads
{
    using Newtonsoft.Json;

    public class AuthState
    {
        [JsonProperty] public readonly bool Authorized;

        [JsonProperty] public readonly string Message;

        [JsonConstructor]
        public AuthState(bool authorized, string message)
        {
            Authorized = authorized;
            Message = message;
        }
    }
}