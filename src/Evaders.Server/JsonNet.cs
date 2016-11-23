namespace Evaders.Server
{
    using Core.Utility;
    using Newtonsoft.Json;

    internal static class JsonNet
    {
        private static readonly JsonSerializer Serializer;

        static JsonNet()
        {
            Serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings {ContractResolver = new GameContractResolver()});
        }

        internal static string Serialize<T>(T obj)
        {
            return Serializer.SerializeEx(obj);
        }

        internal static T Deserialize<T>(string json)
        {
            return Serializer.DeserializeEx<T>(json);
        }
    }
}