using System;

namespace Hangfire.AspNetCore.Plus.Sample
{
    public class TestJob
    {
        [TestFilter]
        public void Test()
        {
            throw new InvalidOperationException();
        }
    }
}