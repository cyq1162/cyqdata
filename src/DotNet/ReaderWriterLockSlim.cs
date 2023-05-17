using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;

namespace System.Threading
{
    /// <summary>
    /// 仅 2.0 或 3.0 下编绎使用，其它环境编绎可以移除。
    /// </summary>
    internal class ReaderWriterLockSlim
    {
        object readWriteLockSlim = null;
        MethodInfo enterReadLockMethod;
        MethodInfo exitReadLockMethod;

        MethodInfo enterWriteLockMethod;
        MethodInfo exitWriteLockMethod;

        private void Init(Assembly ass)
        {
            if (ass != null)
            {
                // 获取 ReaderWriterLockSlim 类型
                Type readWriteLockSlimType = ass.GetType("System.Threading.ReaderWriterLockSlim");
                if (readWriteLockSlimType != null)
                {
                    // 创建 ReadWriteLockSlim 对象
                    readWriteLockSlim = Activator.CreateInstance(readWriteLockSlimType);

                    // 调用方法
                    enterReadLockMethod = readWriteLockSlimType.GetMethod("EnterReadLock");
                    enterWriteLockMethod = readWriteLockSlimType.GetMethod("EnterWriteLock");

                    enterReadLockMethod.Invoke(readWriteLockSlim, null);

                    // 阻塞以等待读取锁释放
                    // ...

                    // 释放读取锁
                    exitReadLockMethod = readWriteLockSlimType.GetMethod("ExitReadLock");
                    exitWriteLockMethod = readWriteLockSlimType.GetMethod("ExitWriteLock");
                    exitReadLockMethod.Invoke(readWriteLockSlim, null);
                }
            }
        }
        public ReaderWriterLockSlim()
        {

            Assembly[] asList = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly ass in asList)
            {
                if (ass.FullName.StartsWith("System.Core"))
                {
                    Version ver = ass.GetName().Version;
                    if (ver.Major > 3)// || (ver.Major == 3 && ver.Minor == 5) 3.5版本性能好像也不咋的。
                    {
                        Init(ass);
                    }
                    return;
                }
            }






        }
        private object obj = new object();
        //ReaderWriterLock _lock = new ReaderWriterLock();//有 bug ，并发不稳定
        internal void TryEnterWriteLock(int p)
        {
            if (enterWriteLockMethod != null)
            {
                enterWriteLockMethod.Invoke(readWriteLockSlim, null);
                return;
            }
            Monitor.Enter(obj);
            //_lock.AcquireWriterLock(p);
        }
        internal void ExitWriteLock()
        {
            if (exitWriteLockMethod != null)
            {
                exitWriteLockMethod.Invoke(readWriteLockSlim, null);
                return;
            }
            Monitor.Exit(obj);
            // _lock.ReleaseWriterLock();
        }

        internal void TryEnterReadLock(int p)
        {
            if (enterReadLockMethod != null)
            {
                enterReadLockMethod.Invoke(readWriteLockSlim, null);
                return;
            }
            Monitor.Enter(obj);
            //_lock.AcquireReaderLock(p);
        }

        internal void ExitReadLock()
        {
            if (exitReadLockMethod != null)
            {
                exitReadLockMethod.Invoke(readWriteLockSlim, null);
                return;
            }
            Monitor.Exit(obj);
            //_lock.ReleaseReaderLock();
        }
    }
}
