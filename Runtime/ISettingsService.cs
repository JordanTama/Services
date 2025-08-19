namespace Services
{
    public interface ISettingsService : IServiceAsync
    {
    }
    
    public interface ISettingsService<in T> : ISettingsService where T : ServiceSettings
    {
        T Settings { set; }
    }
}