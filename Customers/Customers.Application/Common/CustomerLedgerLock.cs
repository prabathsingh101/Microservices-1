using System.Threading;

namespace Customers.Application.Common
{
    public static class CustomerLedgerLock
    {
        public static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
    }
}