using Cysharp.Threading.Tasks;

namespace Services
{
    public interface IService
    {
        UniTask OnRegistered();
        UniTask OnUnregistered();
    }
}