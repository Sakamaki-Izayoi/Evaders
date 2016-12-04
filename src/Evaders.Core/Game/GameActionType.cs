namespace Evaders.Core.Game
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum GameActionType
    {
        Move,
        Shoot
    }
}