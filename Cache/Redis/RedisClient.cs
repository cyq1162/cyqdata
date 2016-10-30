using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.Text;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// Memcached client main class.
    /// Use the static methods Setup and GetInstance to setup and get an instance of the client for use.
    /// </summary>
    internal class RedisClient
    {
        #region Static fields and methods.
        private static Dictionary<string, RedisClient> instances = new Dictionary<string, RedisClient>();
        private static LogAdapter logger = LogAdapter.GetLogger(typeof(RedisClient));

        /// <summary>
        /// Static method for creating an instance. This method will throw an exception if the name already exists.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <param name="servers">A list of memcached servers in standard notation: host:port. 
        /// If port is omitted, the default value of 11211 is used. 
        /// Both IP addresses and host names are accepted, for example:
        /// "localhost", "127.0.0.1", "cache01.example.com:12345", "127.0.0.1:12345", etc.</param>
        public static RedisClient Setup(string name, string[] servers)
        {
            if (instances.ContainsKey(name))
            {
                throw new ConfigurationErrorsException("Trying to configure RedisClient instance \"" + name + "\" twice.");
            }
            RedisClient client = new RedisClient(name, servers);
            instances[name] = client;
            return client;
        }

        /// <summary>
        /// Static method which checks if a given named RedisClient instance exists.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns></returns>
        public static bool Exists(string name)
        {
            return instances.ContainsKey(name);
        }

        /// <summary>
        /// Static method for getting the default instance named "default".
        /// </summary>
        private static RedisClient defaultInstance = null;
        public static RedisClient GetInstance()
        {
            return defaultInstance ?? (defaultInstance = GetInstance("default"));
        }

        /// <summary>
        /// Static method for getting an instance. 
        /// This method will first check for named instances that has been set up programmatically.
        /// If no such instance exists, it will check the "beitmemcached" section of the standard 
        /// config file and see if it can find configuration info for it there.
        /// If that also fails, an exception is thrown.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <returns>The named instance.</returns>
        public static RedisClient GetInstance(string name)
        {
            RedisClient c;
            if (instances.TryGetValue(name, out c))
            {
                return c;
            }
            Error.Throw("Unable to find RedisClient instance \"" + name + "\".");
            return null;
        }
        #endregion

        #region Fields, constructors, and private methods.
        public readonly string Name;
        internal readonly ServerPool serverPool;

        /// <summary>
        /// If you specify a key prefix, it will be appended to all keys before they are sent to the memcached server.
        /// They key prefix is not used when calculating which server a key belongs to.
        /// </summary>
        public string KeyPrefix { get { return keyPrefix; } set { keyPrefix = value; } }
        private string keyPrefix = "";

        /// <summary>
        /// The send receive timeout is used to determine how long the client should wait for data to be sent 
        /// and received from the server, specified in milliseconds. The default value is 2000.
        /// </summary>
        public int SendReceiveTimeout { get { return serverPool.SendReceiveTimeout; } set { serverPool.SendReceiveTimeout = value; } }

        /// <summary>
        /// The connect timeout is used to determine how long the client should wait for a connection to be established,
        /// specified in milliseconds. The default value is 2000.
        /// </summary>
        public int ConnectTimeout { get { return serverPool.ConnectTimeout; } set { serverPool.ConnectTimeout = value; } }

        /// <summary>
        /// The min pool size determines the number of sockets the socket pool will keep.
        /// Note that no sockets will be created on startup, only on use, so the socket pool will only
        /// contain this amount of sockets if the amount of simultaneous requests goes above it.
        /// The default value is 5.
        /// </summary>
        public uint MinPoolSize
        {
            get { return serverPool.MinPoolSize; }
            set
            {
                if (value > MaxPoolSize) { throw new ConfigurationErrorsException("MinPoolSize (" + value + ") may not be larger than the MaxPoolSize (" + MaxPoolSize + ")."); }
                serverPool.MinPoolSize = value;
            }
        }

        /// <summary>
        /// The max pool size determines how large the socket connection pool is allowed to grow.
        /// There can be more sockets in use than this amount, but when the extra sockets are returned, they will be destroyed.
        /// The default value is 10.
        /// </summary>
        public uint MaxPoolSize
        {
            get { return serverPool.MaxPoolSize; }
            set
            {
                if (value < MinPoolSize) { throw new ConfigurationErrorsException("MaxPoolSize (" + value + ") may not be smaller than the MinPoolSize (" + MinPoolSize + ")."); }
                serverPool.MaxPoolSize = value;
            }
        }

        /// <summary>
        /// If the pool contains more than the minimum amount of sockets, and a socket is returned that is older than this recycle age
        /// that socket will be destroyed instead of put back in the pool. This allows the pool to shrink back to the min pool size after a peak in usage.
        /// The default value is 30 minutes.
        /// </summary>
        public TimeSpan SocketRecycleAge { get { return serverPool.SocketRecycleAge; } set { serverPool.SocketRecycleAge = value; } }

        /// <summary>
        /// 指定数据长度超过值时进行压缩
        /// </summary>
        private uint compressionThreshold = 1024 * 128; //128kb
        /// <summary>
        /// If an object being stored is larger in bytes than the compression threshold, it will internally be compressed before begin stored,
        /// and it will transparently be decompressed when retrieved. Only strings, byte arrays and objects can be compressed.
        /// The default value is 1048576 bytes = 1MB.
        /// </summary>
        public uint CompressionThreshold { get { return compressionThreshold; } set { compressionThreshold = value; } }

        //Private constructor
        private RedisClient(string name, string[] hosts)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ConfigurationErrorsException("Name of RedisClient instance cannot be empty.");
            }
            if (hosts == null || hosts.Length == 0)
            {
                throw new ConfigurationErrorsException("Cannot configure RedisClient with empty list of hosts.");
            }

            Name = name;
            serverPool = new ServerPool(hosts);
        }

        /// <summary>
        /// Private key hashing method that uses the modified FNV hash.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <returns>The hashed key.</returns>
        private uint hash(string key)
        {
            checkKey(key);

            return BitConverter.ToUInt32(new ModifiedFNV1_32().ComputeHash(Encoding.UTF8.GetBytes(key)), 0);
        }

        /// <summary>
        /// Private hashing method for user-supplied hash values.
        /// </summary>
        /// <param name="hashvalue">The user-supplied hash value to hash.</param>
        /// <returns>The hashed value</returns>
        private uint hash(uint hashvalue)
        {
            return BitConverter.ToUInt32(new ModifiedFNV1_32().ComputeHash(BitConverter.GetBytes(hashvalue)), 0);
        }

        /// <summary>
        /// Private multi-hashing method.
        /// </summary>
        /// <param name="keys">An array of keys to hash.</param>
        /// <returns>An arrays of hashes.</returns>
        private uint[] hash(string[] keys)
        {
            uint[] result = new uint[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                result[i] = hash(keys[i]);
            }
            return result;
        }

        /// <summary>
        /// Private multi-hashing method for user-supplied hash values.
        /// </summary>
        /// <param name="hashvalues">An array of keys to hash.</param>
        /// <returns>An arrays of hashes.</returns>
        private uint[] hash(uint[] hashvalues)
        {
            uint[] result = new uint[hashvalues.Length];
            for (int i = 0; i < hashvalues.Length; i++)
            {
                result[i] = hash(hashvalues[i]);
            }
            return result;
        }

        /// <summary>
        /// Private key-checking method.
        /// Throws an exception if the key does not conform to memcached protocol requirements:
        /// It may not contain whitespace, it may not be null or empty, and it may not be longer than 250 characters.
        /// </summary>
        /// <param name="key">The key to check.</param>
        private void checkKey(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("Key may not be null.");
            }
            if (key.Length == 0)
            {
                throw new ArgumentException("Key may not be empty.");
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
        private static int getUnixTime(DateTime datetime)
        {
            return (int)(datetime.ToUniversalTime() - epoch).TotalSeconds;
        }
        #endregion

        #region Set、Append

        public void Append(string key, object value, int seconds) { Set("append", key, false, value, hash(key), seconds); }
        public void Set(string key, object value, int seconds) { Set("set", key, false, value, hash(key), seconds); }


        private bool Set(string command, string key, bool keyIsChecked, object value, uint hash, int expirySeconds)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }

            string result = serverPool.Execute<string>(hash, "", delegate(MSocket socket)
              {
                  SerializedType type;
                  byte[] bytes;
                  byte[] typeBit = new byte[1];
                  try
                  {

                      bytes = Serializer.Serialize(value, out type, CompressionThreshold);
                      typeBit[0] = (byte)type;
                  }
                  catch (Exception e)
                  {
                      logger.Error("Error serializing object for key '" + key + "'.", e);
                      return "";
                  }
                  CheckDB(socket, hash);
                  using (RedisCommand cmd = new RedisCommand(socket, 3, command))
                  {
                      cmd.WriteKey(keyPrefix + key);
                      cmd.WriteValue(typeBit, bytes);
                      result = socket.ReadResponse();
                      if (result[0] != '-')
                      {
                          if (expirySeconds > 0)
                          {
                              cmd.Reset(3, "EXPIRE");
                              cmd.WriteKey(keyPrefix + key);
                              cmd.WriteValue(expirySeconds.ToString());
                              result = socket.ReadResponse();
                          }
                      }
                      return result;
                  }

              });
            return !string.IsNullOrEmpty(result);
        }

        #endregion

        #region Get
        public object Get(string key) { return Get("get", key, true, hash(key)); }

        private object Get(string command, string key, bool keyIsChecked, uint hash)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }
            object value = serverPool.Execute<object>(hash, null, delegate(MSocket socket)
            {
                CheckDB(socket, hash);
                using (RedisCommand cmd = new RedisCommand(socket, 2, command))
                {
                    cmd.WriteKey(keyPrefix + key);
                }
                string result = socket.ReadResponse();
                if (!string.IsNullOrEmpty(result) && result[0] == '$')
                {
                    int len = 0;
                    if (int.TryParse(result.Substring(1), out len) && len > 0)
                    {
                        byte[] bytes = socket.ReadLineBytes();
                        if (bytes.Length > 0)
                        {
                            byte[] data = new byte[bytes.Length - 1];
                            SerializedType st = (SerializedType)bytes[0];
                            Array.Copy(bytes, 1, data, 0, data.Length);
                            bytes = null;
                            return Serializer.DeSerialize(data, st);
                        }
                    }
                }
                return null;
            });
            return value;
        }

        #endregion

        #region Exists
        public bool ContainsKey(string key) { return ContainsKey(key, true, hash(key)); }

        private bool ContainsKey(string key, bool keyIsChecked, uint hash)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }
            return serverPool.Execute<bool>(hash, false, delegate(MSocket socket)
            {
                CheckDB(socket, hash);
                using (RedisCommand cmd = new RedisCommand(socket, 2, "Exists"))
                {
                    cmd.WriteKey(keyPrefix + key);
                }
                return !socket.ReadResponse().StartsWith("-");
            });
        }

        #endregion

        #region Select DB
        internal void CheckDB(MSocket socket, uint hash)
        {
            if (AppConfig.Cache.RedisUseDBCount > 1)
            {
                uint db = hash % (uint)AppConfig.Cache.RedisUseDBCount;//默认分散在16个DB中。
                if (socket.DB != db)
                {
                    socket.DB = db;
                    using (RedisCommand cmd = new RedisCommand(socket, 2, "Select"))
                    {
                        cmd.WriteKey(db.ToString());
                    }
                    socket.SkipToEndOfLine();
                }
            }
        }
        //public bool SelectDB(SocketPool pool, int num)
        //{
        //    return serverPool.Execute<bool>(pool, false, delegate(MSocket socket)
        //              {
        //                  using (RedisCommand cmd = new RedisCommand(socket, 2, "Select"))
        //                  {
        //                      cmd.WriteKey(num.ToString());
        //                  }
        //                  return !socket.ReadResponse().StartsWith("-");
        //              });
        //}

        #endregion

        #region Delete


        public bool Delete(string key) { return Delete(key, true, hash(key), 0); }

        private bool Delete(string key, bool keyIsChecked, uint hash, int time)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }

            return serverPool.Execute<bool>(hash, false, delegate(MSocket socket)
            {
                CheckDB(socket, hash);
                using (RedisCommand cmd = new RedisCommand(socket, 2, "DEL"))
                {
                    cmd.WriteKey(keyPrefix + key);
                }
                return socket.ReadResponse().StartsWith(":1");
            });
        }
        #endregion


        #region Flush All
        /// <summary>
        /// This method corresponds to the "flush_all" command in the memcached protocol.
        /// When this method is called, it will send the flush command to all servers, thereby deleting
        /// all items on all servers.
        /// Use the overloads to set a delay for the flushing. If the parameter staggered is set to true,
        /// the client will increase the delay for each server, i.e. the first will flush after delay*0, 
        /// the second after delay*1, the third after delay*2, etc. If set to false, all servers will flush 
        /// after the same delay.
        /// It returns true if the command was successful on all servers.
        /// </summary>
        public bool FlushAll()
        {
            foreach (SocketPool pool in serverPool.HostList)
            {
                serverPool.Execute(pool, delegate(MSocket socket)
                {
                    using (RedisCommand cmd = new RedisCommand(socket, 1, "flushall"))
                    {

                    }

                });
            }
            return true;
        }

        #endregion

        #region Stats
        /// <summary>
        /// This method corresponds to the "stats" command in the memcached protocol.
        /// It will send the stats command to all servers, and it will return a Dictionary for each server
        /// containing the results of the command.
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> Stats()
        {
            Dictionary<string, Dictionary<string, string>> results = new Dictionary<string, Dictionary<string, string>>();
            foreach (SocketPool pool in serverPool.HostList)
            {
                results.Add(pool.Host, stats(pool));
            }
            return results;
        }

        private Dictionary<string, string> stats(SocketPool pool)
        {
            if (pool == null)
            {
                return null;
            }
            Dictionary<string, string> dic = new Dictionary<string, string>();
            serverPool.Execute(pool, delegate(MSocket socket)
            {
                using (RedisCommand cmd = new RedisCommand(socket, 1, "info"))
                {

                }
                string result = socket.ReadResponse();
                if (!string.IsNullOrEmpty(result) && result[0] == '$')
                {
                    string line = null;
                    while (true)
                    {
                        line = socket.ReadLine();
                        if (line == null)
                        {
                            break;
                        }
                        string[] s = line.Split(':');
                        dic.Add(s[0], s[1]);
                    }


                }
            });
            return dic;
        }

        #endregion

        #region Status
        internal int okServer = 0, errorServer = 0;
        /// <summary>
        /// This method retrives the status from the serverpool. It checks the connection to all servers
        /// and returns usage statistics for each server.
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> Status()
        {
            okServer = 0;
            errorServer = 0;
            Dictionary<string, Dictionary<string, string>> results = new Dictionary<string, Dictionary<string, string>>();
            foreach (SocketPool pool in serverPool.HostList)
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                if (serverPool.Execute<bool>(pool, false, delegate { return true; }))
                {
                    okServer++;
                    result.Add("Status", "Ok");
                }
                else
                {
                    errorServer++;
                    result.Add("Status", "Dead, next retry at: " + pool.DeadEndPointRetryTime);
                }
                result.Add("Sockets in pool", pool.Poolsize.ToString());
                result.Add("Acquired sockets", pool.Acquired.ToString());
                result.Add("Sockets reused", pool.ReusedSockets.ToString());
                result.Add("New sockets created", pool.NewSockets.ToString());
                result.Add("New sockets failed", pool.FailedNewSockets.ToString());
                result.Add("Sockets died in pool", pool.DeadSocketsInPool.ToString());
                result.Add("Sockets died on return", pool.DeadSocketsOnReturn.ToString());
                result.Add("Dirty sockets on return", pool.DirtySocketsOnReturn.ToString());
                results.Add(pool.Host, result);
            }
            return results;
        }
        #endregion
    }
}