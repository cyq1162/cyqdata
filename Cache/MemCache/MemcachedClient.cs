//Copyright (c) 2007-2008 Henrik Schröder, Oliver Kofoed Pedersen

//Permission is hereby granted, free of charge, to any person
//obtaining a copy of this software and associated documentation
//files (the "Software"), to deal in the Software without
//restriction, including without limitation the rights to use,
//copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the
//Software is furnished to do so, subject to the following
//conditions:

//The above copyright notice and this permission notice shall be
//included in all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
//OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
//WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
//FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
//OTHER DEALINGS IN THE SOFTWARE.

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
    internal class MemcachedClient
    {
        #region Static fields and methods.
        private static Dictionary<string, MemcachedClient> instances = new Dictionary<string, MemcachedClient>();
        private static LogAdapter logger = LogAdapter.GetLogger(typeof(MemcachedClient));

        /// <summary>
        /// Static method for creating an instance. This method will throw an exception if the name already exists.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <param name="servers">A list of memcached servers in standard notation: host:port. 
        /// If port is omitted, the default value of 11211 is used. 
        /// Both IP addresses and host names are accepted, for example:
        /// "localhost", "127.0.0.1", "cache01.example.com:12345", "127.0.0.1:12345", etc.</param>
        public static MemcachedClient Setup(string name, string[] servers)
        {
            if (instances.ContainsKey(name))
            {
                throw new ConfigurationErrorsException("Trying to configure MemcachedClient instance \"" + name + "\" twice.");
            }
            MemcachedClient client = new MemcachedClient(name, servers);
            instances[name] = client;
            return client;
        }

        /// <summary>
        /// Static method which checks if a given named MemcachedClient instance exists.
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
        private static MemcachedClient defaultInstance = null;
        public static MemcachedClient GetInstance()
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
        public static MemcachedClient GetInstance(string name)
        {
            MemcachedClient c;
            if (instances.TryGetValue(name, out c))
            {
                return c;
            }
            Error.Throw("Unable to find MemcachedClient instance \"" + name + "\".");
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

        private uint compressionThreshold = 1024 * 128; //128kb
        /// <summary>
        /// If an object being stored is larger in bytes than the compression threshold, it will internally be compressed before begin stored,
        /// and it will transparently be decompressed when retrieved. Only strings, byte arrays and objects can be compressed.
        /// The default value is 1048576 bytes = 1MB.
        /// </summary>
        public uint CompressionThreshold { get { return compressionThreshold; } set { compressionThreshold = value; } }

        //Private constructor
        private MemcachedClient(string name, string[] hosts)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ConfigurationErrorsException("Name of MemcachedClient instance cannot be empty.");
            }
            if (hosts == null || hosts.Length == 0)
            {
                throw new ConfigurationErrorsException("Cannot configure MemcachedClient with empty list of hosts.");
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

        #region Set, Add, and Replace.
        /// <summary>
        /// This method corresponds to the "set" command in the memcached protocol. 
        /// It will unconditionally set the given key to the given value.
        /// Using the overloads it is possible to specify an expiry time, either relative as a TimeSpan or 
        /// absolute as a DateTime. It is also possible to specify a custom hash to override server selection.
        /// This method returns true if the value was successfully set.
        /// </summary>
        public bool Set(string key, object value) { return store("set", key, true, value, hash(key), 0); }
        public bool Set(string key, object value, uint hash) { return store("set", key, false, value, this.hash(hash), 0); }
        public bool Set(string key, object value, TimeSpan expiry) { return store("set", key, true, value, hash(key), (int)expiry.TotalSeconds); }
        public bool Set(string key, object value, uint hash, TimeSpan expiry) { return store("set", key, false, value, this.hash(hash), (int)expiry.TotalSeconds); }
        public bool Set(string key, object value, DateTime expiry) { return store("set", key, true, value, hash(key), getUnixTime(expiry)); }
        public bool Set(string key, object value, uint hash, DateTime expiry) { return store("set", key, false, value, this.hash(hash), getUnixTime(expiry)); }

        /// <summary>
        /// This method corresponds to the "add" command in the memcached protocol. 
        /// It will set the given key to the given value only if the key does not already exist.
        /// Using the overloads it is possible to specify an expiry time, either relative as a TimeSpan or 
        /// absolute as a DateTime. It is also possible to specify a custom hash to override server selection.
        /// This method returns true if the value was successfully added.
        /// </summary>
        public bool Add(string key, object value) { return store("add", key, true, value, hash(key), 0); }
        public bool Add(string key, object value, uint hash) { return store("add", key, false, value, this.hash(hash), 0); }
        public bool Add(string key, object value, TimeSpan expiry) { return store("add", key, true, value, hash(key), (int)expiry.TotalSeconds); }
        public bool Add(string key, object value, uint hash, TimeSpan expiry) { return store("add", key, false, value, this.hash(hash), (int)expiry.TotalSeconds); }
        public bool Add(string key, object value, DateTime expiry) { return store("add", key, true, value, hash(key), getUnixTime(expiry)); }
        public bool Add(string key, object value, uint hash, DateTime expiry) { return store("add", key, false, value, this.hash(hash), getUnixTime(expiry)); }

        /// <summary>
        /// This method corresponds to the "replace" command in the memcached protocol. 
        /// It will set the given key to the given value only if the key already exists.
        /// Using the overloads it is possible to specify an expiry time, either relative as a TimeSpan or 
        /// absolute as a DateTime. It is also possible to specify a custom hash to override server selection.
        /// This method returns true if the value was successfully replaced.
        /// </summary>
        public bool Replace(string key, object value) { return store("replace", key, true, value, hash(key), 0); }
        public bool Replace(string key, object value, uint hash) { return store("replace", key, false, value, this.hash(hash), 0); }
        public bool Replace(string key, object value, TimeSpan expiry) { return store("replace", key, true, value, hash(key), (int)expiry.TotalSeconds); }
        public bool Replace(string key, object value, uint hash, TimeSpan expiry) { return store("replace", key, false, value, this.hash(hash), (int)expiry.TotalSeconds); }
        public bool Replace(string key, object value, DateTime expiry) { return store("replace", key, true, value, hash(key), getUnixTime(expiry)); }
        public bool Replace(string key, object value, uint hash, DateTime expiry) { return store("replace", key, false, value, this.hash(hash), getUnixTime(expiry)); }



        public enum CasResult
        {
            Stored = 0,
            NotStored = 1,
            Exists = 2,
            NotFound = 3
        }

        //Private overload for the Set, Add and Replace commands.
        private bool store(string command, string key, bool keyIsChecked, object value, uint hash, int expiry)
        {
            return store(command, key, keyIsChecked, value, hash, expiry, 0).StartsWith("STORED");
        }

        //Private overload for the Append and Prepend commands.
        private bool store(string command, string key, bool keyIsChecked, object value, uint hash)
        {
            return store(command, key, keyIsChecked, value, hash, 0, 0).StartsWith("STORED");
        }

        //Private overload for the Cas command.
        private CasResult store(string key, bool keyIsChecked, object value, uint hash, int expiry, ulong unique)
        {
            string result = store("cas", key, keyIsChecked, value, hash, expiry, unique);
            if (result.StartsWith("STORED"))
            {
                return CasResult.Stored;
            }
            else if (result.StartsWith("EXISTS"))
            {
                return CasResult.Exists;
            }
            else if (result.StartsWith("NOT_FOUND"))
            {
                return CasResult.NotFound;
            }
            return CasResult.NotStored;
        }

        //Private common store method.
        private string store(string command, string key, bool keyIsChecked, object value, uint hash, int expiry, ulong unique)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }

            return serverPool.Execute<string>(hash, "", delegate(MSocket socket)
            {
                SerializedType type;
                byte[] bytes;

                //Serialize object efficiently, store the datatype marker in the flags property.
                try
                {
                    bytes = Serializer.Serialize(value, out type, CompressionThreshold);
                }
                catch (Exception e)
                {
                    //If serialization fails, return false;

                    logger.Error("Error serializing object for key '" + key + "'.", e);
                    return "";
                }

                //Create commandline
                string commandline = "";
                switch (command)
                {
                    case "set":
                    case "add":
                    case "replace":
                        commandline = command + " " + keyPrefix + key + " " + (ushort)type + " " + expiry + " " + bytes.Length + "\r\n";
                        break;
                    case "append":
                    case "prepend":
                        commandline = command + " " + keyPrefix + key + " 0 0 " + bytes.Length + "\r\n";
                        break;
                    case "cas":
                        commandline = command + " " + keyPrefix + key + " " + (ushort)type + " " + expiry + " " + bytes.Length + " " + unique + "\r\n";
                        break;
                }

                //Write commandline and serialized object.
                socket.Write(commandline);
                socket.Write(bytes);
                socket.Write("\r\n");
                return socket.ReadResponse();
            });
        }

        #endregion

        #region Get
        /// <summary>
        /// This method corresponds to the "get" command in the memcached protocol.
        /// It will return the value for the given key. It will return null if the key did not exist,
        /// or if it was unable to retrieve the value.
        /// If given an array of keys, it will return a same-sized array of objects with the corresponding
        /// values.
        /// Use the overload to specify a custom hash to override server selection.
        /// </summary>
        public object Get(string key) { ulong i; return get("get", key, true, hash(key), out i); }
        public object Get(string key, uint hash) { ulong i; return get("get", key, false, this.hash(hash), out i); }

        private object get(string command, string key, bool keyIsChecked, uint hash, out ulong unique)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }

            ulong __unique = 0;
            object value = serverPool.Execute<object>(hash, null, delegate(MSocket socket)
            {
                socket.Write(command + " " + keyPrefix + key + "\r\n");
                object _value;
                ulong _unique;
                if (readValue(socket, out _value, out key, out _unique))
                {
                    socket.ReadLine(); //Read the trailing END.
                }
                __unique = _unique;
                return _value;
            });
            unique = __unique;
            return value;
        }

        private object[] get(string command, string[] keys, bool keysAreChecked, uint[] hashes, out ulong[] uniques)
        {
            //Check arguments.
            if (keys == null || hashes == null)
            {
                throw new ArgumentException("Keys and hashes arrays must not be null.");
            }
            if (keys.Length != hashes.Length)
            {
                throw new ArgumentException("Keys and hashes arrays must be of the same length.");
            }
            uniques = new ulong[keys.Length];

            //Avoid going through the server grouping if there's only one key.
            if (keys.Length == 1)
            {
                return new object[] { get(command, keys[0], keysAreChecked, hashes[0], out uniques[0]) };
            }

            //Check keys.
            if (!keysAreChecked)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    checkKey(keys[i]);
                }
            }

            //Group the keys/hashes by server(pool)
            Dictionary<SocketPool, Dictionary<string, List<int>>> dict = new Dictionary<SocketPool, Dictionary<string, List<int>>>();
            for (int i = 0; i < keys.Length; i++)
            {
                Dictionary<string, List<int>> getsForServer;
                SocketPool pool = serverPool.GetSocketPool(hashes[i]);
                if (!dict.TryGetValue(pool, out getsForServer))
                {
                    dict[pool] = getsForServer = new Dictionary<string, List<int>>();
                }

                List<int> positions;
                if (!getsForServer.TryGetValue(keys[i], out positions))
                {
                    getsForServer[keyPrefix + keys[i]] = positions = new List<int>();
                }
                positions.Add(i);
            }

            //Get the values
            object[] returnValues = new object[keys.Length];
            ulong[] _uniques = new ulong[keys.Length];
            foreach (KeyValuePair<SocketPool, Dictionary<string, List<int>>> kv in dict)
            {
                serverPool.Execute(kv.Key, delegate(MSocket socket)
                {
                    //Build the get request
                    StringBuilder getRequest = new StringBuilder(command);
                    foreach (KeyValuePair<string, List<int>> key in kv.Value)
                    {
                        getRequest.Append(" ");
                        getRequest.Append(key.Key);
                    }
                    getRequest.Append("\r\n");

                    //Send get request
                    socket.Write(getRequest.ToString());

                    //Read values, one by one
                    object gottenObject;
                    string gottenKey;
                    ulong unique;
                    while (readValue(socket, out gottenObject, out gottenKey, out unique))
                    {
                        foreach (int position in kv.Value[gottenKey])
                        {
                            returnValues[position] = gottenObject;
                            _uniques[position] = unique;
                        }
                    }
                });
            }
            uniques = _uniques;
            return returnValues;
        }

        //Private method for reading results of the "get" command.
        private bool readValue(MSocket socket, out object value, out string key, out ulong unique)
        {
            string response = socket.ReadResponse();
            string[] parts = response.Split(' '); //Result line from server: "VALUE <key> <flags> <bytes> <cas unique>"
            if (parts[0] == "VALUE")
            {
                key = parts[1];
                SerializedType type = (SerializedType)Enum.Parse(typeof(SerializedType), parts[2]);
                byte[] bytes = new byte[Convert.ToUInt32(parts[3], CultureInfo.InvariantCulture)];
                if (parts.Length > 4)
                {
                    unique = Convert.ToUInt64(parts[4]);
                }
                else
                {
                    unique = 0;
                }
                socket.Read(bytes);
                socket.SkipUntilEndOfLine(); //Skip the trailing \r\n
                try
                {
                    value = Serializer.DeSerialize(bytes, type);
                }
                catch (Exception e)
                {
                    //If deserialization fails, return null
                    value = null;
                    logger.Error("Error deserializing object for key '" + key + "' of type " + type + ".", e);
                }
                return true;
            }
            else
            {
                key = null;
                value = null;
                unique = 0;
                return false;
            }
        }
        #endregion

        #region Delete
        /// <summary>
        /// This method corresponds to the "delete" command in the memcache protocol.
        /// It will immediately delete the given key and corresponding value.
        /// Use the overloads to specify an amount of time the item should be in the delete queue on the server,
        /// or to specify a custom hash to override server selection.
        /// </summary>
        public bool Delete(string key) { return Delete(key, true, hash(key), 0); }


        private bool Delete(string key, bool keyIsChecked, uint hash, int time)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }

            return serverPool.Execute<bool>(hash, false, delegate(MSocket socket)
            {
                string commandline;
                if (time == 0)
                {
                    commandline = "delete " + keyPrefix + key + "\r\n";
                }
                else
                {
                    commandline = "delete " + keyPrefix + key + " " + time + "\r\n";
                }
                socket.Write(commandline);
                return socket.ReadResponse().StartsWith("DELETED");
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
        public bool FlushAll() { return FlushAll(TimeSpan.Zero, false); }
        public bool FlushAll(TimeSpan delay) { return FlushAll(delay, false); }
        public bool FlushAll(TimeSpan delay, bool staggered)
        {
            bool noerrors = true;
            uint count = 0;
            foreach (SocketPool pool in serverPool.HostList)
            {
                serverPool.Execute(pool, delegate(MSocket socket)
                {
                    uint delaySeconds = (staggered ? (uint)delay.TotalSeconds * count : (uint)delay.TotalSeconds);
                    //Funnily enough, "flush_all 0" has no effect, you have to send "flush_all" to flush immediately.
                    socket.Write("flush_all " + (delaySeconds == 0 ? "" : delaySeconds.ToString()) + "\r\n");
                    if (!socket.ReadResponse().StartsWith("OK"))
                    {
                        noerrors = false;
                    }
                    count++;
                });
            }
            return noerrors;
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
            Dictionary<string, string> result = new Dictionary<string, string>();
            serverPool.Execute(pool, delegate(MSocket socket)
            {
                socket.Write("stats\r\n");
                string line;
                while (!(line = socket.ReadResponse().TrimEnd('\0', '\r', '\n')).StartsWith("END"))
                {
                    string[] s = line.Split(' ');
                    result.Add(s[1], s[2]);
                }
            });
            return result;
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