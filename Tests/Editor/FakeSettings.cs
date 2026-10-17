using Services;

namespace JordanTama.Services.Editor.Tests
{
    [ServiceSettings(Key, Folder)]
    public class FakeSettings : ServiceSettings
    {
        public const string Key = "ServicesTestsFakeSettings";
        public const string Folder = "Assets/ServicesTests";
        public const string AssetPath = Folder + "/" + Key + ".asset";
    }
}
