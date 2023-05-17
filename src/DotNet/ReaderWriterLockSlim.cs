using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace System.Threading
{
    /// <summary>
    /// 仅 2.0 或 3.0 下使用，其它可以移除。
    /// </summary>
    internal class ReaderWriterLockSlim
    {
        private object obj = new object();
        //ReaderWriterLock _lock = new ReaderWriterLock();//有 bug ，并发不稳定
        internal void TryEnterWriteLock(int p)
        {
            Monitor.Enter(obj);
            //_lock.AcquireWriterLock(p);
        }

        internal void ExitWriteLock()
        {
            Monitor.Exit(obj);
           // _lock.ReleaseWriterLock();
        }

        internal void TryEnterReadLock(int p)
        {
            Monitor.Enter(obj);
            //_lock.AcquireReaderLock(p);
        }

        internal void ExitReadLock()
        {
            Monitor.Exit(obj);
            //_lock.ReleaseReaderLock();
        }
    }
}
