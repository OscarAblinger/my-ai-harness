using System.Threading.Tasks;

namespace Ablinger.MyAiHarness.Core.Harness.Prompting;

public interface IPrompter
{
    Task Register(Harness harness);
}