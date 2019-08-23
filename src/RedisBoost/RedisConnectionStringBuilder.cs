#region Apache Licence, Version 2.0
/*
 Copyright 2015 Andrey Bulygin.

 Licensed under the Apache License, Version 2.0 (the "License"); 
 you may not use this file except in compliance with the License. 
 You may obtain a copy of the License at 

		http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software 
 distributed under the License is distributed on an "AS IS" BASIS, 
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
 See the License for the specific language governing permissions 
 and limitations under the License.
 */
#endregion

using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using RedisBoost.Core;

namespace RedisBoost
{
	public class RedisConnectionStringBuilder: DbConnectionStringBuilder
	{
		private readonly SqlConnectionStringBuilder _connectionStringBuilder;
		private readonly string _connectionString;
        public EndPoint EndPoint { get; private set; }
		public int DbIndex { get; private set; }
        public string Password { get; private set; } 

		public RedisConnectionStringBuilder(string host, int port = RedisConstants.DefaultPort, int dbIndex = 0, string password = default(string))
			: this(ParseEndpoint(string.Format("{0}:{1}", host, port)), dbIndex, password)
		{
		}
		public RedisConnectionStringBuilder(EndPoint endPoint,int dbIndex = 0, string password = default(string))
		{
            var builder = new System.Text.StringBuilder(string.Format("data source={0};initial catalog=\"{1}\"", endPoint, dbIndex));
            if (!string.IsNullOrWhiteSpace(password))
            {
                Password = ParsePassword(password);
                builder.AppendFormat(";password=\"{0}\"", password);
            }
            _connectionString = builder.ToString();
			EndPoint = endPoint;
			DbIndex = dbIndex;
		}

		public RedisConnectionStringBuilder(string connectionString)
		{
			_connectionString = connectionString;
			_connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
			EndPoint = ParseEndpoint(_connectionStringBuilder.DataSource);
			DbIndex = ParseDbIndex(_connectionStringBuilder.InitialCatalog);
            Password = ParsePassword(_connectionStringBuilder.Password);
		}

		private static int ParseDbIndex(string dbIndex)
		{
			if (string.IsNullOrWhiteSpace(dbIndex))
				return 0;
			int result = 0;
			if (!int.TryParse(dbIndex,out result))
				throw new RedisException("Invalid connection string");
			return result;
		}

		private static EndPoint ParseEndpoint(string address)
		{
			var port = RedisConstants.DefaultPort;
			var host = string.Empty;
			var parts = address.Split(':').Select(x => x.Trim()).ToArray();

			if (parts.Length >= 1)
				host = parts[0];
			if (parts.Length == 2 && !int.TryParse(parts[1], out port))
				throw new RedisException("Invalid connection string");

			IPAddress ipAddress;
			if (!IPAddress.TryParse(host,out ipAddress))
				return new DnsEndPoint(host, port, AddressFamily.InterNetwork);

			return new IPEndPoint(ipAddress,port);
		}
        private string ParsePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return default(string);
            }
            return password;
        }
		public override string ToString()
		{
			return _connectionString;
		}
	}
}
