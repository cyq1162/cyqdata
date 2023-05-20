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
        private static LogAdapter logger = LogAdapter.GetLogger(typeof(RedisClient));


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

        #region SetNx
        public bool SetNX(string key, object value, int seconds) { return SetNX("setnx", key, true, value, hash(key), seconds); }
        private bool SetNX(string command, string key, bool keyIsChecked, object value, uint hash, int expirySeconds)
        {
            if (!keyIsChecked)
            {
                checkKey(key);
            }

            string result = hostServer.Execute<string>(hash, "", delegate(MSocket socket)
            {
                SerializedType type;
                byte[] bytes;
                byte[] typeBit = new byte[1];
                try
                {

                    bytes = Serializer.Serialize(value, out type, compressionThreshold);
                    typeBit[0] = (byte)type;
                }
                catch (Exception e)
                {
                    logger.Error("Error serializing object for key '" + key + "'.", e);
                    return "";
                }
                // CheckDB(socket, hash);
                int db = GetDBIndex(socket, hash);
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

            string result = hostServer.Execute<string>(hash, "", delegate(MSocket socket)
              {
                  SerializedType type;
                  byte[] bytes;
                  byte[] typeBit = new byte[1];
                  try
                  {

                      bytes = Serializer.Serialize(value, out type, compressionThreshold);
                      typeBit[0] = (byte)type;
                  }
                  catch (Exception e)
                  {
                      logger.Error("Error serializing object for key '" + key + "'.", e);
                      return "";
                  }
                  // CheckDB(socket, hash);
                  int db = GetDBIndex(socket, hash);
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
                  return socket.ReadResponse();

              });
            return result == "+OK" || result == ":1";
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
            object value = hostServer.Execute<object>(hash, null, delegate(MSocket socket)
            {
                int db = GetDBIndex(socket, hash);
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
            return hostServer.Execute<bool>(hash, false, delegate(MSocket socket)
            {
                int db = GetDBIndex(socket, hash);
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
                return !result.StartsWith(":0") && !result.StartsWith("-");
            });
        }

        #endregion

        #region Select DB
        internal int GetDBIndex(MSocket socket, uint hash)
        {
            if (AppConfig.Redis.UseDBCount > 1 || AppConfig.Redis.UseDBIndex > 0)
            {
                return AppConfig.Redis.UseDBIndex > 0 ? AppConfig.Redis.UseDBIndex : (int)(hash % AppConfig.Redis.UseDBCount);//默认分散在16个DB中。
                //if (socket.DB != db)
                //{
                //    socket.DB = db;
                //    return (int)db;
                //}
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

            return hostServer.Execute<bool>(hash, false, delegate(MSocket socket)
            {
                int db = GetDBIndex(socket, hash);
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
                hostServer.Execute(pool, delegate(MSocket socket)
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
            Dictionary<string, string> dic = new Dictionary<string, string>();
            hostServer.Execute(pool, delegate(MSocket socket)
            {
                using (RedisCommand cmd = new RedisCommand(socket, 1, "info"))
                {

                }
                string result = socket.ReadResponse();
                if (!string.IsNullOrEmpty(result) && (result[0] == '$' || result == "+OK"))
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
                        if (s.Length > 1)
                        {
                            dic.Add(s[0], s[1]);
                        }
                    }


                }
            });
            return dic;
        }

        #endregion


    }
}