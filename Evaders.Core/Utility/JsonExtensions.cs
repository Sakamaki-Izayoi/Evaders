namespace Evaders.Core.Utility
{
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;

    public static class JsonExtensions
    {
        public static T DeserializeEx<T>(this JsonSerializer serializer, string json)
        {
            using (var reader = new JsonTextReader(new StringReader(json)))
                return (T) serializer.Deserialize(reader, typeof (T));
        }

        public static string SerializeEx<T>(this JsonSerializer serializer, T obj)
        {
            var sb = new StringBuilder(256);
            var sw = new StringWriter(sb, CultureInfo.InvariantCulture);
            using (var jsonWriter = new JsonTextWriter(sw))
            {
                jsonWriter.Formatting = serializer.Formatting;

                serializer.Serialize(jsonWriter, obj, typeof (T));
            }

            return sw.ToString();
        }
    }
}