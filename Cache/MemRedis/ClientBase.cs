using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Tool;

namespace CYQ.Data.Cache
{
    internal abstract class ClientBase
    {
        /// <summary>
        /// 主机服务
        /// </summary>
        internal HostServer hostServer;

        /// <summary>
        /// 指定数据长度超过值时进行压缩，默认128K
        /// </summary>
        protected uint compressionThreshold = 1024 * 128; //128kb


        /// <summary>
        /// Private key hashing method that uses the modified FNV hash.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <returns>The hashed key.</returns>
        protected uint hash(string key)
        {
            checkKey(key);
            return HashCreator.Create(key);
        }

        /// <summary>
        /// Private key-checking method.
        /// Throws an exception if the key does not conform to memcached protocol requirements:
        /// It may not contain whitespace, it may not be null or empty, and it may not be longer than 250 characters.
        /// </summary>
        /// <param name="key">The key to check.</param>
        protected void checkKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("Key may not be empty.");
            }
            if (key.Length > 250)
            {
                throw new ArgumentException("Key may not be longer than 250 characters.");
            }
            foreach (char c in key)
            {
                if (c <= 32)
                {
                    throw new ArgumentException("Key may not contain whitespace or control characters.");
                }
            }
        }

        //Private Unix-time converter
        private static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        protected static int getUnixTime(DateTime datetime)
        {
            return (int)(datetime.ToUniversalTime() - epoch).TotalSeconds;
        }

        #region Status
        DateTime allowGetWorkInfoTime = DateTime.Now;
        string workInfo = string.Empty;
        public string WorkInfo
        {
            get
            {
                if (workInfo == string.Empty || DateTime.Now > allowGetWorkInfoTime)
                {
                    Dictionary<string, Dictionary<string, string>> status = GetAllHostNodeStatus();
                    if (status != null)
                    {
                        JsonHelper js = new JsonHelper(false, false);
                        js.Add("OKServerCount", OkServer.ToString());
                        js.Add("DeadServerCount",ErrorServer.ToString());
                        foreach (KeyValuePair<string, Dictionary<string, string>> item in status)
                        {
                            js.Add(item.Key, JsonHelper.ToJson(item.Value));
                        }
                        js.AddBr();
                        workInfo = js.ToString();
                    }
                    allowGetWorkInfoTime = DateTime.Now.AddSeconds(5);
                }
                return workInfo;
            }
        }
        public int OkServer = 0, ErrorServer = 0;
        /// <summary>
        /// 获取主机节点的基础信息
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> GetAllHostNodeStatus()
        {
            OkServer = 0;
            ErrorServer = 0;
            Dictionary<string, Dictionary<string, string>> results = new Dictionary<string, Dictionary<string, string>>();
            foreach (KeyValuePair<string, HostNode> item in hostServer.HostList)
            {
                Dictionary<string, string> result = GetInfoByHost(item.Value, false);
                results.Add(item.Key, result);
            }
            return results;
        }
        private Dictionary<string, string> GetInfoByHost(HostNode host, bool isBackup)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (!host.IsEndPointDead)
            {
                if(!isBackup) OkServer++;
                result.Add("Status", "Ok");
                result.Add("Sockets in pool", host.Poolsize.ToString());
                result.Add("Acquired sockets", host.Acquired.ToString());
                result.Add("Sockets reused", host.ReusedSockets.ToString());
                result.Add("New sockets created", host.NewSockets.ToString());
                result.Add("New sockets failed", host.FailedNewSockets.ToString());
                result.Add("Sockets died in pool", host.DeadSocketsInPool.ToString());
                result.Add("Sockets died on return", host.DeadSocketsOnReturn.ToString());
            }
            else
            {
                if (!isBackup) ErrorServer++;
                result.Add("Status", "Dead , next retry at : " + host.DeadEndPointRetryTime);
                result.Add("Error", host.Error);
                if (host.HostNodeBak != null)
                {
                    result.Add("Backup - " + host.HostNodeBak.Host, JsonHelper.ToJson(GetInfoByHost(host.HostNodeBak, true)));
                }
            }

            return result;
        }
        #endregion
    }
}
