using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using CYQ.Data.Cache;
using Microsoft.AspNetCore.Http;
using CYQ.Data;
namespace System.Web
{
    public class HttpSessionState : ISession
    {
        Microsoft.AspNetCore.Http.HttpContext context
        {
            get
            {
                return HttpContext.contextAccessor.HttpContext;
            }
        }

        internal HttpSessionState()
        {

        }
      
        private object ToObject(byte[] value)
        {
            try
            {
                if (value != null && value.Length > 0)
                {
                    SerializedType type = (SerializedType)value[0];
                    byte[] bytes = new byte[value.Length - 1];
                    Buffer.BlockCopy(value, 1, bytes, 0, bytes.Length);
                    return Serializer.DeSerialize(bytes, type);
                }
            }
            catch (Exception e)
            {
                Log.Write(e, LogType.Error);
            }
            return null;

        }
        private byte[] ToBytes(object value)
        {
            SerializedType type;
            byte[] bytes = null;

            try
            {
                bytes = Serializer.Serialize(value, out type, 1024 * 1000);//1M
                if (bytes != null)
                {
                    byte[] bytesWithType = new byte[bytes.Length + 1];
                    bytesWithType[0] = (byte)type;
                    Buffer.BlockCopy(bytes, 0, bytesWithType, 1, bytes.Length);
                    return bytesWithType;
                }
            }
            catch (Exception e)
            {
                Log.Write(e, LogType.Error);
            }
            return bytes;
        }
        public object this[int index]
        {
            get
            {
                if (SessionIsNull() || index < 0)
                {
                    return null;
                }
                int i = 0;
                foreach (var name in Keys)
                {
                    if (i == index)
                    {
                        return ToObject(context.Session.Get(name));
                    }
                    i++;
                }
                return null;
            }
            set
            {
                if (!SessionIsNull())
                {
                    int i = 0;
                    foreach (var name in Keys)
                    {
                        if (i == index)
                        {
                            if (value == null)
                            {
                                Remove(name);
                            }
                            else
                            {
                                context.Session.Set(name, ToBytes(value));
                                break;
                            }
                        }
                        i++;
                    }
                }
            }
        }
        public object this[string name]
        {
            get
            {
                if (SessionIsNull())
                {
                    return null;
                }
                return ToObject(context.Session.Get(name));
            }
            set
            {
                if (!SessionIsNull())
                {
                    if (value == null)
                    {
                        Remove(name);
                    }
                    else
                    {
                        context.Session.Set(name, ToBytes(value));
                    }
                }
            }
        }
        public string SessionID
        {
            get
            {
                if (SessionIsNull())
                {
                    return null;
                }
                return context.Session.Id;
            }
        }

        public bool IsAvailable
        {
            get
            {
                if (SessionIsNull())
                {
                    return false;
                }
                return context.Session.IsAvailable;
            }
        }

        public string Id => SessionID;

        public IEnumerable<string> Keys
        {
            get
            {
                if (SessionIsNull())
                {
                    return null;
                }
                return context.Session.Keys;
            }
        }


        public void Clear()
        {
            if (SessionIsNull())
            {
                return;
            }
            context.Session.Clear();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return context.Session.CommitAsync(cancellationToken = default(CancellationToken));
        }

        public Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return context.Session.LoadAsync(cancellationToken = default(CancellationToken));
        }

        public void Remove(string key)
        {
            if (SessionIsNull())
            {
                return;
            }
            context.Session.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            if (SessionIsNull())
            {
                return;
            }
            context.Session.Set(key, value);
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            if (SessionIsNull())
            {
                value = null;
                return false;
            }
            return context.Session.TryGetValue(key, out value);
        }
        private bool SessionIsNull()
        {
            if (context == null)
            {
                return true;
            }
            try
            {
                return context.Session == null;//可能抛异常
            }
            catch
            {
                return true;
            }
        }
        /// <summary>
        /// 向会话状态集合添加一个新项。
        /// </summary>
        public void Add(string name,object value)
        {
            Set(name, ToBytes(value));
        }
    }
}