using CYQ.Data.Cache;

namespace CYQ.Data.Lock
{
    internal class LocalLock : DistributedLock
    {
        private static readonly LocalLock _instance = new LocalLock();
        private LocalLock() { }
        public static LocalLock Instance
        {
            get
            {
                return _instance;
            }
        }
        public override LockType LockType
        {
            get
            {
                return LockType.Local;
            }
        }

        public override bool Lock(string key, int millisecondsTimeout)
        {
            return DistributedCache.Local.Lock(key, millisecondsTimeout);
        }

        public override void UnLock(string key)
        {
            DistributedCache.Local.UnLock(key);
        }

        public override bool Idempotent(string key)
        {
            return Idempotent(key, 0);
        }

        public override bool Idempotent(string key, double keepMinutes)
        {
            return DistributedLock.File.Idempotent(key);
        }
    }
}
