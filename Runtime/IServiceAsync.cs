using Cysharp.Threading.Tasks;

namespace Services
{
    public interface IServiceAsync : IService
    {
        UniTask OnRegistered();
        UniTask OnUnregistered();
    }
}