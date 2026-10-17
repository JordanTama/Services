using Services;

namespace JordanTama.Services.Tests
{
    [DependsOnService(typeof(BehaviourService))]
    public class DependentBehaviourService : MonoBehaviourService { }
}
