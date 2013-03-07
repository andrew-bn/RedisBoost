using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NBoosters.RedisBoost.Core;

namespace NBoosters.RedisBoost
{
	public class RedisConnectionStringBuilder: DbConnectionStringBuilder
	{
		private readonly SqlConnectionStringBuilder _connectionStringBuilder;
		private readonly string _connectionString;
		public EndPoint EndPoint { get; private set; }
		public int DbIndex { get; private set; }

		public RedisConnectionStringBuilder(string host, int port = RedisConstants.DefaultPort, int dbIndex = 0)
			: this(string.Format("data source={0}:{1};initial catalog=\"{2}\"", host, port, dbIndex))
		{
		}
		public RedisConnectionStringBuilder(EndPoint endPoint,int dbIndex = 0)
		{
			_connectionString = string.Format("data source={0};initial catalog=\"{1}\"", endPoint, dbIndex);
			EndPoint = endPoint;
			DbIndex = dbIndex;
		}

		public RedisConnectionStringBuilder(string connectionString)
		{
			_connectionString = connectionString;
			_connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
			EndPoint = ParseEndpoint(_connectionStringBuilder.DataSource);
			DbIndex = ParseDbIndex(_connectionStringBuilder.InitialCatalog);
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

		public override string ToString()
		{
			return _connectionString;
		}
	}
}
