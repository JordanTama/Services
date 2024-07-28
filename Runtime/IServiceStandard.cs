namespace Services
{
    public interface IServiceStandard : IService
    {
        void OnRegistered();
        void OnUnregistered();
    }
}