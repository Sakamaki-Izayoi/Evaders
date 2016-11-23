namespace Evaders.Server
{
    using System.IO;
    using System.Threading.Tasks;

    public static class AsyncSaving
    {
        public static void SaveObject(string path, object obj)
        {
            SaveString(path, JsonNet.Serialize(obj));
        }

        public static void SaveString(string path, string str)
        {
            new Task(() => { File.WriteAllText(path, str); }).Start();
        }
    }
}