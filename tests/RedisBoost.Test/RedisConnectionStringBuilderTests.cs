using Xunit;

namespace RedisBoost.Tests
{
	public class RedisConnectionStringBuilderTests
	{
		[Fact]
		public static void InitializeByConnectionString_OnlyIpIncluded()
		{
			var cs = new RedisConnectionStringBuilder("data source=127.0.0.1");
			Assert.Equal("127.0.0.1:6379", cs.EndPoint.ToString());
		}
		[Fact]
		public static void InitializeByConnectionString_DefaultDbIndexIsZero()
		{
			var cs = new RedisConnectionStringBuilder("data source=127.0.0.1");
			Assert.Equal(0, cs.DbIndex);
		}
		[Fact]
		public static void InitializeByConnectionString_IpAndPortIncluded()
		{
			var cs = new RedisConnectionStringBuilder("data source=127.0.0.1:2312");
			Assert.Equal("127.0.0.1:2312", cs.EndPoint.ToString());
		}
		[Fact]
		public static void InitializeByConnectionString_DbIndexIncluded()
		{
			var cs = new RedisConnectionStringBuilder("data source=127.0.0.1; initial catalog=23");
			Assert.Equal(23, cs.DbIndex);
		}
		[Fact]
		public static void InitializeByHostPort_ValidConnectionString()
		{
			var cs = new RedisConnectionStringBuilder("127.0.0.1", 3242, 23);
			Assert.Equal("127.0.0.1:3242", cs.EndPoint.ToString());
			Assert.Equal(23, cs.DbIndex);
		}
	}
}
