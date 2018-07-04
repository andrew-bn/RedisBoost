﻿
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RedisBoost.Tests
{
	[TestClass]
	public class RedisConnectionStringBuilderTests
	{
		[TestMethod]
		public static void InitializeByConnectionString_OnlyIpIncluded()
		{
			var cs = new RedisConnectionStringBuilder("data source=127.0.0.1");
			Assert.AreEqual("127.0.0.1:6379", cs.EndPoint.ToString());
		}
		[TestMethod]
		public static void InitializeByConnectionString_DefaultDbIndexIsZero()
		{
			var cs = new RedisConnectionStringBuilder("data source=127.0.0.1");
			Assert.AreEqual(0,cs.DbIndex);
		}
		[TestMethod]
		public static void InitializeByConnectionString_IpAndPortIncluded()
		{
			var cs = new RedisConnectionStringBuilder("data source=127.0.0.1:2312");
			Assert.AreEqual("127.0.0.1:2312", cs.EndPoint.ToString());
		}
		[TestMethod]
		public static void InitializeByConnectionString_DbIndexIncluded()
		{
			var cs = new RedisConnectionStringBuilder("data source=127.0.0.1; initial catalog=23");
			Assert.AreEqual(23, cs.DbIndex);
		}
		[TestMethod]
		public static void InitializeByHostPort_ValidConnectionString()
		{
			var cs = new RedisConnectionStringBuilder("127.0.0.1", 3242, 23);
			Assert.AreEqual("127.0.0.1:3242", cs.EndPoint.ToString());
			Assert.AreEqual(23, cs.DbIndex);
		}
	}
}
