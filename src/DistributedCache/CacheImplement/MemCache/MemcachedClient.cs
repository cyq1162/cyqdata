
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// Memcached client main class.
    /// Use the static methods Setup and GetInstance to setup and get an instance of the client for use.
    /// </summary>
    internal class MemcachedClient : ClientBase
    {
        #region Static fields and methods.
        private static LogAdapter logger = LogAdapter.GetLogger(typeof(MemcachedClient));

        public static MemcachedClient Create(string configValue)
        {
            return new MemcachedClient(configValue);
        }
        //Private constructor
        private MemcachedClient(string configValue)
        {
            this.HostServer = new HostServer(CacheType.MemCache, configValue);
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
        //public bool Set(string key, object value, uint hash) { return store("set", key, false, value, this.hash(hash), 0); }
        public bool Set(string key, object value, TimeSpan expiry) { return store("set", key, true, value, hash(key), (int)expiry.TotalSeconds); }
        //public bool Set(string key, object value, uint hash, TimeSpan expiry) { return store("set", key, false, value, this.hash(hash), (int)expiry.TotalSeconds); }
        public bool Set(string key, object value, DateTime expiry) { return store("set", key, true, value, hash(key), getUnixTime(expiry)); }
        // public bool Set(string key, object value, uint hash, DateTime expiry) { return store("set", key, false, value, this.hash(hash), getUnixTime(expiry)); }

        /// <summary>
        /// This method corresponds to the "add" command in the memcached protocol. 
        /// It will set the given key to the given value only if the key does not already exist.
        /// Using the overloads it is possible to specify an expiry time, either relative as a TimeSpan or 
        /// absolute as a DateTime. It is also possible to specify a custom hash to override server selection.
        /// This method returns true if the value was successfully added.
        /// </summary>
        public bool Add(string key, object value) { return store("add", key, true, value, hash(key), 0); }
        ////public bool Add(string key, object value, uint hash) { return store("add", key, false, value, this.hash(hash), 0); }
        public bool Add(string key, object value, TimeSpan expiry) { return store("add", key, true, value, hash(key), (int)expiry.TotalSeconds); }
        //public bool Add(string key, object value, uint hash, TimeSpan expiry) { return store("add", key, false, value, this.hash(hash), (int)expiry.TotalSeconds); }
        public bool Add(string key, object value, DateTime expiry) { return store("add", key, true, value, hash(key), getUnixTime(expiry)); }
        //public bool Add(string key, object value, uint hash, DateTime expiry) { return store("add", key, false, value, this.hash(hash), getUnixTime(expiry)); }

        /// <summary>
        /// This method corresponds to the "replace" command in the memcached protocol. 
        /// It will set the given key to the given value only if the key already exists.
        /// Using the overloads it is possible to specify an expiry time, either relative as a TimeSpan or 
        /// absolute as a DateTime. It is also possible to specify a custom hash to override server selection.
        /// This method returns true if the value was successfully replaced.
        /// </summary>
        //public bool Replace(string key, object value) { return store("replace", key, true, value, hash(key), 0); }
        //public bool Replace(string key, object value, uint hash) { return store("replace", key, false, value, this.hash(hash), 0); }
        //public bool Replace(string key, object value, TimeSpan expiry) { return store("replace", key, true, value, hash(key), (int)expiry.TotalSeconds); }
        //public bool Replace(string key, object value, uint hash, TimeSpan expiry) { return store("replace", key, false, value, this.hash(hash), (int)expiry.TotalSeconds); }
        //public bool Replace(string key, object value, DateTime expiry) { return store("replace", key, true, value, hash(key), getUnixTime(expiry)); }
        //public bool Replace(string key, object value, uint hash, DateTime expiry) { return store("replace", key, false, value, this.hash(hash), getUnixTime(expiry)); }



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

            return hostServer.Execute<string>(hash, "", delegate(MSocket socket, out bool isNoResponse)
            {
                SerializedType type;
                byte[] bytes;

                //Serialize object efficiently, store the datatype marker in the flags property.
                bytes = Serializer.Serialize(value, out type, compressionThreshold);

                //Create commandline
                string commandline = "";
                switch (command)
                {
                    case "set":
                    case "add":
                    case "replace":
                        commandline = command + " " + key + " " + (ushort)type + " " + expiry + " " + bytes.Length + "\r\n";
                        break;
                    case "append":
                    case "prepend":
                        commandline = command + " " + key + " 0 0 " + bytes.Length + "\r\n";
                        break;
                    case "cas":
                        commandline = command + " " + key + " " + (ushort)type + " " + expiry + " " + bytes.Length + " " + unique + "\r\n";
                        break;
                }

                //Write commandline and serialized object.
                socket.Write(commandline);
                socket.Write(bytes);
                socket.Write("\r\n");
                string result = socket.ReadResponse();
                isNoResponse = string.IsNullOrEmpty(result);
                return result;
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
        // public object Get(string key, uint hash) { ulong i; return get("get", key, false, this.hash(hash), out i); }

        private object get(string command, string key, bool keyIsChecked, uint hash, out ulong unique)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }

            ulong __unique = 0;
            object value = hostServer.Execute<object>(hash, null, delegate(MSocket socket, out bool isNoResponse)
            {
                socket.Write(command + " " + key + "\r\n");
                object _value;
                ulong _unique;
                if (readValue(socket, out _value, out key, out _unique, out isNoResponse))
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
            Dictionary<HostNode, Dictionary<string, List<int>>> dict = new Dictionary<HostNode, Dictionary<string, List<int>>>();
            for (int i = 0; i < keys.Length; i++)
            {
                Dictionary<string, List<int>> getsForServer;
                HostNode pool = hostServer.GetHost(hashes[i]);
                if (!dict.TryGetValue(pool, out getsForServer))
                {
                    dict[pool] = getsForServer = new Dictionary<string, List<int>>();
                }

                List<int> positions;
                if (!getsForServer.TryGetValue(keys[i], out positions))
                {
                    getsForServer[keys[i]] = positions = new List<int>();
                }
                positions.Add(i);
            }

            //Get the values
            object[] returnValues = new object[keys.Length];
            ulong[] _uniques = new ulong[keys.Length];
            foreach (KeyValuePair<HostNode, Dictionary<string, List<int>>> kv in dict)
            {
                hostServer.Execute(kv.Key, delegate(MSocket socket)
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
                    bool isNoResponse;
                    while (readValue(socket, out gottenObject, out gottenKey, out unique, out isNoResponse))
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
        private bool readValue(MSocket socket, out object value, out string key, out ulong unique, out bool isNoResponse)
        {
            string response = socket.ReadResponse();
            isNoResponse = string.IsNullOrEmpty(response);
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
                socket.SkipToEndOfLine(); //Skip the trailing \r\n

                value = Serializer.DeSerialize(bytes, type);

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

            return hostServer.Execute<bool>(hash, false, delegate(MSocket socket, out bool isNoResponse)
            {
                string commandline;
                if (time == 0)
                {
                    commandline = "delete " + key + "\r\n";
                }
                else
                {
                    commandline = "delete " + key + " " + time + "\r\n";
                }
                socket.Write(commandline);
                string result = socket.ReadResponse();
                isNoResponse = string.IsNullOrEmpty(result);
                return result.StartsWith("DELETED");
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
            foreach (KeyValuePair<string, HostNode> item in hostServer.HostList)
            {
                HostNode pool = item.Value;
                hostServer.Execute(pool, delegate(MSocket socket)
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
            foreach (KeyValuePair<string, HostNode> item in hostServer.HostList)
            {
                results.Add(item.Key, stats(item.Value));
            }
            return results;
        }
        private Dictionary<string, string> stats(HostNode pool)
        {
            if (pool == null)
            {
                return null;
            }
            Dictionary<string, string> result = new Dictionary<string, string>();
            hostServer.Execute(pool, delegate(MSocket socket)
            {
                socket.Write("stats\r\n");
                string line;
                while (!(line = socket.ReadResponse().TrimEnd('\0', '\r', '\n')).StartsWith("END"))
                {
                    string[] s = line.Split(' ');
                    if (s.Length > 2)
                    {
                        result.Add(s[1], s[2]);
                    }
                }
            });
            return result;
        }

        #endregion

        #region Exe All
        public void SetAll(string key, object value, DateTime expiry) { storeAll("set", key, true, value, getUnixTime(expiry), 0); }
        private void storeAll(string command, string key, bool keyIsChecked, object value, int expiry, ulong unique)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }

            hostServer.ExecuteAll(delegate(MSocket socket)
            {
                SerializedType type;
                byte[] bytes;

                //Serialize object efficiently, store the datatype marker in the flags property.
                bytes = Serializer.Serialize(value, out type, compressionThreshold);

                //Create commandline
                string commandline = "";
                switch (command)
                {
                    case "set":
                    case "add":
                    case "replace":
                        commandline = command + " " + key + " " + (ushort)type + " " + expiry + " " + bytes.Length + "\r\n";
                        break;
                    case "append":
                    case "prepend":
                        commandline = command + " " + key + " 0 0 " + bytes.Length + "\r\n";
                        break;
                    case "cas":
                        commandline = command + " " + key + " " + (ushort)type + " " + expiry + " " + bytes.Length + " " + unique + "\r\n";
                        break;
                }

                //Write commandline and serialized object.
                socket.Write(commandline);
                socket.Write(bytes);
                socket.Write("\r\n");
                socket.SkipToEndOfLine();
            });
        }

        public void DeleteAll(string key) {  DeleteAll(key, true, hash(key), 0); }


        private void DeleteAll(string key, bool keyIsChecked, uint hash, int time)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }

            hostServer.ExecuteAll(delegate(MSocket socket)
            {
                string commandline;
                if (time == 0)
                {
                    commandline = "delete " + key + "\r\n";
                }
                else
                {
                    commandline = "delete " + key + " " + time + "\r\n";
                }
                socket.Write(commandline);
                socket.SkipToEndOfLine();
            });
        }

        public bool AddAll(string key, object value, DateTime expiry) { return addAll("add", key, true, value, hash(key), getUnixTime(expiry)); }

        private bool addAll(string command, string key, bool keyIsChecked, object value, uint hash, int expiry)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }
            List<HostNode> okNodeList = new List<HostNode>();
            List<string> hosts = hostServer.HostList.GetKeys();
            int exeCount = 0;
            int okCount = 0;
            foreach (string host in hosts)
            {
                HostNode hostNode = hostServer.HostList[host];
                if (!hostNode.IsEndPointDead)
                {
                    exeCount++;
                }
                bool isOK = hostServer.Execute<bool>(hostNode, hash, false, delegate(MSocket socket, out bool isNoResponse)
                {
                    SerializedType type;
                    byte[] bytes;

                    //Serialize object efficiently, store the datatype marker in the flags property.
                    bytes = Serializer.Serialize(value, out type, compressionThreshold);

                    //Create commandline
                    string commandline = command + " " + key + " " + (ushort)type + " " + expiry + " " + bytes.Length + "\r\n";
                    
                    //Write commandline and serialized object.
                    socket.Write(commandline);
                    socket.Write(bytes);
                    socket.Write("\r\n");
                    string result = socket.ReadResponse();
                    isNoResponse = string.IsNullOrEmpty(result);
                    return result.StartsWith("STORED");
                   

                }, false);
                if (isOK)
                {
                    okCount++;
                    okNodeList.Add(hostNode);
                }
            }

            bool retResult = false;
            if (exeCount < 3)
            {
                retResult = okCount > 0 && okCount == exeCount;//2个节点以下，要求全部成功。
            }
            else
            {
                retResult = okCount > exeCount / 2 + 1;//超过1半的成功。
            }
            if (!retResult && okCount > 0)
            {
                foreach (var node in okNodeList)
                {
                    hostServer.Execute(node, delegate(MSocket socket)
                    {
                        string commandline = "delete " + key + "\r\n";
                        socket.Write(commandline);
                        socket.SkipToEndOfLine();
                    });
                }
            }
            return retResult;
        }

        #endregion
    }
}