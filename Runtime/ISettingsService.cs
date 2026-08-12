namespace Services
{
    public interface ISettingsService<in T> : IService where T : ServiceSettings
    {
        T Settings { set; }
    }
}