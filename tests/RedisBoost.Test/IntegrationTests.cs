using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RedisBoost;

namespace RedisBoost.Test
{
	public class IntegrationTests
	{
		[Fact]
		public void FlushDb()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				cli.FlushDbAsync().Wait();
				var result = cli.KeysAsync("*").Result;
				Assert.Equal(0, result.Length);
			}
		}
		[Fact]
		public void Set()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
			}
		}
		[Fact]
		public void Del()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value adfadf")).Wait();
				cli.DelAsync("Key").Wait();

				var result = cli.KeysAsync("*").Result;
				Assert.Equal(0, result.Length);
			}
		}
		[Fact]
		public void BitOp()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("key1", GetBytes("foobar")).Wait();
				cli.SetAsync("key2", GetBytes("abcdef")).Wait();
				cli.BitOpAsync(BitOpType.And, "dst", "key1", "key2").Wait();
				Assert.Equal("`bc`ab", GetString(cli.GetAsync("dst").Result));
			}
		}
		[Fact]
		public void GetSetBit()
		{
			using (var cli = CreateClient())
			{
				Assert.Equal(0, cli.SetBitAsync("key", 7, 1).Result);
				Assert.Equal(0, cli.GetBitAsync("key", 0).Result);
				Assert.Equal(1, cli.GetBitAsync("key", 7).Result);
				Assert.Equal(0, cli.GetBitAsync("key", 100).Result);
			}
		}
		[Fact]
		public void GetBit()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("key1", GetBytes("foobar")).Wait();
				cli.SetAsync("key2", GetBytes("abcdef")).Wait();
				cli.BitOpAsync(BitOpType.And, "dst", "key1", "key2").Wait();
				Assert.Equal("`bc`ab", GetString(cli.GetAsync("dst").Result));
			}
		}
		[Fact]
		public void Dump()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var result = cli.DumpAsync("Key").Result;
				Assert.Equal(17, result.Length);
			}
		}
		[Fact]
		public void Restore()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var result = cli.DumpAsync("Key").Result;
				cli.FlushDbAsync().Wait();

				var status = cli.RestoreAsync("Key", 0, result).Result;
				Assert.Equal("OK", status);
				Assert.Equal(1, cli.ExistsAsync("Key").Result);
			}
		}
		[Fact]
		public void Type()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				Assert.Equal("string", cli.TypeAsync("Key").Result);
			}
		}
		[Fact]
		public void Type_NoKey()
		{
			using (var cli = CreateClient())
			{
				Assert.Equal("none", cli.TypeAsync("Key").Result);
			}
		}
		[Fact]
		public void Exists()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var result = cli.ExistsAsync("Key").Result;
				Assert.Equal(1, result);
			}
		}
		[Fact]
		public void Expire()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var expR = cli.ExpireAsync("Key", 2).Result;
				Assert.Equal(1, expR);
				Thread.Sleep(2100);
				var exists = cli.ExistsAsync("Key").Result;
				Assert.Equal(0, exists);
			}
		}
		[Fact]
		public void PExpire()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var expR = cli.PExpireAsync("Key", 2000).Result;
				Assert.Equal(1, expR);
				Thread.Sleep(2100);
				var exists = cli.ExistsAsync("Key").Result;
				Assert.Equal(0, exists);
			}
		}
		[Fact]
		public void Pttl()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var expR = cli.PExpireAsync("Key", 2000).Result;
				Assert.Equal(1, expR);
				var ttl = cli.PttlAsync("Key").Result;
				Assert.True(ttl > 100 && ttl < 2100);
			}
		}
		[Fact]
		public void Persist()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var expR = cli.ExpireAsync("Key", 1).Result;
				Assert.Equal(1, expR);
				expR = cli.PersistAsync("Key").Result;
				Assert.Equal(1, expR);
				Thread.Sleep(1100);
				var exists = cli.ExistsAsync("Key").Result;
				Assert.Equal(1, exists);
			}
		}
		[Fact]
		public void ExpireAt()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var expR = cli.ExpireAtAsync("Key", DateTime.Now.AddSeconds(2)).Result;
				var ttl = cli.TtlAsync("Key").Result;
				Assert.True(ttl > 0);
			}
		}
		[Fact]
		public void RandomKey()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var key = cli.RandomKeyAsync().Result;
				Assert.Equal("Key", key);
			}
		}
		[Fact]
		public void RandomKey_NoKeys()
		{
			using (var cli = CreateClient())
			{
				var key = cli.RandomKeyAsync().Result;
				Assert.Equal(string.Empty, key);
			}
		}
		[Fact]
		public void Rename()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var res = cli.RenameAsync("Key", "Key2").Result;
				Assert.Equal("OK", res);
				var res2 = cli.ExistsAsync("Key2").Result;
				Assert.Equal(1, res2);
			}
		}
		[Fact]
		public void Get()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				var result = cli.GetAsync("Key").Result;
				Assert.Equal("Value", GetString(result));
			}
		}
		[Fact]
		public void Append()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				cli.AppendAsync("Key", GetBytes("_appendix")).Wait();
				Assert.Equal("Value_appendix", GetString(cli.GetAsync("Key").Result));
			}
		}
		[Fact]
		public void Move()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				Assert.Equal(1, cli.MoveAsync("Key", 3).Result);
				Assert.Equal(0, cli.ExistsAsync("Key").Result);
				cli.SelectAsync(3).Wait();
				Assert.Equal(1, cli.ExistsAsync("Key").Result);
			}
		}
		[Fact]
		public void Object()
		{
			using (var cli = CreateClient())
			{
				cli.LPushAsync("mylist", GetBytes("Hello world"));
				Assert.Equal(1, cli.ObjectAsync(Subcommand.RefCount, "mylist").Result.AsInteger());
				Assert.Equal("ziplist", GetString(cli.ObjectAsync(Subcommand.Encoding, "mylist").Result.AsBulk()));
			}
		}

		[Fact]
		public void Sort()
		{
			using (var cli = CreateClient())
			{
				cli.SortAsync("mylist", limitCount: 2, limitOffset: 23, by: "byPattern",
					asc: false, alpha: true, destination: "dst", getPatterns: new[] { "getPattern" }).Wait();
			}
		}
		[Fact]
		public void IncrDecr()
		{
			using (var cli = CreateClient())
			{
				Assert.Equal(1, cli.IncrAsync("Key").Result);
				Assert.Equal(0, cli.DecrAsync("Key").Result);
			}
		}

		[Fact]
		public void IncrByDecrBy()
		{
			using (var cli = CreateClient())
			{
				Assert.Equal(3, cli.IncrByAsync("Key", 3).Result);
				Assert.Equal(0, cli.DecrByAsync("Key", 3).Result);
			}
		}
		[Fact]
		public void IncrByFloat()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("key", GetBytes("10.50")).Wait();
				Assert.Equal("10.6", GetString(cli.IncrByFloatAsync("key", 0.1).Result));
			}
		}
		[Fact]
		public void HIncrByFloat()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("key", "field", GetBytes("10.50")).Wait();
				Assert.Equal("10.6", GetString(cli.HIncrByFloatAsync("key", "field", 0.1).Result));
			}
		}

		[Fact]
		public void GetRange()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				Assert.Equal("alu", GetString(cli.GetRangeAsync("Key", 1, 3).Result));
			}
		}
		[Fact]
		public void GetSet()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				Assert.Equal("Value", GetString(cli.GetSetAsync("Key", GetBytes("NewValue")).Result));
			}
		}
		[Fact]
		public void MGet()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				cli.SetAsync("Key2", GetBytes("Value2")).Wait();
				var result = cli.MGetAsync("Key", "Key2").Result;
				Assert.Equal("Value", GetString(result[0]));
				Assert.Equal("Value2", GetString(result[1]));
			}
		}

		[Fact]
		public void MSet()
		{
			using (var cli = CreateClient())
			{
				var res = cli.MSetAsync(new MSetArgs("Key", GetBytes("Value")),
							  new MSetArgs("Key2", GetBytes("Value2"))).Result;
				Assert.Equal("OK", res);
				var result = cli.MGetAsync("Key", "Key2").Result;
				Assert.Equal("Value", GetString(result[0]));
				Assert.Equal("Value2", GetString(result[1]));
			}
		}

		[Fact]
		public void MSetNx()
		{
			using (var cli = CreateClient())
			{
				var res = cli.MSetNxAsync(new MSetArgs("Key", GetBytes("Value")),
							  new MSetArgs("Key2", GetBytes("Value2"))).Result;
				Assert.Equal(1, res);
				var result = cli.MGetAsync("Key", "Key2").Result;
				Assert.Equal("Value", GetString(result[0]));
				Assert.Equal("Value2", GetString(result[1]));
			}
		}
		[Fact]
		public void SetEx()
		{
			using (var cli = CreateClient())
			{
				cli.SetExAsync("Key", 2, GetBytes("Value")).Wait();
				Thread.Sleep(2500);
				var exists = cli.ExistsAsync("Key").Result;
				Assert.Equal(0, exists);
			}
		}
		[Fact]
		public void PSetEx()
		{
			using (var cli = CreateClient())
			{
				cli.PSetExAsync("Key", 2000, GetBytes("Value")).Wait();
				Thread.Sleep(2500);
				var exists = cli.ExistsAsync("Key").Result;
				Assert.Equal(0, exists);
			}
		}
		[Fact]
		public void SetNx()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				cli.SetNxAsync("Key", GetBytes("NewValue")).Wait();
				Assert.Equal("Value", GetString(cli.GetAsync("Key").Result));
			}
		}
		[Fact]
		public void SetRange()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				cli.SetRangeAsync("Key", 2, GetBytes("eul")).Wait();
				Assert.Equal("Vaeul", GetString(cli.GetAsync("Key").Result));
			}
		}
		[Fact]
		public void StrLen()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", GetBytes("Value")).Wait();
				Assert.Equal(5, cli.StrLenAsync("Key").Result);
			}
		}
		[Fact]
		public void HSet()
		{
			using (var cli = CreateClient())
			{
				Assert.Equal(1, cli.HSetAsync("Key", "Fld", GetBytes("Value")).Result);
			}
		}
		[Fact]
		public void HDel()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("Key", "Fld1", GetBytes("Val1")).Wait();
				cli.HSetAsync("Key", "Fld2", GetBytes("Val2")).Wait();
				Assert.Equal(2, cli.HDelAsync("Key", "Fld1", "Fld2").Result);
				Assert.Equal(0, cli.HExistsAsync("Key", "Fld1").Result);
				Assert.Equal(0, cli.HExistsAsync("Key", "Fld2").Result);
			}
		}
		[Fact]
		public void HExists()
		{
			using (var cli = CreateClient())
			{
				Assert.Equal(0, cli.HExistsAsync("Key", "Fld").Result);
				cli.HSetAsync("Key", "Fld", GetBytes("Value")).Wait();
				Assert.Equal(1, cli.HExistsAsync("Key", "Fld").Result);
			}
		}
		[Fact]
		public void HGet()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("Key", "Fld1", GetBytes("Val1")).Wait();
				Assert.Equal("Val1", GetString(cli.HGetAsync("Key", "Fld1").Result));
			}
		}
		[Fact]
		public void HGetAll()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("Key", "Fld1", GetBytes("Val1")).Wait();
				cli.HSetAsync("Key", "Fld2", GetBytes("Val2")).Wait();
				var result = cli.HGetAllAsync("Key").Result;
				Assert.Equal("Fld1", GetString(result[0]));
				Assert.Equal("Val1", GetString(result[1]));
				Assert.Equal("Fld2", GetString(result[2]));
				Assert.Equal("Val2", GetString(result[3]));
			}
		}
		[Fact]
		public void HIncrBy()
		{
			using (var cli = CreateClient())
			{
				Assert.Equal(23, cli.HIncrByAsync("Key", "Fld", 23).Result);
			}
		}
		[Fact]
		public void HKeys()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("Key", "Fld1", GetBytes("Val1")).Wait();
				cli.HSetAsync("Key", "Fld2", GetBytes("Val2")).Wait();
				var result = cli.HKeysAsync("Key").Result;
				Assert.Equal("Fld1", GetString(result[0]));
				Assert.Equal("Fld2", GetString(result[1]));
			}
		}
		[Fact]
		public void HVals()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("Key", "Fld1", GetBytes("Val1")).Wait();
				cli.HSetAsync("Key", "Fld2", GetBytes("Val2")).Wait();
				var result = cli.HValsAsync("Key").Result;
				Assert.Equal("Val1", GetString(result[0]));
				Assert.Equal("Val2", GetString(result[1]));
			}
		}
		[Fact]
		public void LPushBlPop()
		{
			using (var cli = CreateClient())
			{
				using (var cli2 = CreateClient())
				{
					var blpop = cli.BlPopAsync(10, "Key");
					cli2.LPushAsync("Key", GetBytes("Value")).Wait();
					var result = blpop.Result;
					Assert.Equal("Key", GetString(result[0]));
					Assert.Equal("Value", GetString(result[1]));
				}
			}
		}
		[Fact]
		public void LPushBlPop_NilReply()
		{
			using (var cli = CreateClient())
			{
				var blpop = cli.BlPopAsync(1, "Key");
				var result = blpop.Result;
				Assert.True(result.IsNull);
			}
		}
		[Fact]
		public void RPushBrPop()
		{
			using (var cli = CreateClient())
			{
				using (var cli2 = CreateClient())
				{
					var brpop = cli.BrPopAsync(10, "Key");
					cli2.RPushAsync("Key", GetBytes("Value")).Wait();
					var result = brpop.Result;
					Assert.Equal("Key", GetString(result[0]));
					Assert.Equal("Value", GetString(result[1]));
				}
			}
		}
		[Fact]
		public void RPop()
		{
			using (var cli = CreateClient())
			{
				cli.RPushAsync("Key", GetBytes("Value")).Wait();
				Assert.Equal("Value", GetString(cli.RPopAsync("Key").Result));
			}
		}
		[Fact]
		public void LPop()
		{
			using (var cli = CreateClient())
			{
				cli.LPushAsync("Key", GetBytes("Value")).Wait();
				Assert.Equal("Value", GetString(cli.LPopAsync("Key").Result));
			}
		}
		[Fact]
		public void HLen()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("Key", "Fld1", GetBytes("Val1")).Wait();
				cli.HSetAsync("Key", "Fld2", GetBytes("Val2")).Wait();
				Assert.Equal(2, cli.HLenAsync("Key").Result);
			}
		}
		[Fact]
		public void HMGet()
		{
			using (var cli = CreateClient())
			{
				cli.HSetAsync("Key", "Fld1", GetBytes("Val1")).Wait();
				cli.HSetAsync("Key", "Fld2", GetBytes("Val2")).Wait();
				var result = cli.HMGetAsync("Key", "Fld1", "Fld2").Result;
				Assert.Equal("Val1", GetString(result[0]));
				Assert.Equal("Val2", GetString(result[1]));
			}
		}
		[Fact]
		public void HMSet()
		{
			using (var cli = CreateClient())
			{
				cli.HMSetAsync("Key", new MSetArgs("Fld1", GetBytes("Val1")),
					new MSetArgs("Fld2", GetBytes("Val2"))).Wait();
				var result = cli.HMGetAsync("Key", "Fld1", "Fld2").Result;
				Assert.Equal("Val1", GetString(result[0]));
				Assert.Equal("Val2", GetString(result[1]));
			}
		}
		#region hyperloglog
		[Fact]
		public void PfAdd()
		{
			using (var cli = CreateClient())
			{
				var res = cli.PfAddAsync("hll", 1, 2, 3, 4, 5, 6, 7).Result;
				Assert.Equal(1, res);
			}
		}
		[Fact]
		public void PfCount()
		{
			using (var cli = CreateClient())
			{
				cli.PfAddAsync("hll", 1, 2, 3, 4, 5, 6, 7).Wait();
				var res = cli.PfCountAsync("hll").Result;
				Assert.Equal(7, res);
			}
		}
		[Fact]
		public void PfMerge()
		{
			using (var cli = CreateClient())
			{
				cli.PfAddAsync("hll1", "foo", "bar", "zap", "a").Wait();
				cli.PfAddAsync("hll2", "a", "b", "c", "foo").Wait();
				var mergeRes = cli.PfMergeAsync("hll3", "hll1", "hll2").Result;
				var countRes = cli.PfCountAsync("hll3").Result;
				Assert.Equal("OK", mergeRes);
				Assert.Equal(6, countRes);
			}
		}
		#endregion hyperloglog
		[Fact]
		public void RPopLPush()
		{
			using (var cli = CreateClient())
			{
				cli.RPushAsync("mylist", GetBytes("one")).Wait();
				cli.RPushAsync("mylist", GetBytes("two")).Wait();
				cli.RPushAsync("mylist", GetBytes("three")).Wait();
				Assert.Equal("three", GetString(cli.RPopLPushAsync("mylist", "mylist2").Result));
			}
		}
		[Fact]
		public void BRPopLPush()
		{
			using (var cli = CreateClient())
			{
				cli.RPushAsync("mylist", GetBytes("one")).Wait();
				cli.RPushAsync("mylist", GetBytes("two")).Wait();
				cli.RPushAsync("mylist", GetBytes("three")).Wait();
				Assert.Equal("three", GetString(cli.BRPopLPushAsync("mylist", "mylist2", 10).Result));
			}
		}
		[Fact]
		public void LIndex()
		{
			using (var cli = CreateClient())
			{
				cli.LPushAsync("Key", GetBytes("Value1")).Wait();
				cli.RPushAsync("Key", GetBytes("Value2")).Wait();
				Assert.Equal("Value2", GetString(cli.LIndexAsync("Key", 1).Result));
			}
		}
		[Fact]
		public void LInsert()
		{
			using (var cli = CreateClient())
			{
				cli.LPushAsync("Key", GetBytes("Value1")).Wait();
				cli.RPushAsync("Key", GetBytes("Value2")).Wait();
				cli.LInsertAsync("Key", GetBytes("Value2"), GetBytes("Value1.5")).Wait();

				Assert.Equal("Value1.5", GetString(cli.LIndexAsync("Key", 1).Result));
			}
		}
		[Fact]
		public void LLen()
		{
			using (var cli = CreateClient())
			{
				cli.LPushAsync("Key", GetBytes("Value1")).Wait();
				cli.RPushAsync("Key", GetBytes("Value2")).Wait();

				Assert.Equal(2, cli.LLenAsync("Key").Result);
			}
		}
		[Fact]
		public void LPushX()
		{
			using (var cli = CreateClient())
			{
				cli.LPushXAsync("Key", GetBytes("Value1")).Wait();


				Assert.Equal(0, cli.LLenAsync("Key").Result);
			}
		}
		[Fact]
		public void RPushX()
		{
			using (var cli = CreateClient())
			{
				cli.RPushXAsync("Key", GetBytes("Value1")).Wait();
				Assert.Equal(0, cli.LLenAsync("Key").Result);
			}
		}
		[Fact]
		public void LRange()
		{
			using (var cli = CreateClient())
			{
				cli.RPushAsync("Key", GetBytes("One")).Wait();
				cli.RPushAsync("Key", GetBytes("Two")).Wait();
				cli.RPushAsync("Key", GetBytes("Three")).Wait();

				var result = cli.LRangeAsync("Key", 0, 1).Result;
				Assert.Equal("One", GetString(result[0]));
				Assert.Equal("Two", GetString(result[1]));
			}
		}
		[Fact]
		public void LRem()
		{
			using (var cli = CreateClient())
			{
				cli.RPushAsync("Key", GetBytes("hello")).Wait();
				cli.RPushAsync("Key", GetBytes("hello")).Wait();
				cli.RPushAsync("Key", GetBytes("foo")).Wait();
				cli.RPushAsync("Key", GetBytes("hello")).Wait();

				Assert.Equal(2, cli.LRemAsync("Key", -2, GetBytes("hello")).Result);
				Assert.Equal(2, cli.LLenAsync("Key").Result);
			}
		}
		[Fact]
		public void LTrim()
		{
			using (var cli = CreateClient())
			{
				cli.RPushAsync("Key", GetBytes("one")).Wait();
				cli.RPushAsync("Key", GetBytes("two")).Wait();
				Assert.Equal("OK", cli.LTrimAsync("Key", 1, 1).Result);

				Assert.Equal(1, cli.LLenAsync("Key").Result);
			}
		}
		[Fact]
		public void SAdd()
		{
			using (var cli = CreateClient())
			{
				Assert.Equal(2, cli.SAddAsync("Key", GetBytes("Val1"), GetBytes("Val2")).Result);
			}
		}
		[Fact]
		public void SRem()
		{
			using (var cli = CreateClient())
			{
				Assert.Equal(2, cli.SAddAsync("Key", GetBytes("Val1"), GetBytes("Val2")).Result);
				Assert.Equal(1, cli.SRemAsync("Key", GetBytes("Val1")).Result);
				Assert.Equal(1, cli.SCardAsync("Key").Result);
			}
		}
		[Fact]
		public void SCard()
		{
			using (var cli = CreateClient())
			{
				cli.SAddAsync("Key", GetBytes("Val1"), GetBytes("Val2")).Wait();
				Assert.Equal(2, cli.SCardAsync("Key").Result);
			}
		}


		[Fact]
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

				Assert.Equal(2, cli.SDiffStoreAsync("New", "Key", "Key2").Result);
				Assert.Equal(2, cli.SCardAsync("New").Result);
			}
		}

		[Fact]
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
				Assert.Equal(4, result);
			}
		}
		[Fact]
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
				Assert.Equal("c", GetString(result[0]));
			}
		}
		[Fact]
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

				Assert.Equal(1, cli.SInterStoreAsync("New", "Key", "Key2").Result);
				Assert.Equal(1, cli.SCardAsync("New").Result);
			}
		}
		[Fact]
		public void SIsMember()
		{
			using (var cli = CreateClient())
			{
				cli.SAddAsync("Key", GetBytes("a")).Wait();
				Assert.Equal(1, cli.SIsMemberAsync("Key", GetBytes("a")).Result);
			}
		}

		[Fact]
		public void SMove()
		{
			using (var cli = CreateClient())
			{
				cli.SAddAsync("Key", GetBytes("a")).Wait();
				Assert.Equal(1, cli.SMoveAsync("Key", "Key2", GetBytes("a")).Result);
				var result = cli.SMembersAsync("Key2").Result;
				Assert.Equal("a", GetString(result[0]));
			}
		}
		[Fact]
		public void SPop()
		{
			using (var cli = CreateClient())
			{
				cli.SAddAsync("Key", GetBytes("a")).Wait();
				Assert.Equal("a", GetString(cli.SPopAsync("Key").Result));

			}
		}
		[Fact]
		public void SRandMember()
		{
			using (var cli = CreateClient())
			{
				cli.SAddAsync("Key", GetBytes("a")).Wait();
				cli.SAddAsync("Key", GetBytes("b")).Wait();
				Assert.Equal(2, cli.SRandMemberAsync("Key", 2).Result.Length);
			}
		}
		[Fact]
		public void ZAdd()
		{
			using (var cli = CreateClient())
			{
				Assert.Equal(2, cli.ZAddAsync("Key", new ZAddArgs(100.2, GetBytes("Value")),
														new ZAddArgs(100, GetBytes("Value2"))).Result);
			}
		}
		[Fact]
		public void ZCard()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("Key", new ZAddArgs(100.2, GetBytes("Value")),
														new ZAddArgs(100, GetBytes("Value2"))).Wait();
				Assert.Equal(2, cli.ZCardAsync("Key").Result);
			}
		}
		[Fact]
		public void ZCount()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("Key", new ZAddArgs(100, GetBytes("Value")),
									 new ZAddArgs(200, GetBytes("Value2")),
									 new ZAddArgs(300, GetBytes("Value3"))).Wait();
				Assert.Equal(1, cli.ZCountAsync("Key", 150, 222).Result);
			}
		}
		[Fact]
		public void ZCountInf()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("Key", new ZAddArgs(100.0, GetBytes("Value")),
									 new ZAddArgs(200, GetBytes("Value2")),
									 new ZAddArgs(300, GetBytes("Value3"))).Wait();
				Assert.Equal(2, cli.ZCountAsync("Key", double.NegativeInfinity, 222).Result);
			}
		}

		[Fact]
		public void ZIncrBy()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("Key", new ZAddArgs(100.0, GetBytes("Value"))).Wait();
				Assert.Equal(110.3, cli.ZIncrByAsync("Key", 10.3, GetBytes("Value")).Result);
			}
		}
		[Fact]
		public void ZInterStore()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.Equal(2, cli.ZInterStoreAsync("out", "zset1", "zset2").Result);
			}
		}
		[Fact]
		public void ZInterStore_Min()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.Equal(2, cli.ZInterStoreAsync("out", Aggregation.Min, "zset1", "zset2").Result);
			}
		}
		[Fact]
		public void ZInterStore_WithWeights()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.Equal(2, cli.ZInterStoreAsync("out", new ZAggrStoreArgs("zset1", 2),
														 new ZAggrStoreArgs("zset2", 3)).Result);
			}
		}
		[Fact]
		public void ZInterStore_WithWeights_Sum()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.Equal(2, cli.ZInterStoreAsync("out", Aggregation.Sum,
													new ZAggrStoreArgs("zset1", 2),
													new ZAggrStoreArgs("zset2", 3)).Result);
			}
		}
		[Fact]
		public void ZRange()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();

				var result = cli.ZRangeAsync("zset1", 1, 2).Result;

				Assert.Equal(2, result.Length);
				Assert.Equal("two", GetString(result[0]));
				Assert.Equal("three", GetString(result[1]));
			}
		}
		[Fact]
		public void ZRange_WithScores()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();

				var result = cli.ZRangeAsync("zset1", 1, 2, true).Result;

				Assert.Equal(4, result.Length);

				Assert.Equal("two", GetString(result[0]));
				Assert.Equal("2", GetString(result[1]));

				Assert.Equal("three", GetString(result[2]));
				Assert.Equal("3", GetString(result[3]));
			}
		}
		[Fact]
		public void ZRangeByScore()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();


				var result = cli.ZRangeByScoreAsync("zset1", double.NegativeInfinity, 2).Result;

				Assert.Equal(2, result.Length);

				Assert.Equal("one", GetString(result[0]));
				Assert.Equal("two", GetString(result[1]));
			}
		}
		[Fact]
		public void ZRangeByScore_WithScores()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();


				var result = cli.ZRangeByScoreAsync("zset1", double.NegativeInfinity, 2, true).Result;

				Assert.Equal(4, result.Length);

				Assert.Equal("1", GetString(result[1]));
				Assert.Equal("2", GetString(result[3]));

				Assert.Equal("one", GetString(result[0]));
				Assert.Equal("two", GetString(result[2]));
			}
		}
		[Fact]
		public void ZRangeByScore_WithLimits()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();

				var result = cli.ZRangeByScoreAsync("zset1", double.NegativeInfinity, 2, 1, 1).Result;

				Assert.Equal(1, result.Length);

				Assert.Equal("two", GetString(result[0]));
			}
		}
		[Fact]
		public void ZRangeByScore_WithLimits_WithScores()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();

				var result = cli.ZRangeByScoreAsync("zset1", double.NegativeInfinity, 2, 1, 1, true).Result;

				Assert.Equal(2, result.Length);

				Assert.Equal("two", GetString(result[0]));
				Assert.Equal("2", GetString(result[1]));
			}
		}
		[Fact]
		public void ZRank()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();

				Assert.Equal(1, cli.ZRankAsync("zset1", GetBytes("three")).Result.Value);
			}
		}
		[Fact]
		public void ZRevRank()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();

				Assert.Equal(0, cli.ZRevRankAsync("zset1", GetBytes("three")).Result.Value);
			}
		}
		[Fact]
		public void ZRank_NullReply()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();

				Assert.Equal(null, cli.ZRankAsync("zset1", GetBytes("four")).Result);
			}
		}
		[Fact]
		public void ZRem()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(5, GetBytes("five"))).Wait();
				Assert.Equal(2, cli.ZRemAsync("zset1", GetBytes("three"), GetBytes("one")).Result);
				Assert.Equal(1, cli.ZCardAsync("zset1").Result);
			}
		}
		[Fact]
		public void ZRemRangeByRank()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(5, GetBytes("five"))).Wait();
				Assert.Equal(1, cli.ZRemRangeByRankAsync("zset1", 2, 4).Result);
				Assert.Equal(2, cli.ZCardAsync("zset1").Result);
			}
		}
		[Fact]
		public void ZRemRangeByScore()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(5, GetBytes("five"))).Wait();
				Assert.Equal(2, cli.ZRemRangeByScoreAsync("zset1", 2, double.PositiveInfinity).Result);
				Assert.Equal(1, cli.ZCardAsync("zset1").Result);
			}
		}
		[Fact]
		public void ZRevRange()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", 1, GetBytes("one")).Wait();
				cli.ZAddAsync("zset1", 2, GetBytes("two")).Wait();
				cli.ZAddAsync("zset1", 3, GetBytes("three")).Wait();

				var result = cli.ZRevRangeAsync("zset1", 0, -1).Result;
				Assert.Equal(3, result.Length);
				Assert.Equal("three", GetString(result[0]));
				Assert.Equal("two", GetString(result[1]));
				Assert.Equal("one", GetString(result[2]));
			}
		}
		[Fact]
		public void ZRevRange_WithScores()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", 1, GetBytes("one")).Wait();
				cli.ZAddAsync("zset1", 2, GetBytes("two")).Wait();
				cli.ZAddAsync("zset1", 3, GetBytes("three")).Wait();

				var result = cli.ZRevRangeAsync("zset1", 0, -1, true).Result;
				Assert.Equal(6, result.Length);
			}
		}
		[Fact]
		public void ZRevRangeByScore()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("s1", 3, GetBytes("three")).Wait();
				cli.ZAddAsync("s1", 1, GetBytes("one")).Wait();
				cli.ZAddAsync("s1", 2, GetBytes("two")).Wait();


				var result = cli.ZRevRangeByScoreAsync("s1", double.PositiveInfinity, double.NegativeInfinity).Result;

				Assert.Equal(3, result.Length);

				Assert.Equal("one", GetString(result[2]));
				Assert.Equal("two", GetString(result[1]));
				Assert.Equal("three", GetString(result[0]));
			}
		}
		[Fact]
		public void ZRevRangeByScore_WithScores()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();


				var result = cli.ZRevRangeByScoreAsync("zset1", double.PositiveInfinity, double.NegativeInfinity, true).Result;

				Assert.Equal(6, result.Length);
			}
		}
		[Fact]
		public void ZRevRangeByScore_WithLimits()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();

				var result = cli.ZRevRangeByScoreAsync("zset1", 3, 0, 1, 1).Result;

				Assert.Equal(1, result.Length);

				Assert.Equal("two", GetString(result[0]));
			}
		}
		[Fact]
		public void ZRevRangeByScore_WithLimits_WithScores()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();

				var result = cli.ZRevRangeByScoreAsync("zset1", 3, 0, 1, 1, true).Result;

				Assert.Equal(2, result.Length);

				Assert.Equal("two", GetString(result[0]));
				Assert.Equal("2", GetString(result[1]));
			}
		}
		[Fact]
		public void ZScore()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, GetBytes("three"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();

				Assert.Equal(3, cli.ZScoreAsync("zset1", GetBytes("three")).Result);
			}
		}
		[Fact]
		public void ZUnionStore()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.Equal(3, cli.ZUnionStoreAsync("out", "zset1", "zset2").Result);
			}
		}
		[Fact]
		public void ZUnionStore_Min()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.Equal(3, cli.ZUnionStoreAsync("out", Aggregation.Min, "zset1", "zset2").Result);
			}
		}
		[Fact]
		public void ZUnionStore_WithWeights()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.Equal(3, cli.ZUnionStoreAsync("out", new ZAggrStoreArgs("zset1", 2),
														 new ZAggrStoreArgs("zset2", 3)).Result);
			}
		}
		[Fact]
		public void ZUnionStore_WithWeights_Sum()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(1, GetBytes("one"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(2, GetBytes("two"))).Wait();
				cli.ZAddAsync("zset2", new ZAddArgs(3, GetBytes("three"))).Wait();

				Assert.Equal(3, cli.ZUnionStoreAsync("out", Aggregation.Sum,
													new ZAggrStoreArgs("zset1", 2),
													new ZAggrStoreArgs("zset2", 3)).Result);
			}
		}
		#region pubsub
		[Fact]
		public void Subscribe()
		{
			using (var cli1 = CreateClient())
			{
				var c1 = cli1.SubscribeAsync("channel").Result;
				var subResult = c1.ReadMessageAsync().Result;
				Assert.Equal(ChannelMessageType.Subscribe, subResult.MessageType);
				Assert.Equal("channel", subResult.Channels[0]);
				Assert.Equal("1", GetString(subResult.Value));
			}
		}
		[Fact]
		public void Subscribe_WithFilter_ReadMessagesAfterChannelClosing()
		{
			Assert.Throws<AggregateException>(() =>
			{
				using (var cli1 = CreateClient())
				{
					var c1 = cli1.SubscribeAsync("channel").Result;
					c1.ReadMessageAsync().Wait(); //read subscribe message
					var readTask = c1.ReadMessageAsync(ChannelMessageType.Message);
					c1.Dispose();
					readTask.Wait();
				}
			});
		}
		[Fact]
		public void Subscribe_NoFilter_ReadMessagesAfterChannelClosing()
		{
			Assert.Throws<AggregateException>(() =>
			{
				using (var cli1 = CreateClient())
				{
					var c1 = cli1.SubscribeAsync("channel").Result;
					c1.ReadMessageAsync().Wait();//read subscribe message
					var readTask = c1.ReadMessageAsync();
					c1.Dispose();
					var subResult = readTask.Result;
					Assert.Equal(ChannelMessageType.Unknown, subResult.MessageType);
					Assert.Equal(ResponseType.Error, subResult.Value.ResponseType);
				}
			});
		}
		[Fact]
		public void Unsubscribe()
		{
			using (var cli1 = CreateClient())
			{
				var subClient = cli1.SubscribeAsync("channel").Result;
				subClient.ReadMessageAsync().Wait();
				subClient.UnsubscribeAsync("channel").Wait();

				var subResult = subClient.ReadMessageAsync().Result;

				Assert.Equal(ChannelMessageType.Unsubscribe, subResult.MessageType);
				Assert.Equal("channel", subResult.Channels[0]);
				Assert.Equal("0", GetString(subResult.Value));
			}
		}

		[Fact]
		public void Publish()
		{
			using (var cli1 = CreateClient())
			{
				Assert.Equal(0, cli1.PublishAsync("channel", GetBytes("Message")).Result);
			}
		}

		[Fact]
		public void Publish_Subscribe_WithFilter()
		{
			using (var subscriber = CreateClient().SubscribeAsync("channel").Result)
			{
				using (var publisher = CreateClient())
				{
					publisher.PublishAsync("channel", GetBytes("Message")).Wait();

					var channelMessage = subscriber.ReadMessageAsync(ChannelMessageType.Message |
																ChannelMessageType.PMessage).Result;

					Assert.Equal(ChannelMessageType.Message, channelMessage.MessageType);
					Assert.Equal("channel", channelMessage.Channels[0]);
					Assert.Equal("Message", GetString(channelMessage.Value));
				}
			}
		}
		[Fact]
		public void Publish_Subscribe_TimeoutEmulation()
		{
			using (var cli1 = CreateClient())
			{
				var c1 = cli1.SubscribeAsync("channel").Result;

				var readAsync = c1.ReadMessageAsync(ChannelMessageType.Message | ChannelMessageType.Unsubscribe);

				Thread.Sleep(1000);
				c1.UnsubscribeAsync("channel").Wait();
				var result = readAsync.Result;
				Assert.Equal(ChannelMessageType.Unsubscribe, result.MessageType);
			}
		}
		[Fact]
		public void Subscribe_Quit_WithFilter_ExceptionExpected()
		{
			Assert.Throws<AggregateException>(() =>
			{
				using (var cli1 = CreateClient().SubscribeAsync("channel").Result)
				{
					cli1.QuitAsync().Wait();
					var result = cli1.ReadMessageAsync(ChannelMessageType.Message).Result;
					Assert.Equal(ChannelMessageType.Unknown, result.MessageType);
					Assert.Equal(ResponseType.Status, result.Value.ResponseType);
				}
			});
		}

		[Fact]
		public void Subscribe_Quit_NoFilter_ValidResponse()
		{
			using (var cli1 = CreateClient().SubscribeAsync("channel").Result)
			{
				cli1.ReadMessageAsync().Wait();//read subscribe message;
				cli1.QuitAsync().Wait();
				var result = cli1.ReadMessageAsync().Result;
				Assert.Equal(ChannelMessageType.Unknown, result.MessageType);
				Assert.Equal(ResponseType.Status, result.Value.ResponseType);
			}
		}
		#endregion

		[Fact]
		public void Transactions()
		{
			using (var cli1 = CreateClient())
			{
				Assert.Equal("OK", cli1.MultiAsync().Result);
				Assert.Equal(0, cli1.IncrAsync("foo").Result);
				Assert.Equal(0, cli1.IncrAsync("bar").Result);
				var result = cli1.ExecAsync().Result;
				Assert.Equal("1", GetString(result[0]));
				Assert.Equal("1", GetString(result[1]));
			}
		}
		[Fact]
		public void Eval()
		{
			using (var cli = CreateClient())
			{
				var result = cli.EvalAsync("return {KEYS[1],KEYS[2],ARGV[1],ARGV[2]}",
											new[] { "key1", "key2" }, GetBytes("first"), GetBytes("second")).Result;

				Assert.Equal(ResponseType.MultiBulk, result.ResponseType);
				var mb = result.AsMultiBulk();
				Assert.Equal("key1", GetString(mb[0].AsBulk()));
				Assert.Equal("key2", GetString(mb[1].AsBulk()));
				Assert.Equal("first", GetString(mb[2].AsBulk()));
				Assert.Equal("second", GetString(mb[3].AsBulk()));
			}
		}
		[Fact]
		public void ScriptLoadSha()
		{
			using (var cli = CreateClient())
			{
				var sha1 = cli.ScriptLoadAsync("return {KEYS[1],KEYS[2],ARGV[1],ARGV[2]}").Result;

				var result = cli.EvalShaAsync(sha1, new[] { "key1", "key2" }, GetBytes("first"), GetBytes("second")).Result;
				Assert.Equal(ResponseType.MultiBulk, result.ResponseType);
				var mb = result.AsMultiBulk();
				Assert.Equal("key1", GetString(mb[0].AsBulk()));
				Assert.Equal("key2", GetString(mb[1].AsBulk()));
				Assert.Equal("first", GetString(mb[2].AsBulk()));
				Assert.Equal("second", GetString(mb[3].AsBulk()));
			}
		}
		[Fact]
		public void ScriptExists()
		{
			using (var cli = CreateClient())
			{
				var sha1 = cli.ScriptLoadAsync("return {KEYS[1],KEYS[2],ARGV[1],ARGV[2]}").Result;

				var result = cli.ScriptExistsAsync(sha1).Result;
				Assert.Equal(1, result.Length);
				Assert.Equal("1", GetString(result[0]));
			}
		}
		[Fact]
		public void ScriptFlushExists()
		{
			using (var cli = CreateClient())
			{
				var sha1 = cli.ScriptLoadAsync("return {KEYS[1],KEYS[2],ARGV[1],ARGV[2]}").Result;
				cli.ScriptFlushAsync().Wait();
				var result = cli.ScriptExistsAsync(sha1).Result;
				Assert.Equal(1, result.Length);
				Assert.Equal("0", GetString(result[0]));
			}
		}
		[Fact]
		public void Echo()
		{
			using (var cli = CreateClient())
			{
				var resp = cli.EchoAsync(GetBytes("Message")).Result;
				Assert.Equal("Message", GetString(resp));
			}
		}
		[Fact]
		public void Ping()
		{
			using (var cli = CreateClient())
			{
				Assert.Equal("PONG", cli.PingAsync().Result);
			}
		}
		[Fact]
		public void Quit()
		{
			using (var cli = CreateClient())
			{
				Assert.Equal("OK", cli.QuitAsync().Result);
			}
		}

		[Fact]
		public void ClientList()
		{
			using (var cli = CreateClient())
			{
				var result = GetString(cli.ClientListAsync().Result);
				Assert.NotEmpty(result);
			}
		}

		[Fact]
		public void DbSize()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("key", GetBytes("value")).Wait();
				Assert.Equal(1, cli.DbSizeAsync().Result);
			}
		}
		[Fact]
		public void FlushAll()
		{
			using (var cli = CreateClient())
			{
				Assert.Equal("OK", cli.FlushAllAsync().Result);
			}
		}
		[Fact]
		public void Info()
		{
			using (var cli = CreateClient())
			{
				var result = GetString(cli.InfoAsync().Result);
				Assert.NotEmpty(result);
			}
		}
		[Fact]
		public void ConfigGet()
		{
			using (var cli = CreateClient())
			{
				var result = cli.ConfigGetAsync("loglevel").Result;
			}
		}
		[Fact]
		public void LastSave()
		{
			using (var cli = CreateClient())
			{
				Assert.True(cli.LastSaveAsync().Result > 0);
			}
		}
		[Fact]
		public void Time()
		{
			using (var cli = CreateClient())
			{
				var result = cli.TimeAsync().Result;
			}
		}
		[Fact]
		public void ResetStat()
		{
			using (var cli = CreateClient())
			{
				var result = cli.ConfigResetStatAsync().Result;
			}
		}
		[Fact]
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
				Assert.Equal(cli1, cli2);
			}
		}

		[Fact]
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
				Assert.NotEqual(cli1, cli2);
			}
		}
		[Fact]
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
					Assert.Equal(typeof(RedisException), ex.InnerException.GetType());
					Assert.Equal("Pipeline is in OneWay mode", ex.InnerException.Message);
				}

			}
		}
		[Fact]
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
					Assert.Equal("Value" + i, GetString(tasks[i].Result[0]));
					Assert.Equal("Value" + i, GetString(tasks[i].Result[1]));
				}
			}
		}
		[Fact]
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
					Assert.False(tasks[i].IsFaulted);
					Assert.Equal("Value" + i, GetString(tasks[i].Result));
				}
				for (int i = 5000; i < 10000; i++)
				{
					SpinWait.SpinUntil(() => tasks[i].IsCompleted);
					Assert.True(tasks[i].IsFaulted);
				}
			}
		}
		private int _requestId = 0;
		[Fact]
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
					Assert.Equal("Value" + item.Key, GetString(item.Value.Result));
				}
			}
		}
		[Fact]
		public void Ctor_WithHostPort()
		{
			var sb = new RedisConnectionStringBuilder(ConnectionString);
			using (var cli = RedisClient.ConnectAsync(((IPEndPoint)sb.EndPoint).Address.ToString(), ((IPEndPoint)sb.EndPoint).Port, 3).Result)
			{
				Assert.Equal("PONG", cli.PingAsync().Result);
			}
		}
		[Fact]
		public void PubSubChannels()
		{
			Assert.True(false, "waiting for Redis 2.8 released");
		}
		[Fact]
		public void PubSubNumSub()
		{
			Assert.True(false, "waiting for Redis 2.8 released");
		}
		[Fact]
		public void PubSubNumPat()
		{
			Assert.True(false, "waiting for Redis 2.8 released");
		}
		private static byte[] GetBytes(string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}
		private static string GetString(byte[] data)
		{
			return Encoding.UTF8.GetString(data, 0, data.Length);
		}
		private string ConnectionString
		{
			get { return "data source = 127.0.0.1:6379; initial catalog = 0;"; }
		}
		private IRedisClient CreateClient()
		{
			var cli = RedisClient.ConnectAsync(ConnectionString).Result;
			cli.FlushDbAsync().Wait();
			return cli;
		}
	}
}


