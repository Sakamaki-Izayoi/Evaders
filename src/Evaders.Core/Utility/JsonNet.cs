namespace Evaders.Core.Utility
{
    using System.IO;
    using Newtonsoft.Json;

    public static class JsonNet
    {
        private static readonly JsonSerializer Serializer;

        static JsonNet()
        {
            Serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings {ContractResolver = new GameContractResolver()});
        }

        public static string Serialize<T>(T obj)
        {
            return Serializer.SerializeEx(obj);
        }

        public static T Deserialize<T>(string json)
        {
            return Serializer.DeserializeEx<T>(json);
        }

        public static byte[] SerializeBson<T>(T obj)
        {
            return Serializer.SerializeBsonEx(obj);
        }

        public static T DeserializeBson<T>(MemoryStream data)
        {
            return Serializer.DeserializeBsonEx<T>(data);
        }
    }
}