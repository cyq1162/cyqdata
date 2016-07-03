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
using System.Text;

namespace CYQ.Data.Cache {
	internal delegate T UseSocket<T>(PooledSocket socket);
	internal delegate void UseSocket(PooledSocket socket);

	/// <summary>
	/// The ServerPool encapsulates a collection of memcached servers and the associated SocketPool objects.
	/// This class contains the server-selection logic, and contains methods for executing a block of code on 
	/// a socket from the server corresponding to a given key.
	/// </summary>
	internal class ServerPool {
		private static LogAdapter logger = LogAdapter.GetLogger(typeof(ServerPool));

		//Expose the socket pools.
		private SocketPool[] hostList;
		internal SocketPool[] HostList { get { return hostList; } }

		private Dictionary<uint, SocketPool> hostDictionary;
		private uint[] hostKeys;

		//Internal configuration properties
		private int sendReceiveTimeout = 5000;
		private int connectTimeout = 3000;
		private uint maxPoolSize = 10;
		private uint minPoolSize = 1;
		private TimeSpan socketRecycleAge = TimeSpan.FromMinutes(30);
		internal int SendReceiveTimeout { get { return sendReceiveTimeout; } set { sendReceiveTimeout = value; } }
		internal int ConnectTimeout { get { return connectTimeout; } set { connectTimeout = value; } }
		internal uint MaxPoolSize { get { return maxPoolSize; } set { maxPoolSize = value; } }
		internal uint MinPoolSize { get { return minPoolSize; } set { minPoolSize = value; } }
		internal TimeSpan SocketRecycleAge { get { return socketRecycleAge; } set { socketRecycleAge = value; } }

		/// <summary>
		/// Internal constructor. This method takes the array of hosts and sets up an internal list of socketpools.
		/// </summary>
		internal ServerPool(string[] hosts) {
			hostDictionary = new Dictionary<uint, SocketPool>();
			List<SocketPool> pools = new List<SocketPool>();
			List<uint> keys = new List<uint>();
			foreach(string host in hosts) {
				//Create pool
				SocketPool pool = new SocketPool(this, host.Trim());

				//Create 250 keys for this pool, store each key in the hostDictionary, as well as in the list of keys.
				for (int i = 0; i < 250; i++) {
					uint key = BitConverter.ToUInt32(new ModifiedFNV1_32().ComputeHash(Encoding.UTF8.GetBytes(host + "-" + i)), 0);
					if (!hostDictionary.ContainsKey(key)) {
						hostDictionary[key] = pool;
						keys.Add(key);
					}
				}

				pools.Add(pool);
			}

			//Hostlist should contain the list of all pools that has been created.
			hostList = pools.ToArray();

			//Hostkeys should contain the list of all key for all pools that have been created.
			//This array forms the server key continuum that we use to lookup which server a
			//given item key hash should be assigned to.
			keys.Sort();
			hostKeys = keys.ToArray();
		}

		/// <summary>
		/// Given an item key hash, this method returns the socketpool which is closest on the server key continuum.
		/// </summary>
		internal SocketPool GetSocketPool(uint hash) {
			//Quick return if we only have one host.
			if (hostList.Length == 1) {
				return hostList[0];
			}

			//New "ketama" host selection.
			int i = Array.BinarySearch(hostKeys, hash);

			//If not exact match...
			if(i < 0) {
				//Get the index of the first item bigger than the one searched for.
				i = ~i;

				//If i is bigger than the last index, it was bigger than the last item = use the first item.
				if (i >= hostKeys.Length) {
					i = 0;
				}
			}
			return hostDictionary[hostKeys[i]];
		}

		internal SocketPool GetSocketPool(string host) {
			return Array.Find(HostList, delegate(SocketPool socketPool) { return socketPool.Host == host; });
		}

		/// <summary>
		/// This method executes the given delegate on a socket from the server that corresponds to the given hash.
		/// If anything causes an error, the given defaultValue will be returned instead.
		/// This method takes care of disposing the socket properly once the delegate has executed.
		/// </summary>
		internal T Execute<T>(uint hash, T defaultValue, UseSocket<T> use) {
			return Execute(GetSocketPool(hash), defaultValue, use);
		}

		internal T Execute<T>(SocketPool pool, T defaultValue, UseSocket<T> use) {
			PooledSocket sock = null;
			try {
				//Acquire a socket
				sock = pool.Acquire();

				//Use the socket as a parameter to the delegate and return its result.
				if (sock != null) {
					return use(sock);
				}
			} catch(Exception e) {
				logger.Error("Error in Execute<T>: " + pool.Host, e);

				//Socket is probably broken
				if (sock != null) {
					sock.Close();
				}
			} finally {
				if (sock != null) {
					sock.Dispose();
				}
			}
			return defaultValue;
		}

		internal void Execute(SocketPool pool, UseSocket use) {
			PooledSocket sock = null;
			try {
				//Acquire a socket
				sock = pool.Acquire();

				//Use the socket as a parameter to the delegate and return its result.
				if (sock != null) {
					use(sock);
				}
			} catch(Exception e) {
				logger.Error("Error in Execute: " + pool.Host, e);

				//Socket is probably broken
				if (sock != null) {
					sock.Close();
				}
			}
			finally {
				if(sock != null) {
					sock.Dispose();
				}
			}
		}

		/// <summary>
		/// This method executes the given delegate on all servers.
		/// </summary>
		internal void ExecuteAll(UseSocket use) {
			foreach(SocketPool socketPool in hostList){
				Execute(socketPool, use);
			}
		}
	}
}