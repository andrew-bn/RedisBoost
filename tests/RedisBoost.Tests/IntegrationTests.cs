using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RedisBoost.Tests
{
	[TestClass]
	public class IntegrationTests
	{
	    public IntegrationTests()
	    {
            var _builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            _configuration = _builder.Build();
        }

        [TestMethod]
		public void FlushDb()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				cli.FlushDbAsync().Wait();
				var result = cli.KeysAsync("*").Result;
				Assert.AreEqual(0, result.Length);
			}
		}
		[TestMethod]
		public void Set()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
			}
		}
		[TestMethod]
		public void Del()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value adfadf")).Wait();
				cli.DelAsync("Key").Wait();

				var result = cli.KeysAsync("*").Result;
				Assert.AreEqual(0, result.Length);
			}
		}
		[TestMethod]
		public void BitOp()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("key1", GetBytes("foobar")).Wait();
				cli.SetAsync("key2", GetBytes("abcdef")).Wait();
				cli.BitOpAsync(BitOpType.And, "dst", "key1", "key2").Wait();
				Assert.AreEqual("`bc`ab", GetString(cli.GetAsync("dst").Result));
			}
		}
		[TestMethod]
		public void GetSetBit()
		{
			using (var cli = CreateClient())
			{
				Assert.AreEqual(0, cli.SetBitAsync("key", 7, 1).Result);
				Assert.AreEqual(0, cli.GetBitAsync("key", 0).Result);
				Assert.AreEqual(1, cli.GetBitAsync("key", 7).Result);
				Assert.AreEqual(0, cli.GetBitAsync("key", 100).Result);
			}
		}
		[TestMethod]
		public void GetBit()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("key1", GetBytes("foobar")).Wait();
				cli.SetAsync("key2", GetBytes("abcdef")).Wait();
				cli.BitOpAsync(BitOpType.And, "dst", "key1", "key2").Wait();
				Assert.AreEqual("`bc`ab", GetString(cli.GetAsync("dst").Result));
			}
		}
		[TestMethod]
		public void Dump()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var result = cli.DumpAsync("Key").Result;
				Assert.AreEqual(17, result.Length);
			}
		}
		[TestMethod]
		public void Restore()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var result = cli.DumpAsync("Key").Result;
				cli.FlushDbAsync().Wait();

				var status = cli.RestoreAsync("Key", 0, result).Result;
				Assert.AreEqual("OK", status);
				Assert.AreEqual(1, cli.ExistsAsync("Key").Result);
			}
		}
		[TestMethod]
		public void Type()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				Assert.AreEqual("string", cli.TypeAsync("Key").Result);
			}
		}
		[TestMethod]
		public void Type_NoKey()
		{
			using (var cli = CreateClient())
			{
				Assert.AreEqual("none", cli.TypeAsync("Key").Result);
			}
		}
		[TestMethod]
		public void Exists()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var result = cli.ExistsAsync("Key").Result;
				Assert.AreEqual(1, result);
			}
		}
		[TestMethod]
		public void Expire()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var expR = cli.ExpireAsync("Key", 2).Result;
				Assert.AreEqual(1, expR);
				Thread.Sleep(2100);
				var exists = cli.ExistsAsync("Key").Result;
				Assert.AreEqual(0, exists);
			}
		}
		[TestMethod]
		public void PExpire()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var expR = cli.PExpireAsync("Key", 2000).Result;
				Assert.AreEqual(1, expR);
				Thread.Sleep(2100);
				var exists = cli.ExistsAsync("Key").Result;
				Assert.AreEqual(0, exists);
			}
		}
		[TestMethod]
		public void Pttl()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var expR = cli.PExpireAsync("Key", 2000).Result;
				Assert.AreEqual(1, expR);
				var ttl = cli.PttlAsync("Key").Result;
				Assert.IsTrue(ttl > 100 && ttl < 2100);
			}
		}
		[TestMethod]
		public void Persist()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var expR = cli.ExpireAsync("Key", 1).Result;
				Assert.AreEqual(1, expR);
				expR = cli.PersistAsync("Key").Result;
				Assert.AreEqual(1, expR);
				Thread.Sleep(1100);
				var exists = cli.ExistsAsync("Key").Result;
				Assert.AreEqual(1, exists);
			}
		}
		[TestMethod]
		public void ExpireAt()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var expR = cli.ExpireAtAsync("Key", DateTime.Now.AddSeconds(2)).Result;
				var ttl = cli.TtlAsync("Key").Result;
				Assert.IsTrue(ttl > 0);
			}
		}
		[TestMethod]
		public void RandomKey()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var key = cli.RandomKeyAsync().Result;
				Assert.AreEqual("Key", key);
			}
		}
		[TestMethod]
		public void RandomKey_NoKeys()
		{
			using (var cli = CreateClient())
			{
				var key = cli.RandomKeyAsync().Result;
				Assert.AreEqual(string.Empty, key);
			}
		}
		[TestMethod]
		public void Rename()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var res = cli.RenameAsync("Key", "Key2").Result;
				Assert.AreEqual("OK", res);
				var res2 = cli.ExistsAsync("Key2").Result;
				Assert.AreEqual(1, res2);
			}
		}
		[TestMethod]
		public void Get()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var result = cli.GetAsync("Key").Result;
				Assert.AreEqual("Value", GetString(result));
			}
		}
		[TestMethod]
		public void Append()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				cli.AppendAsync("Key", GetBytes("_appendix")).Wait();
				Assert.AreEqual("Value_appendix", GetString(cli.GetAsync("Key").Result));
			}
		}
		[TestMethod]
		public void Move()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				Assert.AreEqual(1, cli.MoveAsync("Key", 3).Result);
				Assert.AreEqual(0, cli.ExistsAsync("Key").Result);
				cli.SelectAsync(3).Wait();
				Assert.AreEqual(1, cli.ExistsAsync("Key").Result);
			}
		}
		[TestMethod]
		public void Object()
		{
			using (var cli = CreateClient())
			{
				cli.LPushAsync("mylist", GetBytes("Hello world"));
				Assert.AreEqual(1, cli.ObjectAsync(Subcommand.RefCount, "mylist").Result.AsInteger());
				Assert.AreEqual("ziplist", GetString(cli.ObjectAsync(Subcommand.Encoding, "mylist").Result.AsBulk()));
			}
		}

		[TestMethod]
		public void Sort()
		{
			using (var cli = CreateClient())
			{
				cli.SortAsync("mylist", limitCount: 2, limitOffset: 23, by: "byPattern",
					asc: false, alpha: true, destination: "dst", getPatterns: new[] { "getPattern" }).Wait();
			}
		}
		[TestMethod]
		public void IncrDecr()
		{
			using (var cli = CreateClient())
			{
				Assert.AreEqual(1, cli.IncrAsync("Key").Result);
				Assert.AreEqual(0, cli.DecrAsync("Key").Result);
			}
		}

		[TestMethod]
		public void IncrByDecrBy()
		{
			using (var cli = CreateClient())
			{
				Assert.AreEqual(3, cli.IncrByAsync("Key", 3).Result);
				Assert.AreEqual(0, cli.DecrByAsync("Key", 3).Result);
			}
		}
		[TestMethod]
		public void IncrByFloat()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("key", GetBytes("10.50")).Wait();
				Assert.AreEqual("10.6", GetString(cli.IncrByFloatAsync("key", 0.1).Result));
			}
		}
		[TestMethod]
		public void HIncrByFloat()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("key", "field", GetBytes("10.50")).Wait();
				Assert.AreEqual("10.6", GetString(cli.HIncrByFloatAsync("key", "field", 0.1).Result));
			}
		}

		[TestMethod]
		public void GetRange()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				Assert.AreEqual("alu", GetString(cli.GetRangeAsync("Key", 1, 3).Result));
			}
		}
		[TestMethod]
		public void GetSet()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				Assert.AreEqual("Value", GetString(cli.GetSetAsync("Key", GetBytes("NewValue")).Result));
			}
		}
		[TestMethod]
		public void MGet()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				cli.SetAsync("Key2", GetBytes("Value2")).Wait();
				var result = cli.MGetAsync("Key", "Key2").Result;
				Assert.AreEqual("Value", GetString(result[0]));
				Assert.AreEqual("Value2", GetString(result[1]));
			}
		}

		[TestMethod]
		public void MSet()
		{
			using (var cli = CreateClient())
			{
				var res = cli.MSetAsync(new MSetArgs("Key", GetBytes("Value")),
							  new MSetArgs("Key2", GetBytes("Value2"))).Result;
				Assert.AreEqual("OK", res);
				var result = cli.MGetAsync("Key", "Key2").Result;
				Assert.AreEqual("Value", GetString(result[0]));
				Assert.AreEqual("Value2", GetString(result[1]));
			}
		}

		[TestMethod]
		public void MSetNx()
		{
			using (var cli = CreateClient())
			{
				var res = cli.MSetNxAsync(new MSetArgs("Key", GetBytes("Value")),
							  new MSetArgs("Key2", GetBytes("Value2"))).Result;
				Assert.AreEqual(1, res);
				var result = cli.MGetAsync("Key", "Key2").Result;
				Assert.AreEqual("Value", GetString(result[0]));
				Assert.AreEqual("Value2", GetString(result[1]));
			}
		}
		[TestMethod]
		public void SetEx()
		{
			using (var cli = CreateClient())
			{
				cli.SetExAsync("Key", 2, GetBytes("Value")).Wait();
				Thread.Sleep(2500);
				var exists = cli.ExistsAsync("Key").Result;
				Assert.AreEqual(0, exists);
			}
		}
		[TestMethod]
		public void PSetEx()
		{
			using (var cli = CreateClient())
			{
				cli.PSetExAsync("Key", 2000, GetBytes("Value")).Wait();
				Thread.Sleep(2500);
				var exists = cli.ExistsAsync("Key").Result;
				Assert.AreEqual(0, exists);
			}
		}
		[TestMethod]
		public void SetNx()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				cli.SetNxAsync("Key", GetBytes("NewValue")).Wait();
				Assert.AreEqual("Value", GetString(cli.GetAsync("Key").Result));
			}
		}
		[TestMethod]
		public void SetRange()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				cli.SetRangeAsync("Key", 2, GetBytes("eul")).Wait();
				Assert.AreEqual("Vaeul", GetString(cli.GetAsync("Key").Result));
			}
		}
		[TestMethod]
		public void StrLen()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				Assert.AreEqual(5, cli.StrLenAsync("Key").Result);
			}
		}
		[TestMethod]
		public void HSet()
		{
			using (var cli = CreateClient())
			{
				Assert.AreEqual(1, cli.HSetAsync("Key", "Fld", GetBytes("Value")).Result);
			}
		}
		[TestMethod]
		public void HDel()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("Key", "Fld1", GetBytes("Val1")).Wait();
				cli.HSetAsync("Key", "Fld2", GetBytes("Val2")).Wait();
				Assert.AreEqual(2, cli.HDelAsync("Key", "Fld1", "Fld2").Result);
				Assert.AreEqual(0, cli.HExistsAsync("Key", "Fld1").Result);
				Assert.AreEqual(0, cli.HExistsAsync("Key", "Fld2").Result);
			}
		}
		[TestMethod]
		public void HExists()
		{
			using (var cli = CreateClient())
			{
				Assert.AreEqual(0, cli.HExistsAsync("Key", "Fld").Result);
				cli.HSetAsync("Key", "Fld", GetBytes("Value")).Wait();
				Assert.AreEqual(1, cli.HExistsAsync("Key", "Fld").Result);
			}
		}
		[TestMethod]
		public void HGet()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("Key", "Fld1", GetBytes("Val1")).Wait();
				Assert.AreEqual("Val1", GetString(cli.HGetAsync("Key", "Fld1").Result));
			}
		}
		[TestMethod]
		public void HGetAll()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("Key", "Fld1", GetBytes("Val1")).Wait();
				cli.HSetAsync("Key", "Fld2", GetBytes("Val2")).Wait();
				var result = cli.HGetAllAsync("Key").Result;
				Assert.AreEqual("Fld1", GetString(result[0]));
				Assert.AreEqual("Val1", GetString(result[1]));
				Assert.AreEqual("Fld2", GetString(result[2]));
				Assert.AreEqual("Val2", GetString(result[3]));
			}
		}
		[TestMethod]
		public void HIncrBy()
		{
			using (var cli = CreateClient())
			{
				Assert.AreEqual(23, cli.HIncrByAsync("Key", "Fld", 23).Result);
			}
		}
		[TestMethod]
		public void HKeys()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("Key", "Fld1", GetBytes("Val1")).Wait();
				cli.HSetAsync("Key", "Fld2", GetBytes("Val2")).Wait();
				var result = cli.HKeysAsync("Key").Result;
				Assert.AreEqual("Fld1", GetString(result[0]));
				Assert.AreEqual("Fld2", GetString(result[1]));
			}
		}
		[TestMethod]
		public void HVals()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("Key", "Fld1", GetBytes("Val1")).Wait();
				cli.HSetAsync("Key", "Fld2", GetBytes("Val2")).Wait();
				var result = cli.HValsAsync("Key").Result;
				Assert.AreEqual("Val1", GetString(result[0]));
				Assert.AreEqual("Val2", GetString(result[1]));
			}
		}
		[TestMethod]
		public void LPushBlPop()
		{
			using (var cli = CreateClient())
			{
				using (var cli2 = CreateClient())
				{
					var blpop = cli.BlPopAsync(10, "Key");
					cli2.LPushAsync("Key", GetBytes("Value")).Wait();
					var result = blpop.Result;
					Assert.AreEqual("Key", GetString(result[0]));
					Assert.AreEqual("Value", GetString(result[1]));
				}
			}
		}
		[TestMethod]
		public void LPushBlPop_NilReply()
		{
			using (var cli = CreateClient())
			{
				var blpop = cli.BlPopAsync(1, "Key");
				var result = blpop.Result;
				Assert.IsTrue(result.IsNull);
			}
		}
		[TestMethod]
		public void RPushBrPop()
		{
			using (var cli = CreateClient())
			{
				using (var cli2 = CreateClient())
				{
					var brpop = cli.BrPopAsync(10, "Key");
					cli2.RPushAsync("Key", GetBytes("Value")).Wait();
					var result = brpop.Result;
					Assert.AreEqual("Key", GetString(result[0]));
					Assert.AreEqual("Value", GetString(result[1]));
				}
			}
		}
		[TestMethod]
		public void RPop()
		{
			using (var cli = CreateClient())
			{
				cli.RPushAsync("Key", GetBytes("Value")).Wait();
				Assert.AreEqual("Value", GetString(cli.RPopAsync("Key").Result));
			}
		}
		[TestMethod]
		public void LPop()
		{
			using (var cli = CreateClient())
			{
				cli.LPushAsync("Key", GetBytes("Value")).Wait();
				Assert.AreEqual("Value", GetString(cli.LPopAsync("Key").Result));
			}
		}
		[TestMethod]
		public void HLen()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("Key", "Fld1", GetBytes("Val1")).Wait();
				cli.HSetAsync("Key", "Fld2", GetBytes("Val2")).Wait();
				Assert.AreEqual(2, cli.HLenAsync("Key").Result);
			}
		}
		[TestMethod]
		public void HMGet()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("Key", "Fld1", GetBytes("Val1")).Wait();
				cli.HSetAsync("Key", "Fld2", GetBytes("Val2")).Wait();
				var result = cli.HMGetAsync("Key", "Fld1", "Fld2").Result;
				Assert.AreEqual("Val1", GetString(result[0]));
				Assert.AreEqual("Val2", GetString(result[1]));
			}
		}
		[TestMethod]
		public void HMSet()
		{
			using (var cli = CreateClient())
			{
				cli.HMSetAsync("Key", new MSetArgs("Fld1", GetBytes("Val1")),
					new MSetArgs("Fld2", GetBytes("Val2"))).Wait();
				var result = cli.HMGetAsync("Key", "Fld1", "Fld2").Result;
				Assert.AreEqual("Val1", GetString(result[0]));
				Assert.AreEqual("Val2", GetString(result[1]));
			}
		}
		#region hyperloglog
		[TestMethod]
		public void PfAdd()
		{
			using (var cli = CreateClient())
			{
				var res = cli.PfAddAsync("hll", 1, 2, 3, 4, 5, 6, 7).Result;
				Assert.AreEqual(1, res);
			}
		}
		[TestMethod]
		public void PfCount()
		{
			using (var cli = CreateClient())
			{
				cli.PfAddAsync("hll", 1, 2, 3, 4, 5, 6, 7).Wait();
				var res = cli.PfCountAsync("hll").Result;
				Assert.AreEqual(7, res);
			}
		}
		[TestMethod]
		public void PfMerge()
		{
			using (var cli = CreateClient())
			{
				cli.PfAddAsync("hll1", "foo", "bar", "zap", "a").Wait();
				cli.PfAddAsync("hll2", "a", "b", "c", "foo").Wait();
				var mergeRes = cli.PfMergeAsync("hll3", "hll1", "hll2").Result;
				var countRes = cli.PfCountAsync("hll3").Result;
				Assert.AreEqual("OK", mergeRes);
				Assert.AreEqual(6, countRes);
			}
		}
		#endregion hyperloglog
		[TestMethod]
		public void RPopLPush()
		{
			using (var cli = CreateClient())
			{
				cli.RPushAsync("mylist", GetBytes("one")).Wait();
				cli.RPushAsync("mylist", GetBytes("two")).Wait();
				cli.RPushAsync("mylist", GetBytes("three")).Wait();
				Assert.AreEqual("three", GetString(cli.RPopLPushAsync("mylist", "mylist2").Result));
			}
		}
		[TestMethod]
		public void BRPopLPush()
		{
			using (var cli = CreateClient())
			{
				cli.RPushAsync("mylist", GetBytes("one")).Wait();
				cli.RPushAsync("mylist", GetBytes("two")).Wait();
				cli.RPushAsync("mylist", GetBytes("three")).Wait();
				Assert.AreEqual("three", GetString(cli.BRPopLPushAsync("mylist", "mylist2", 10).Result));
			}
		}
		[TestMethod]
		public void LIndex()
		{
			using (var cli = CreateClient())
			{
				cli.LPushAsync("Key", GetBytes("Value1")).Wait();
				cli.RPushAsync("Key", GetBytes("Value2")).Wait();
				Assert.AreEqual("Value2", GetString(cli.LIndexAsync("Key", 1).Result));
			}
		}
		[TestMethod]
		public void LInsert()
		{
			using (var cli = CreateClient())
			{
				cli.LPushAsync("Key", GetBytes("Value1")).Wait();
				cli.RPushAsync("Key", GetBytes("Value2")).Wait();
				cli.LInsertAsync("Key", GetBytes("Value2"), GetBytes("Value1.5")).Wait();

				Assert.AreEqual("Value1.5", GetString(cli.LIndexAsync("Key", 1).Result));
			}
		}
		[TestMethod]
		public void LLen()
		{
			using (var cli = CreateClient())
			{
				cli.LPushAsync("Key", GetBytes("Value1")).Wait();
				cli.RPushAsync("Key", GetBytes("Value2")).Wait();

				Assert.AreEqual(2, cli.LLenAsync("Key").Result);
			}
		}
		[TestMethod]
		public void LPushX()
		{
			using (var cli = CreateClient())
			{
				cli.LPushXAsync("Key", GetBytes("Value1")).Wait();


				Assert.AreEqual(0, cli.LLenAsync("Key").Result);
			}
		}
		[TestMethod]
		public void RPushX()
		{
			using (var cli = CreateClient())
			{
				cli.RPushXAsync("Key", GetBytes("Value1")).Wait();
				Assert.AreEqual(0, cli.LLenAsync("Key").Result);
			}
		}
		[TestMethod]
		public void LRange()
		{
			using (var cli = CreateClient())
			{
				cli.RPushAsync("Key", GetBytes("One")).Wait();
				cli.RPushAsync("Key", GetBytes("Two")).Wait();
				cli.RPushAsync("Key", GetBytes("Three")).Wait();

				var result = cli.LRangeAsync("Key", 0, 1).Result;
				Assert.AreEqual("One", GetString(result[0]));
				Assert.AreEqual("Two", GetString(result[1]));
			}
		}
		[TestMethod]
		public void LRem()
		{
			using (var cli = CreateClient())
			{
				cli.RPushAsync("Key", GetBytes("hello")).Wait();
				cli.RPushAsync("Key", GetBytes("hello")).Wait();
				cli.RPushAsync("Key", GetBytes("foo")).Wait();
				cli.RPushAsync("Key", GetBytes("hello")).Wait();

				Assert.AreEqual(2, cli.LRemAsync("Key", -2, GetBytes("hello")).Result);
				Assert.AreEqual(2, cli.LLenAsync("Key").Result);
			}
		}
		[TestMethod]
		public void LTrim()
		{
			using (var cli = CreateClient())
			{
				cli.RPushAsync("Key", GetBytes("one")).Wait();
				cli.RPushAsync("Key", GetBytes("two")).Wait();
				Assert.AreEqual("OK", cli.LTrimAsync("Key", 1, 1).Result);

				Assert.AreEqual(1, cli.LLenAsync("Key").Result);
			}
		}
		[TestMethod]
		public void SAdd()
		{
			using (var cli = CreateClient())
			{
				Assert.AreEqual(2, cli.SAddAsync("Key", GetBytes("Val1"), GetBytes("Val2")).Result);
			}
		}
		[TestMethod]
		public void SRem()
		{
			using (var cli = CreateClient())
			{
				Assert.AreEqual(2, cli.SAddAsync("Key", GetBytes("Val1"), GetBytes("Val2")).Result);
				Assert.AreEqual(1, cli.SRemAsync("Key", GetBytes("Val1")).Result);
				Assert.AreEqual(1, cli.SCardAsync("Key").Result);
			}
		}
		[TestMethod]
		public void SCard()
		{
			using (var cli = CreateClient())
			{
				cli.SAddAsync("Key", GetBytes("Val1"), GetBytes("Val2")).Wait();
				Assert.AreEqual(2, cli.SCardAsync("Key").Result);
			}
		}


		[TestMethod]
		public void SDiffStore()
		{
			using (var cli = CreateClient())
			{
				cli.SAddAsync("Key", GetBytes("a")).Wait();
				cli.SAddAsync("Key", GetBytes("b")).Wait();
				cli.SAddAsync("Key", GetBytes("c")).Wait();

				cli.SAddAsync("Key2", GetBytes("c")).Wait();
				cli.SAddAsync("Key2", GetBytes("d")).Wait();
				cli.SAddAsync("Key2", GetBytes("e")).Wait();

				Assert.AreEqual(2, cli.SDiffStoreAsync("New", "Key", "Key2").Result);
				Assert.AreEqual(2, cli.SCardAsync("New").Result);
			}
		}

		[TestMethod]
		public void SUnionStore()
		{
			using (var cli = CreateClient())
			{
				cli.SAddAsync("Key", GetBytes("a")).Wait();
				cli.SAddAsync("Key", GetBytes("b")).Wait();
				cli.SAddAsync("Key", GetBytes("c")).Wait();

				cli.SAddAsync("Key2", GetBytes("b")).Wait();
				cli.SAddAsync("Key2", GetBytes("c")).Wait();
				cli.SAddAsync("Key2", GetBytes("d")).Wait();

				var result = cli.SUnionStoreAsync("New", "Key", "Key2").Result;
				Assert.AreEqual(4, result);
			}
		}
		[TestMethod]
		public void SInter()
		{
			using (var cli = CreateClient())
			{
				cli.SAddAsync("Key", GetBytes("a")).Wait();
				cli.SAddAsync("Key", GetBytes("b")).Wait();
				cli.SAddAsync("Key", GetBytes("c")).Wait();

				cli.SAddAsync("Key2", GetBytes("c")).Wait();
				cli.SAddAsync("Key2", GetBytes("d")).Wait();
				cli.SAddAsync("Key2", GetBytes("e")).Wait();

				var result = cli.SInterAsync("Key", "Key2").Result;
				Assert.AreEqual("c", GetString(result[0]));
			}
		}
		[TestMethod]
		public void SInterStore()
		{
			using (var cli = CreateClient())
			{
				cli.SAddAsync("Key", GetBytes("a")).Wait();
				cli.SAddAsync("Key", GetBytes("b")).Wait();
				cli.SAddAsync("Key", GetBytes("c")).Wait();

				cli.SAddAsync("Key2", GetBytes("c")).Wait();
				cli.SAddAsync("Key2", GetBytes("d")).Wait();
				cli.SAddAsync("Key2", GetBytes("e")).Wait();

				Assert.AreEqual(1, cli.SInterStoreAsync("New", "Key", "Key2").Result);
				Assert.AreEqual(1, cli.SCardAsync("New").Result);
			}
		}
		[TestMethod]
		public void SIsMember()
		{
			using (var cli = CreateClient())
			{
				cli.SAddAsync("Key", GetBytes("a")).Wait();
				Assert.AreEqual(1, cli.SIsMemberAsync("Key", GetBytes("a")).Result);
			}
		}

		[TestMethod]
		public void SMove()
		{
			using (var cli = CreateClient())
			{
				cli.SAddAsync("Key", GetBytes("a")).Wait();
				Assert.AreEqual(1, cli.SMoveAsync("Key", "Key2", GetBytes("a")).Result);
				var result = cli.SMembersAsync("Key2").Result;
				Assert.AreEqual("a", GetString(result[0]));
			}
		}
		[TestMethod]
		public void SPop()
		{
			using (var cli = CreateClient())
			{
				cli.SAddAsync("Key", GetBytes("a")).Wait();
				Assert.AreEqual("a", GetString(cli.SPopAsync("Key").Result));

			}
		}
		[TestMethod]
		public void SRandMember()
		{
			using (var cli = CreateClient())
			{
				cli.SAddAsync("Key", GetBytes("a")).Wait();
				cli.SAddAsync("Key", GetBytes("b")).Wait();
				Assert.AreEqual(2, cli.SRandMemberAsync("Key", 2).Result.Length);
			}
		}
		[TestMethod]
		public void ZAdd()
		{
			using (var cli = CreateClient())
			{
				Assert.AreEqual(2, cli.ZAddAsync("Key", new ZAddArgs(100.2, GetBytes("Value")),
														new ZAddArgs(100, GetBytes("Value2"))).Result);
			}
		}
		[TestMethod]
		public void ZCard()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("Key", new ZAddArgs(100.2, GetBytes("Value")),
														new ZAddArgs(100, GetBytes("Value2"))).Wait();
				Assert.AreEqual(2, cli.ZCardAsync("Key").Result);
			}
		}
		[TestMethod]
		public void ZCount()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("Key", new ZAddArgs(100, GetBytes("Value")),
									 new ZAddArgs(200, GetBytes("Value2")),
									 new ZAddArgs(300, GetBytes("Value3"))).Wait();
				Assert.AreEqual(1, cli.ZCountAsync("Key", 150, 222).Result);
			}
		}
		[TestMethod]
		public void ZCountInf()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("Key", new ZAddArgs(100.0, GetBytes("Value")),
									 new ZAddArgs(200, GetBytes("Value2")),
									 new ZAddArgs(300, GetBytes("Value3"))).Wait();
				Assert.AreEqual(2, cli.ZCountAsync("Key", double.NegativeInfinity, 222).Result);
			}
		}

		[TestMethod]
		public void ZIncrBy()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("Key", new ZAddArgs(100.0, GetBytes("Value"))).Wait();
				Assert.AreEqual(110.3, cli.ZIncrByAsync("Key", 10.3, GetBytes("Value")).Result);
			}
		}
		[TestMethod]
		public void ZInterStore()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.AreEqual(2, cli.ZInterStoreAsync("out", "zset1", "zset2").Result);
			}
		}
		[TestMethod]
		public void ZInterStore_Min()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.AreEqual(2, cli.ZInterStoreAsync("out", Aggregation.Min, "zset1", "zset2").Result);
			}
		}
		[TestMethod]
		public void ZInterStore_WithWeights()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.AreEqual(2, cli.ZInterStoreAsync("out", new ZAggrStoreArgs("zset1", 2),
														 new ZAggrStoreArgs("zset2", 3)).Result);
			}
		}
		[TestMethod]
		public void ZInterStore_WithWeights_Sum()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.AreEqual(2, cli.ZInterStoreAsync("out", Aggregation.Sum,
													new ZAggrStoreArgs("zset1", 2),
													new ZAggrStoreArgs("zset2", 3)).Result);
			}
		}
		[TestMethod]
		public void ZRange()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();

				var result = cli.ZRangeAsync("zset1", 1, 2).Result;

				Assert.AreEqual(2, result.Length);
				Assert.AreEqual("two", GetString(result[0]));
				Assert.AreEqual("three", GetString(result[1]));
			}
		}
		[TestMethod]
		public void ZRange_WithScores()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();

				var result = cli.ZRangeAsync("zset1", 1, 2, true).Result;

				Assert.AreEqual(4, result.Length);

				Assert.AreEqual("two", GetString(result[0]));
				Assert.AreEqual("2", GetString(result[1]));

				Assert.AreEqual("three", GetString(result[2]));
				Assert.AreEqual("3", GetString(result[3]));
			}
		}
		[TestMethod]
		public void ZRangeByScore()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();


				var result = cli.ZRangeByScoreAsync("zset1", double.NegativeInfinity, 2).Result;

				Assert.AreEqual(2, result.Length);

				Assert.AreEqual("one", GetString(result[0]));
				Assert.AreEqual("two", GetString(result[1]));
			}
		}
		[TestMethod]
		public void ZRangeByScore_WithScores()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();


				var result = cli.ZRangeByScoreAsync("zset1", double.NegativeInfinity, 2, true).Result;

				Assert.AreEqual(4, result.Length);

				Assert.AreEqual("1", GetString(result[1]));
				Assert.AreEqual("2", GetString(result[3]));

				Assert.AreEqual("one", GetString(result[0]));
				Assert.AreEqual("two", GetString(result[2]));
			}
		}
		[TestMethod]
		public void ZRangeByScore_WithLimits()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();

				var result = cli.ZRangeByScoreAsync("zset1", double.NegativeInfinity, 2, 1, 1).Result;

				Assert.AreEqual(1, result.Length);

				Assert.AreEqual("two", GetString(result[0]));
			}
		}
		[TestMethod]
		public void ZRangeByScore_WithLimits_WithScores()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();

				var result = cli.ZRangeByScoreAsync("zset1", double.NegativeInfinity, 2, 1, 1, true).Result;

				Assert.AreEqual(2, result.Length);

				Assert.AreEqual("two", GetString(result[0]));
				Assert.AreEqual("2", GetString(result[1]));
			}
		}
		[TestMethod]
		public void ZRank()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();

				Assert.AreEqual(1, cli.ZRankAsync("zset1", GetBytes("three")).Result.Value);
			}
		}
		[TestMethod]
		public void ZRevRank()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();

				Assert.AreEqual(0, cli.ZRevRankAsync("zset1", GetBytes("three")).Result.Value);
			}
		}
		[TestMethod]
		public void ZRank_NullReply()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();

				Assert.AreEqual(null, cli.ZRankAsync("zset1", GetBytes("four")).Result);
			}
		}
		[TestMethod]
		public void ZRem()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(5, GetBytes("five"))).Wait();
				Assert.AreEqual(2, cli.ZRemAsync("zset1", GetBytes("three"), GetBytes("one")).Result);
				Assert.AreEqual(1, cli.ZCardAsync("zset1").Result);
			}
		}
		[TestMethod]
		public void ZRemRangeByRank()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(5, GetBytes("five"))).Wait();
				Assert.AreEqual(1, cli.ZRemRangeByRankAsync("zset1", 2, 4).Result);
				Assert.AreEqual(2, cli.ZCardAsync("zset1").Result);
			}
		}
		[TestMethod]
		public void ZRemRangeByScore()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(5, GetBytes("five"))).Wait();
				Assert.AreEqual(2, cli.ZRemRangeByScoreAsync("zset1", 2, double.PositiveInfinity).Result);
				Assert.AreEqual(1, cli.ZCardAsync("zset1").Result);
			}
		}
		[TestMethod]
		public void ZRevRange()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", 1, GetBytes("one")).Wait();
				cli.ZAddAsync("zset1", 2, GetBytes("two")).Wait();
				cli.ZAddAsync("zset1", 3, GetBytes("three")).Wait();

				var result = cli.ZRevRangeAsync("zset1", 0, -1).Result;
				Assert.AreEqual(3, result.Length);
				Assert.AreEqual("three", GetString(result[0]));
				Assert.AreEqual("two", GetString(result[1]));
				Assert.AreEqual("one", GetString(result[2]));
			}
		}
		[TestMethod]
		public void ZRevRange_WithScores()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", 1, GetBytes("one")).Wait();
				cli.ZAddAsync("zset1", 2, GetBytes("two")).Wait();
				cli.ZAddAsync("zset1", 3, GetBytes("three")).Wait();

				var result = cli.ZRevRangeAsync("zset1", 0, -1, true).Result;
				Assert.AreEqual(6, result.Length);
			}
		}
		[TestMethod]
		public void ZRevRangeByScore()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("s1", 3, GetBytes("three")).Wait();
				cli.ZAddAsync("s1", 1, GetBytes("one")).Wait();
				cli.ZAddAsync("s1", 2, GetBytes("two")).Wait();


				var result = cli.ZRevRangeByScoreAsync("s1", double.PositiveInfinity, double.NegativeInfinity).Result;

				Assert.AreEqual(3, result.Length);

				Assert.AreEqual("one", GetString(result[2]));
				Assert.AreEqual("two", GetString(result[1]));
				Assert.AreEqual("three", GetString(result[0]));
			}
		}
		[TestMethod]
		public void ZRevRangeByScore_WithScores()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();


				var result = cli.ZRevRangeByScoreAsync("zset1", double.PositiveInfinity, double.NegativeInfinity, true).Result;

				Assert.AreEqual(6, result.Length);
			}
		}
		[TestMethod]
		public void ZRevRangeByScore_WithLimits()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();

				var result = cli.ZRevRangeByScoreAsync("zset1", 3, 0, 1, 1).Result;

				Assert.AreEqual(1, result.Length);

				Assert.AreEqual("two", GetString(result[0]));
			}
		}
		[TestMethod]
		public void ZRevRangeByScore_WithLimits_WithScores()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();

				var result = cli.ZRevRangeByScoreAsync("zset1", 3, 0, 1, 1, true).Result;

				Assert.AreEqual(2, result.Length);

				Assert.AreEqual("two", GetString(result[0]));
				Assert.AreEqual("2", GetString(result[1]));
			}
		}
		[TestMethod]
		public void ZScore()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();

				Assert.AreEqual(3, cli.ZScoreAsync("zset1", GetBytes("three")).Result);
			}
		}
		[TestMethod]
		public void ZUnionStore()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.AreEqual(3, cli.ZUnionStoreAsync("out", "zset1", "zset2").Result);
			}
		}
		[TestMethod]
		public void ZUnionStore_Min()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.AreEqual(3, cli.ZUnionStoreAsync("out", Aggregation.Min, "zset1", "zset2").Result);
			}
		}
		[TestMethod]
		public void ZUnionStore_WithWeights()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.AreEqual(3, cli.ZUnionStoreAsync("out", new ZAggrStoreArgs("zset1", 2),
														 new ZAggrStoreArgs("zset2", 3)).Result);
			}
		}
		[TestMethod]
		public void ZUnionStore_WithWeights_Sum()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.AreEqual(3, cli.ZUnionStoreAsync("out", Aggregation.Sum,
													new ZAggrStoreArgs("zset1", 2),
													new ZAggrStoreArgs("zset2", 3)).Result);
			}
		}
		#region pubsub
		[TestMethod]
		public void Subscribe()
		{
			using (var cli1 = CreateClient())
			{
				var c1 = cli1.SubscribeAsync("channel").Result;
				var subResult = c1.ReadMessageAsync().Result;
				Assert.AreEqual(ChannelMessageType.Subscribe, subResult.MessageType);
				Assert.AreEqual("channel", subResult.Channels[0]);
				Assert.AreEqual("1", GetString(subResult.Value));
			}
		}
		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void Subscribe_WithFilter_ReadMessagesAfterChannelClosing()
		{
			using (var cli1 = CreateClient())
			{
				var c1 = cli1.SubscribeAsync("channel").Result;
				c1.ReadMessageAsync().Wait();//read subscribe message
				var readTask = c1.ReadMessageAsync(ChannelMessageType.Message);
				c1.Dispose();
				readTask.Wait();
			}
		}
		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void Subscribe_NoFilter_ReadMessagesAfterChannelClosing()
		{
			using (var cli1 = CreateClient())
			{
				var c1 = cli1.SubscribeAsync("channel").Result;
				c1.ReadMessageAsync().Wait();//read subscribe message
				var readTask = c1.ReadMessageAsync();
				c1.Dispose();
				var subResult = readTask.Result;
				Assert.AreEqual(ChannelMessageType.Unknown, subResult.MessageType);
				Assert.AreEqual(ResponseType.Error, subResult.Value.ResponseType);
			}
		}
		[TestMethod]
		public void Unsubscribe()
		{
			using (var cli1 = CreateClient())
			{
				var subClient = cli1.SubscribeAsync("channel").Result;
				subClient.ReadMessageAsync().Wait();
				subClient.UnsubscribeAsync("channel").Wait();

				var subResult = subClient.ReadMessageAsync().Result;

				Assert.AreEqual(ChannelMessageType.Unsubscribe, subResult.MessageType);
				Assert.AreEqual("channel", subResult.Channels[0]);
				Assert.AreEqual("0", GetString(subResult.Value));
			}
		}

		[TestMethod]
		public void Publish()
		{
			using (var cli1 = CreateClient())
			{
				Assert.AreEqual(0, cli1.PublishAsync("channel", GetBytes("Message")).Result);
			}
		}

		[TestMethod]
		public void Publish_Subscribe_WithFilter()
		{
			using (var subscriber = CreateClient().SubscribeAsync("channel").Result)
			{
				using (var publisher = CreateClient())
				{
					publisher.PublishAsync("channel", GetBytes("Message")).Wait();

					var channelMessage = subscriber.ReadMessageAsync(ChannelMessageType.Message |
																ChannelMessageType.PMessage).Result;

					Assert.AreEqual(ChannelMessageType.Message, channelMessage.MessageType);
					Assert.AreEqual("channel", channelMessage.Channels[0]);
					Assert.AreEqual("Message", GetString(channelMessage.Value));
				}
			}
		}
		[TestMethod]
		public void Publish_Subscribe_TimeoutEmulation()
		{
			using (var cli1 = CreateClient())
			{
				var c1 = cli1.SubscribeAsync("channel").Result;

				var readAsync = c1.ReadMessageAsync(ChannelMessageType.Message | ChannelMessageType.Unsubscribe);

				Thread.Sleep(1000);
				c1.UnsubscribeAsync("channel").Wait();
				var result = readAsync.Result;
				Assert.AreEqual(ChannelMessageType.Unsubscribe, result.MessageType);
			}
		}
		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void Subscribe_Quit_WithFilter_ExceptionExpected()
		{
			using (var cli1 = CreateClient().SubscribeAsync("channel").Result)
			{
				cli1.QuitAsync().Wait();
				var result = cli1.ReadMessageAsync(ChannelMessageType.Message).Result;
				Assert.AreEqual(ChannelMessageType.Unknown, result.MessageType);
				Assert.AreEqual(ResponseType.Status, result.Value.ResponseType);
			}
		}

		[TestMethod]
		public void Subscribe_Quit_NoFilter_ValidResponse()
		{
			using (var cli1 = CreateClient().SubscribeAsync("channel").Result)
			{
				cli1.ReadMessageAsync().Wait();//read subscribe message;
				cli1.QuitAsync().Wait();
				var result = cli1.ReadMessageAsync().Result;
				Assert.AreEqual(ChannelMessageType.Unknown, result.MessageType);
				Assert.AreEqual(ResponseType.Status, result.Value.ResponseType);
			}
		}
		#endregion

		[TestMethod]
		public void Transactions()
		{
			using (var cli1 = CreateClient())
			{
				Assert.AreEqual("OK", cli1.MultiAsync().Result);
				Assert.AreEqual(0, cli1.IncrAsync("foo").Result);
				Assert.AreEqual(0, cli1.IncrAsync("bar").Result);
				var result = cli1.ExecAsync().Result;
				Assert.AreEqual("1", GetString(result[0]));
				Assert.AreEqual("1", GetString(result[1]));
			}
		}
		[TestMethod]
		public void Eval()
		{
			using (var cli = CreateClient())
			{
				var result = cli.EvalAsync("return {KEYS[1],KEYS[2],ARGV[1],ARGV[2]}",
											new[] { "key1", "key2" }, GetBytes("first"), GetBytes("second")).Result;

				Assert.AreEqual(ResponseType.MultiBulk, result.ResponseType);
				var mb = result.AsMultiBulk();
				Assert.AreEqual("key1", GetString(mb[0].AsBulk()));
				Assert.AreEqual("key2", GetString(mb[1].AsBulk()));
				Assert.AreEqual("first", GetString(mb[2].AsBulk()));
				Assert.AreEqual("second", GetString(mb[3].AsBulk()));
			}
		}
		[TestMethod]
		public void ScriptLoadSha()
		{
			using (var cli = CreateClient())
			{
				var sha1 = cli.ScriptLoadAsync("return {KEYS[1],KEYS[2],ARGV[1],ARGV[2]}").Result;

				var result = cli.EvalShaAsync(sha1, new[] { "key1", "key2" }, GetBytes("first"), GetBytes("second")).Result;
				Assert.AreEqual(ResponseType.MultiBulk, result.ResponseType);
				var mb = result.AsMultiBulk();
				Assert.AreEqual("key1", GetString(mb[0].AsBulk()));
				Assert.AreEqual("key2", GetString(mb[1].AsBulk()));
				Assert.AreEqual("first", GetString(mb[2].AsBulk()));
				Assert.AreEqual("second", GetString(mb[3].AsBulk()));
			}
		}
		[TestMethod]
		public void ScriptExists()
		{
			using (var cli = CreateClient())
			{
				var sha1 = cli.ScriptLoadAsync("return {KEYS[1],KEYS[2],ARGV[1],ARGV[2]}").Result;

				var result = cli.ScriptExistsAsync(sha1).Result;
				Assert.AreEqual(1, result.Length);
				Assert.AreEqual("1", GetString(result[0]));
			}
		}
		[TestMethod]
		public void ScriptFlushExists()
		{
			using (var cli = CreateClient())
			{
				var sha1 = cli.ScriptLoadAsync("return {KEYS[1],KEYS[2],ARGV[1],ARGV[2]}").Result;
				cli.ScriptFlushAsync().Wait();
				var result = cli.ScriptExistsAsync(sha1).Result;
				Assert.AreEqual(1, result.Length);
				Assert.AreEqual("0", GetString(result[0]));
			}
		}
		[TestMethod]
		public void Echo()
		{
			using (var cli = CreateClient())
			{
				var resp = cli.EchoAsync(GetBytes("Message")).Result;
				Assert.AreEqual("Message", GetString(resp));
			}
		}
		[TestMethod]
		public void Ping()
		{
			using (var cli = CreateClient())
			{
				Assert.AreEqual("PONG", cli.PingAsync().Result);
			}
		}
		[TestMethod]
		public void Quit()
		{
			using (var cli = CreateClient())
			{
				Assert.AreEqual("OK", cli.QuitAsync().Result);
			}
		}

		[TestMethod]
		public void ClientList()
		{
			using (var cli = CreateClient())
			{
				var result = GetString(cli.ClientListAsync().Result);
				Assert.IsFalse(string.IsNullOrEmpty(result));
			}
		}

		[TestMethod]
		public void DbSize()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("key", GetBytes("value")).Wait();
				Assert.AreEqual(1, cli.DbSizeAsync().Result);
			}
		}
		[TestMethod]
		public void FlushAll()
		{
			using (var cli = CreateClient())
			{
				Assert.AreEqual("OK", cli.FlushAllAsync().Result);
			}
		}
		[TestMethod]
		public void Info()
		{
			using (var cli = CreateClient())
			{
				var result = GetString(cli.InfoAsync().Result);
                Assert.IsFalse(string.IsNullOrEmpty(result));
            }
		}
		[TestMethod]
		public void ConfigGet()
		{
			using (var cli = CreateClient())
			{
				var result = cli.ConfigGetAsync("loglevel").Result;
			}
		}
		[TestMethod]
		public void LastSave()
		{
			using (var cli = CreateClient())
			{
				Assert.IsTrue(cli.LastSaveAsync().Result > 0);
			}
		}
		[TestMethod]
		public void Time()
		{
			using (var cli = CreateClient())
			{
				var result = cli.TimeAsync().Result;
			}
		}
		[TestMethod]
		public void ResetStat()
		{
			using (var cli = CreateClient())
			{
				var result = cli.ConfigResetStatAsync().Result;
			}
		}
		[TestMethod]
		public void ClientsPool()
		{
			using (var pool = RedisClient.CreateClientsPool())
			{
				IRedisClient cli1, cli2;
				using (cli1 = pool.CreateClientAsync(ConnectionString).Result)
				{
					cli1.SetAsync("Key", GetBytes("Value")).Wait();
				}
				using (cli2 = pool.CreateClientAsync(ConnectionString).Result)
				{
					cli2.GetAsync("Key").Wait();
				}
				Assert.AreEqual(cli1, cli2);
			}
		}

		[TestMethod]
		public void PoolTest_Timeout()
		{
			using (var pool = RedisClient.CreateClientsPool(100))
			{
				IRedisClient cli1;
				IRedisClient cli2;
				using (cli1 = pool.CreateClientAsync(connectionString: ConnectionString).Result)
				{
					cli1.SetAsync("Key", GetBytes("Value")).Wait();
				}

				Thread.Sleep(1000);
				using (cli2 = pool.CreateClientAsync(ConnectionString).Result)
				{
					cli2.GetAsync("Key").Wait();
				}
				Assert.AreNotEqual(cli1, cli2);
			}
		}
		[TestMethod]
		public void ClosePipline()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var sub = cli.SubscribeAsync("channel").Result;
				try
				{
					var res = cli.GetAsync("Key").Result;
				}
				catch (AggregateException ex)
				{
					Assert.AreEqual(typeof(RedisException), ex.InnerException.GetType());
					Assert.AreEqual("Pipeline is in OneWay mode", ex.InnerException.Message);
				}

			}
		}
		[TestMethod]
		public void PipelineTest()
		{
			const int size = 10000;
			using (var cli = CreateClient())
			{
				var tasks = new List<Task<MultiBulk>>();

				for (int i = 0; i < size; i++)
				{
					cli.SetAsync("Key" + i, GetBytes("Value" + i));
					cli.SetAsync("Key_" + i, GetBytes("Value" + i));
					tasks.Add(cli.MGetAsync("Key" + i, "Key_" + i));
				}
				// some other work here...
				//...
				for (int i = 0; i < size; i++)
				{
					Assert.AreEqual("Value" + i, GetString(tasks[i].Result[0]));
					Assert.AreEqual("Value" + i, GetString(tasks[i].Result[1]));
				}
			}
		}
		[TestMethod]
		public void PipelineTest_PipelineIsClosed()
		{
			using (var cli = CreateClient())
			{
				var tasks = new List<Task<Bulk>>();

				for (int i = 0; i < 10000; i++)
				{
					cli.SetAsync("Key" + i, GetBytes("Value" + i));
					tasks.Add(cli.GetAsync("Key" + i));
					if (i == 4999)
						cli.SubscribeAsync("channel").Wait();
				}

				for (int i = 0; i < 5000; i++)
				{
					SpinWait.SpinUntil(() => tasks[i].IsCompleted);
					Assert.IsFalse(tasks[i].IsFaulted);
					Assert.AreEqual("Value" + i, GetString(tasks[i].Result));
				}
				for (int i = 5000; i < 10000; i++)
				{
					SpinWait.SpinUntil(() => tasks[i].IsCompleted);
					Assert.IsTrue(tasks[i].IsFaulted);
				}
			}
		}
		private int _requestId = 0;
	    private IConfigurationRoot _configuration;

	    [TestMethod]
		public void PipelineTest_ParallelPipelining()
		{
			_requestId = 0;
			using (var cli = CreateClient())
			{

				var tasksDic = new ConcurrentDictionary<Guid, Task<Bulk>>();
				var tasks = new List<Task>();
				for (int i = 0; i < 10; i++)
				{
					var t = Task.Run(() =>
						{
							for (int j = 0; j < 5000; j++)
							{
								var id = Guid.NewGuid();
								var keyId = Interlocked.Increment(ref _requestId);
								cli.SetAsync("Key" + keyId, GetBytes("Value" + id));
								tasksDic.TryAdd(id, cli.GetAsync("Key" + keyId));
							}
						});
					tasks.Add(t);
				}
				Task.WaitAll(tasks.ToArray());

				foreach (var item in tasksDic)
				{
					Assert.AreEqual("Value" + item.Key, GetString(item.Value.Result));
				}
			}
		}
		[TestMethod]
		public void Ctor_WithHostPort()
		{
			var sb = new RedisConnectionStringBuilder(ConnectionString);
			using (var cli = RedisClient.ConnectAsync(((IPEndPoint)sb.EndPoint).Address.ToString(), ((IPEndPoint)sb.EndPoint).Port, 3).Result)
			{
				Assert.AreEqual("PONG", cli.PingAsync().Result);
			}
		}
		[TestMethod]
		public void PubSubChannels()
		{
			Assert.Fail("waiting for Redis 2.8 released");
		}
		[TestMethod]
		public void PubSubNumSub()
		{
			Assert.Fail("waiting for Redis 2.8 released");
		}
		[TestMethod]
		public void PubSubNumPat()
		{
			Assert.Fail("waiting for Redis 2.8 released");
		}
		private static byte[] GetBytes(string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}
		private static string GetString(byte[] data)
		{
			return Encoding.UTF8.GetString(data);
		}

	    private string ConnectionString => _configuration.GetSection("ConnectionStrings")["Redis"];

        private IRedisClient CreateClient()
		{
			var cli = RedisClient.ConnectAsync(ConnectionString).Result;
			cli.FlushDbAsync().Wait();
			return cli;
		}
	}
}


