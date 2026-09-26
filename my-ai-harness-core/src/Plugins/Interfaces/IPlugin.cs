using System.Threading;
using Ablinger.MyAiHarness.Core.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Ablinger.MyAiHarness.Core.Plugins.Interfaces;

public interface IPlugin
{
    IAIBackendProvider? GetAiBackendProvider();

    public abstract class MemoizedPlugin : IPlugin
    {
        private IAIBackendProvider? aiBackendProvider;
        private readonly Lock aiBackendProviderLock = new();
        public IAIBackendProvider? GetAiBackendProvider()
        {
            if (aiBackendProvider != null) return aiBackendProvider;
            
            lock (aiBackendProviderLock)
            {
                aiBackendProvider ??= CalculateAiBackendProvider();
            }

            return aiBackendProvider;
        }

        protected abstract IAIBackendProvider? CalculateAiBackendProvider();
    }
    
    public class RefectionAutoPlugin(ServiceProvider serviceProvider) : MemoizedPlugin
    {
        protected ServiceProvider GetServiceProvider()
        {
            return serviceProvider;
        }
        
        protected override IAIBackendProvider? CalculateAiBackendProvider()
        {
            return ReflectionUtils.FindImplementation<IAIBackendProvider>(serviceProvider);
        }
    }
}