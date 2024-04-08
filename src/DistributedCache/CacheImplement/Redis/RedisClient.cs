using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using CYQ.Data.Tool;

namespace CYQ.Data.Cache
{
    /// <summary>
    /// Redis client main class.
    /// Use the static methods Setup and GetInstance to setup and get an instance of the client for use.
    /// </summary>
    internal class RedisClient : ClientBase
    {
        #region Static fields and methods.

        public static RedisClient Create(string configValue)
        {
            return new RedisClient(configValue);
        }


        private RedisClient(string configValue)
        {
            hostServer = new HostServer(CacheType.Redis, configValue);
            hostServer.OnAuthEvent += new HostServer.AuthDelegate(hostServer_OnAuthEvent);
        }

        bool hostServer_OnAuthEvent(MSocket socket)
        {
            if (!Auth(socket.HostNode.Password, socket))
            {
                string err = "Auth password fail : " + socket.HostNode.Password;
                socket.HostNode.Error = err;
                Error.Throw(err);
            }
            return true;
        }
        #endregion

        #region Add、SetNX
        public bool Add(string key, object value, int seconds) { return Add("setnx", key, true, value, hash(key), seconds); }
        private bool Add(string command, string key, bool keyIsChecked, object value, uint hash, int expirySeconds)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }

            string result = hostServer.Execute<string>(hash, "", delegate (MSocket socket, out bool isNoResponse)
            {
                SerializedType type;
                byte[] bytes;
                byte[] typeBit = new byte[1];

                bytes = Serializer.Serialize(value, out type, compressionThreshold);
                typeBit[0] = (byte)type;

                // CheckDB(socket, hash);
                int db = GetDBIndex(hash);
                // Console.WriteLine("Set :" + key + ":" + hash + " db." + db);
                int skipCmd = 0;
                using (RedisCommand cmd = new RedisCommand(socket))
                {
                    if (db > -1)
                    {
                        cmd.Reset(2, "Select");
                        cmd.AddKey(db.ToString());
                        skipCmd++;
                    }
                    cmd.Reset(3, command);
                    cmd.AddKey(key);
                    cmd.AddValue(typeBit, bytes);

                    cmd.Reset(2, "ttl");
                    cmd.AddKey(key);//检测失效时间（是否返回-1，可能未设置过期时间，解决setNx和expire原子性问题）

                    cmd.Send();
                    socket.SkipToEndOfLine(skipCmd);
                    result = socket.ReadResponse();
                    string ttl = socket.ReadResponse();
                    if (result == ":1" || ttl == ":-1")
                    {
                        if (expirySeconds > 0)
                        {
                            cmd.Reset(3, "EXPIRE");
                            cmd.AddKey(key);
                            cmd.AddKey(expirySeconds.ToString());
                            cmd.Send();
                            //result = socket.ReadResponse();
                            //if (result != ":1")
                            //{
                            //    cmd.Reset(2, "DEL");
                            //    cmd.AddKey(key);
                            //    cmd.Send();
                            socket.SkipToEndOfLine(1);//跳过结果
                            //}
                        }
                    }
                    isNoResponse = string.IsNullOrEmpty(result);
                    return result;
                }
            });
            return result.StartsWith("+OK") || result.StartsWith(":1");
        }
        #endregion

        #region Set、Append

        public bool Append(string key, object value, int seconds) { return Set("append", key, true, value, hash(key), seconds); }
        public bool Set(string key, object value, int seconds) { return Set("set", key, true, value, hash(key), seconds); }


        private bool Set(string command, string key, bool keyIsChecked, object value, uint hash, int expirySeconds)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }
            UseSocket<string> useSocket = delegate (MSocket socket, out bool isNoResponse)
            {
                SerializedType type;
                byte[] bytes;
                byte[] typeBit = new byte[1];


                bytes = Serializer.Serialize(value, out type, compressionThreshold);
                typeBit[0] = (byte)type;

                // CheckDB(socket, hash);
                int db = GetDBIndex(hash);
                // Console.WriteLine("Set :" + key + ":" + hash + " db." + db);
                int skipCmd = 0;
                using (RedisCommand cmd = new RedisCommand(socket))
                {
                    if (db > -1)
                    {
                        cmd.Reset(2, "Select");
                        cmd.AddKey(db.ToString());
                        skipCmd++;
                    }
                    cmd.Reset(3, command);
                    cmd.AddKey(key);
                    cmd.AddValue(typeBit, bytes);
                    skipCmd++;

                    if (expirySeconds > 0)
                    {
                        cmd.Reset(3, "EXPIRE");
                        cmd.AddKey(key);
                        cmd.AddKey(expirySeconds.ToString());
                        skipCmd++;
                    }
                }
                socket.SkipToEndOfLine(skipCmd - 1);//取最后1次命令的结果
                string responseText = socket.ReadResponse();
                isNoResponse = string.IsNullOrEmpty(responseText);
                return responseText;

            };
            string result = hostServer.Execute<string>(hash, "", useSocket);
            bool ret = result == "+OK" || result == ":1";
            return ret;
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
            object value = hostServer.Execute<object>(hash, null, delegate (MSocket socket, out bool isNoResponse)
            {
                int db = GetDBIndex(hash);
                using (RedisCommand cmd = new RedisCommand(socket))
                {
                    if (db > -1)
                    {
                        cmd.Reset(2, "Select");
                        cmd.AddKey(db.ToString());
                    }
                    cmd.Reset(2, command);
                    cmd.AddKey(key);
                }
                if (db > -1) { socket.SkipToEndOfLine(); }
                string result = socket.ReadResponse();
                isNoResponse = string.IsNullOrEmpty(result);
                if (!string.IsNullOrEmpty(result) && result[0] == '$')
                {
                    int len = 0;
                    if (int.TryParse(result.Substring(1), out len) && len > 0)
                    {
                        byte[] bytes = socket.ReadBytes(len);
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
            return hostServer.Execute<bool>(hash, false, delegate (MSocket socket, out bool isNoResponse)
            {
                int db = GetDBIndex(hash);
                //Console.WriteLine("ContainsKey :" + key + ":" + hash + " db." + db);
                using (RedisCommand cmd = new RedisCommand(socket))
                {
                    if (db > -1)
                    {
                        cmd.Reset(2, "Select");
                        cmd.AddKey(db.ToString());
                    }
                    cmd.Reset(2, "Exists");
                    cmd.AddKey(key);
                }
                if (db > -1) { socket.SkipToEndOfLine(); }
                string result = socket.ReadResponse();
                isNoResponse = string.IsNullOrEmpty(result);
                return !result.StartsWith(":0") && !result.StartsWith("-");
            });
        }

        #endregion

        #region Select DB
        internal int GetDBIndex(uint hash)
        {
            int dbIndex = AppConfig.Redis.UseDBIndex;
            if (dbIndex > 0)
            {
                return dbIndex;
            }
            int dbCount = AppConfig.Redis.UseDBCount;

            if (dbCount > 1)
            {
                //再取hash，是因为hash在选节点的时候，可能已经被分过一次。
                //如果不再取hash，节点进来后，就变成当前节点可能全是单或全是双。
                int index = Math.Abs(hash.ToString().GetHashCode() % dbCount);
                return index;//默认分散在16个DB中。
            }
            return -1;
        }

        #endregion

        #region Delete


        public bool Delete(string key) { return Delete(key, true, hash(key), 0); }

        private bool Delete(string key, bool keyIsChecked, uint hash, int time)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }

            return hostServer.Execute<bool>(hash, false, delegate (MSocket socket, out bool isNoResponse)
            {
                int db = GetDBIndex(hash);
                using (RedisCommand cmd = new RedisCommand(socket))
                {
                    if (db > -1)
                    {
                        cmd.Reset(2, "Select");
                        cmd.AddKey(db.ToString());
                    }
                    cmd.Reset(2, "DEL");
                    cmd.AddKey(key);
                }
                if (db > -1)
                {
                    socket.SkipToEndOfLine();
                }
                string result = socket.ReadResponse();
                isNoResponse = string.IsNullOrEmpty(result);
                return result.StartsWith(":1");
            });
        }
        #endregion

        #region Auth

        private bool Auth(string password, MSocket socket)
        {
            if (!string.IsNullOrEmpty(password))
            {
                using (RedisCommand cmd = new RedisCommand(socket, 2, "AUTH"))
                {
                    cmd.AddKey(password);
                }
                string result = socket.ReadLine();
                return result.StartsWith("+OK");
            }
            return true;
        }
        #endregion

        #region Flush All

        public bool FlushAll()
        {
            foreach (KeyValuePair<string, HostNode> item in hostServer.HostList)
            {
                HostNode pool = item.Value;
                hostServer.Execute(pool, delegate (MSocket socket)
                {
                    using (RedisCommand cmd = new RedisCommand(socket, 1, "flushall"))
                    {
                        cmd.Send();
                        socket.SkipToEndOfLine();
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
            foreach (KeyValuePair<string, HostNode> item in hostServer.HostList)
            {
                results.Add(item.Key, Stats(item.Value));
            }
            return results;
        }

        private Dictionary<string, string> Stats(HostNode pool)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            hostServer.Execute(pool, delegate (MSocket socket)
            {
                using (RedisCommand cmd = new RedisCommand(socket, 1, "info"))
                {

                }
                string result = socket.ReadResponse();
                if (!string.IsNullOrEmpty(result) && (result[0] == '$' || result == "+OK"))
                {
                    string line = null;
                    bool isEnd = false;
                    while (true)
                    {
                        try
                        {
                            line = socket.ReadLine();
                        }
                        catch (Exception err)
                        {
                            break;
                        }

                        if (line == null)
                        {
                            if (isEnd)
                            {
                                break;
                            }
                            continue;
                        }
                        else if (line == "# Keyspace")
                        {
                            isEnd = true;
                        }
                        string[] s = line.Split(':');
                        if (s.Length > 1)
                        {
                            dic.Add(s[0], s[1]);
                        }
                        else
                        {
                            dic.Add(line, "- - -");
                        }
                    }


                }
            });
            return dic;
        }

        #endregion


        #region Exe All
        public int SetAll(string key, object value, int seconds) { return SetAll("set", key, true, value, hash(key), seconds); }
        private int SetAll(string command, string key, bool keyIsChecked, object value, uint hash, int expirySeconds)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }
            UseSocket<bool> useSocket = delegate (MSocket socket, out bool isNoResponse)
            {
                SerializedType type;
                byte[] bytes;
                byte[] typeBit = new byte[1];


                bytes = Serializer.Serialize(value, out type, compressionThreshold);
                typeBit[0] = (byte)type;

                // CheckDB(socket, hash);
                int db = GetDBIndex(hash);
                // Console.WriteLine("Set :" + key + ":" + hash + " db." + db);
                int skipCmd = 0;
                using (RedisCommand cmd = new RedisCommand(socket))
                {
                    if (db > -1)
                    {
                        cmd.Reset(2, "Select");
                        cmd.AddKey(db.ToString());
                        skipCmd++;
                    }
                    cmd.Reset(3, command);
                    cmd.AddKey(key);
                    cmd.AddValue(typeBit, bytes);
                    skipCmd++;

                    if (expirySeconds > 0)
                    {
                        cmd.Reset(3, "EXPIRE");
                        cmd.AddKey(key);
                        cmd.AddKey(expirySeconds.ToString());
                        skipCmd++;
                    }
                }
                socket.SkipToEndOfLine(skipCmd - 1);//取最后1次命令的结果
                string result = socket.ReadResponse();
                isNoResponse = string.IsNullOrEmpty(result);
                bool ret = result == "+OK" || result == ":1";
                return ret;

            };
            return hostServer.ExecuteAll(useSocket, hash);
        }

        public int DeleteAll(string key)
        {
            return DeleteAll(key, true, hash(key), 0);
        }
        private int DeleteAll(string key, bool keyIsChecked, uint hash, int time)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }
            UseSocket<bool> useSocket = delegate (MSocket socket, out bool isNoResponse)
            {
                int db = GetDBIndex(hash);
                using (RedisCommand cmd = new RedisCommand(socket))
                {
                    if (db > -1)
                    {
                        cmd.Reset(2, "Select");
                        cmd.AddKey(db.ToString());
                    }
                    cmd.Reset(2, "DEL");
                    cmd.AddKey(key);
                }
                if (db > -1)
                {
                    socket.SkipToEndOfLine();
                }
                string result = socket.ReadResponse();
                isNoResponse = string.IsNullOrEmpty(result);
                return result.StartsWith(":1");
            };
            return hostServer.ExecuteAll(useSocket, hash);
        }

        public int AddAll(string key, object value, int seconds) { return AddAll("setnx", key, true, value, hash(key), seconds); }
        private int AddAll(string command, string key, bool keyIsChecked, object value, uint hash, int expirySeconds)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }
            UseSocket<bool> useSocket = delegate (MSocket socket, out bool isNoResponse)
            {
                string result = string.Empty;
                SerializedType type;
                byte[] bytes;
                byte[] typeBit = new byte[1];

                bytes = Serializer.Serialize(value, out type, compressionThreshold);
                typeBit[0] = (byte)type;

                // CheckDB(socket, hash);
                int db = GetDBIndex(hash);
                // Console.WriteLine("Set :" + key + ":" + hash + " db." + db);
                int skipCmd = 0;
                using (RedisCommand cmd = new RedisCommand(socket))
                {
                    if (db > -1)
                    {
                        cmd.Reset(2, "Select");
                        cmd.AddKey(db.ToString());
                        skipCmd++;
                    }
                    cmd.Reset(3, command);
                    cmd.AddKey(key);
                    cmd.AddValue(typeBit, bytes);

                    cmd.Reset(2, "ttl");
                    cmd.AddKey(key);//检测失效时间（是否返回-1，可能未设置过期时间，解决setNx和expire原子性问题）

                    cmd.Send();
                    socket.SkipToEndOfLine(skipCmd);
                    result = socket.ReadResponse();
                    string ttl = socket.ReadResponse();
                    if (result == ":1" || ttl == ":-1")
                    {
                        if (expirySeconds > 0)
                        {
                            cmd.Reset(3, "EXPIRE");
                            cmd.AddKey(key);
                            cmd.AddKey(expirySeconds.ToString());
                            cmd.Send();
                            socket.SkipToEndOfLine(1);//跳过结果
                        }
                    }
                    isNoResponse = string.IsNullOrEmpty(result);
                    return result.StartsWith("+OK") || result.StartsWith(":1");
                }
            };

            return hostServer.ExecuteAll(useSocket, hash);

            //List<HostNode> okNodeList = new List<HostNode>();
            //List<string> hosts = hostServer.HostList.GetKeys();
            //int exeCount = 0;
            //int okCount = 0;
            //foreach (string host in hosts)
            //{
            //    HostNode hostNode = hostServer.HostList[host];
            //    if (!hostNode.IsEndPointDead)
            //    {
            //        exeCount++;
            //    }
            //    bool isOK = hostServer.Execute<bool>(hostNode, hash, false, , false);
            //    if (isOK)
            //    {
            //        okCount++;
            //        okNodeList.Add(hostNode);
            //    }
            //}
            //bool retResult = false;
            //if (exeCount < 3)
            //{
            //    retResult = okCount > 0 && okCount == exeCount;//2个节点以下，要求全部成功。
            //}
            //else
            //{
            //    retResult = okCount > exeCount / 2 + 1;//超过1半的成功。
            //}
            //if (!retResult && okCount > 0)
            //{
            //    foreach (var node in okNodeList)
            //    {
            //        hostServer.Execute(node, delegate (MSocket socket)
            //        {
            //            int db = GetDBIndex(socket, hash);
            //            using (RedisCommand cmd = new RedisCommand(socket))
            //            {
            //                if (db > -1)
            //                {
            //                    cmd.Reset(2, "Select");
            //                    cmd.AddKey(db.ToString());
            //                }
            //                cmd.Reset(2, "DEL");
            //                cmd.AddKey(key);
            //            }
            //            if (db > -1)
            //            {
            //                socket.SkipToEndOfLine();
            //            }
            //            socket.SkipToEndOfLine();
            //        });
            //    }
            //}
            //return retResult;
        }

        public bool SetNXAll(string key, object value, int seconds) { return SetNXAll("setnx", key, true, value, hash(key), seconds); }
        private bool SetNXAll(string command, string key, bool keyIsChecked, object value, uint hash, int expirySeconds)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }
            UseSocket<bool> useSocket = delegate (MSocket socket, out bool isNoResponse)
            {
                string result = string.Empty;
                SerializedType type;
                byte[] bytes;
                byte[] typeBit = new byte[1];

                bytes = Serializer.Serialize(value, out type, compressionThreshold);
                typeBit[0] = (byte)type;

                // CheckDB(socket, hash);
                int db = GetDBIndex(hash);
                // Console.WriteLine("Set :" + key + ":" + hash + " db." + db);
                int skipCmd = 0;
                using (RedisCommand cmd = new RedisCommand(socket))
                {
                    if (db > -1)
                    {
                        cmd.Reset(2, "Select");
                        cmd.AddKey(db.ToString());
                        skipCmd++;
                    }
                    cmd.Reset(3, command);
                    cmd.AddKey(key);
                    cmd.AddValue(typeBit, bytes);

                    cmd.Reset(2, "ttl");
                    cmd.AddKey(key);//检测失效时间（是否返回-1，可能未设置过期时间，解决setNx和expire原子性问题）

                    cmd.Send();
                    socket.SkipToEndOfLine(skipCmd);
                    result = socket.ReadResponse();
                    string ttl = socket.ReadResponse();
                    if (result == ":1" || ttl == ":-1")
                    {
                        if (expirySeconds > 0)
                        {
                            cmd.Reset(3, "EXPIRE");
                            cmd.AddKey(key);
                            cmd.AddKey(expirySeconds.ToString());
                            cmd.Send();
                            socket.SkipToEndOfLine(1);//跳过结果
                        }
                    }
                    isNoResponse = string.IsNullOrEmpty(result);
                    return result.StartsWith("+OK") || result.StartsWith(":1");
                }
            };

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
                bool isOK = hostServer.Execute<bool>(hostNode, hash, false, useSocket, false);
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
                    hostServer.Execute(node, delegate (MSocket socket)
                    {
                        int db = GetDBIndex(hash);
                        using (RedisCommand cmd = new RedisCommand(socket))
                        {
                            if (db > -1)
                            {
                                cmd.Reset(2, "Select");
                                cmd.AddKey(db.ToString());
                            }
                            cmd.Reset(2, "DEL");
                            cmd.AddKey(key);
                        }
                        if (db > -1)
                        {
                            socket.SkipToEndOfLine();
                        }
                        socket.SkipToEndOfLine();
                    });
                }
            }
            return retResult;
        }

        #endregion

    }
}