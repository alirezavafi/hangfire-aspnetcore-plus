using System;
using Hangfire.Common;

namespace Hangfire
{
    public class JobFilterWithServiceProviderAttribute : JobFilterAttribute
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider ServiceProvider
        {
            get
            {
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("hangfire not initialized. please use app.UseHangfirePlus()");
                }
                
                return _serviceProvider;
            }
            set => _serviceProvider = value;
        }
    }
}