using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NBoosters.RedisBoost.Tests
{
	[TestFixture]
	public class IntegrationTestsForGenericMethods
	{
		#region model
		public class ModelRoot
		{
			protected bool Equals(ModelRoot other)
			{
				return IntProp == other.IntProp
								&& DateTime.Equals(other.DateTime)
								&& ModelEnum == other.ModelEnum
								&& string.Equals(StrProp, other.StrProp)
								&& Equals(SubModel, other.SubModel);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					int hashCode = IntProp;
					hashCode = (hashCode * 397) ^ DateTime.GetHashCode();
					hashCode = (hashCode * 397) ^ (int)ModelEnum;
					hashCode = (hashCode * 397) ^ (StrProp != null ? StrProp.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (SubModel != null ? SubModel.GetHashCode() : 0);
					return hashCode;
				}
			}

			public int IntProp { get; set; }
			public DateTime DateTime { get; set; }
			public ModelEnum ModelEnum { get; set; }
			public string StrProp { get; set; }
			public SubModel SubModel { get; set; }

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(this, obj))
					return true;
				var val = obj as ModelRoot;
				if (val == null) return false;
				return Equals(val);
			}
		}
		[Serializable]
		public enum ModelEnum
		{
			Value1,
			Value2,
			Value3,
		}

		public class SubModel
		{
			protected bool Equals(SubModel other)
			{
				return string.Equals(Prop1, other.Prop1)
					&& string.Equals(Prop2, other.Prop2)
					&& EnumArray.SequenceEqual(other.EnumArray);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					int hashCode = (Prop1 != null ? Prop1.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (Prop2 != null ? Prop2.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (EnumArray != null ? EnumArray.GetHashCode() : 0);
					return hashCode;
				}
			}

			public string Prop1 { get; set; }
			public string Prop2 { get; set; }
			public ModelEnum[] EnumArray { get; set; }
			public override bool Equals(object obj)
			{
				if (ReferenceEquals(this, obj))
					return true;
				var val = obj as SubModel;
				if (val == null) return false;
				return Equals(val);
			}
		}
		private static ModelRoot CreateModel()
		{
			return new ModelRoot()
			{
				IntProp = 3,
				SubModel = new SubModel()
				{
					Prop1 = "adf",
					EnumArray = new[] { ModelEnum.Value1, ModelEnum.Value3, }
				},
				StrProp = "ET",
				DateTime = DateTime.Now
			};
		}

		#endregion
		[Test]
		public void Set()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", CreateModel()).Wait();
			}
		}
		[Test]
		public void Get()
		{
			using (var cli = CreateClient())
			{
				var expected = CreateModel();
				cli.SetAsync("Key", expected).Wait();
				var result = cli.GetAsync<ModelRoot>("Key").Result;
				Assert.AreEqual(expected, result);
			}
		}
		[Test]
		public void SetString()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", "Value").Wait();
			}
		}
		[Test]
		public void GetString()
		{
			using (var cli = CreateClient())
			{
				var expected = "Value";
				cli.SetAsync("Key", expected).Wait();
				var result = cli.GetAsync<string>("Key").Result;
				Assert.AreEqual(expected, result);
			}
		}
		[Test]
		public void Append()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", "Value").Wait();
				cli.AppendAsync("Key", "_appendix").Wait();
				Assert.AreEqual("Value_appendix", cli.GetAsync<string>("Key").Result);
			}
		}
		[Test]
		public void IncrByFloat()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("key", 10.50).Wait();
				Assert.AreEqual(10.6, cli.IncrByFloatAsync<double>("key", 0.1).Result);
			}
		}
		[Test]
		public void GetRange()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", "Value").Wait();
				Assert.AreEqual("alu", cli.GetRangeAsync<string>("Key", 1, 3).Result);
			}
		}
		[Test]
		public void SetRange()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", "Value").Wait();
				cli.SetRangeAsync("Key", 2, "eul").Wait();
				Assert.AreEqual("Vaeul", cli.GetAsync<string>("Key").Result);
			}
		}
		[Test]
		public void GetSet()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", "Value").Wait();
				Assert.AreEqual("Value", cli.GetSetAsync("Key", "NewValue").Result);
			}
		}
		[Test]
		public void GetSet_DiffTypes()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", 10.3).Wait();
				Assert.AreEqual(10.3, cli.GetSetAsync<double>("Key", "NewValue").Result);
				Assert.AreEqual("NewValue", cli.GetAsync<string>("Key").Result);
			}
		}
		[Test]
		public void Keys()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key1", "Val1").Wait();
				cli.SetAsync("Key2", "Val2").Wait();
				var result = cli.KeysAsync("*").Result;
				Assert.AreEqual("Key2", result[0].As<string>());
				Assert.AreEqual("Key1", result[1].As<string>());
			}
		}
		[Test]
		public void Keys_Generic()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key1", "Val1").Wait();
				cli.SetAsync("Key2", "Val2").Wait();
				var result = cli.KeysAsync<string>("*").Result;
				Assert.AreEqual("Key2", result[0]);
				Assert.AreEqual("Key1", result[1]);
			}
		}
		[Test]
		public void MGet()
		{
			using (var cli = CreateClient())
			{
				var exp1 = CreateModel();
				var exp2 = "Value2";
				cli.SetAsync("Key", exp1).Wait();
				cli.SetAsync("Key2", exp2).Wait();
				var result = cli.MGetAsync("Key", "Key2").Result;
				Assert.AreEqual(exp1, result[0].As<ModelRoot>());
				Assert.AreEqual("Value2", result[1].As<string>());
			}
		}
		[Test]
		public void MSet()
		{
			using (var cli = CreateClient())
			{
				var exp1 = CreateModel();
				var exp2 = "Value2";
				var res = cli.MSetAsync(new MSetArgs("Key", exp1),
										new MSetArgs("Key2", exp2)).Result;
				Assert.AreEqual("OK", res);
				var result = cli.MGetAsync("Key", "Key2").Result;
				Assert.AreEqual(exp1, result[0].As<ModelRoot>());
				Assert.AreEqual(exp2, result[1].As<string>());
			}
		}
		[Test]
		public void HMSet()
		{
			using (var cli = CreateClient())
			{
				var exp1 = CreateModel();
				var exp2 = "Value2";
				cli.HMSetAsync("Key", new MSetArgs("Fld1", exp1),
									  new MSetArgs("Fld2", exp2)).Wait();
				var result = cli.HMGetAsync("Key", "Fld1", "Fld2").Result;
				Assert.AreEqual(exp1, result[0].As<ModelRoot>());
				Assert.AreEqual(exp2, result[1].As<string>());
			}
		}
		[Test]
		public void SetEx()
		{
			using (var cli = CreateClient())
			{
				cli.SetExAsync("Key", 2, "Value").Wait();
				Thread.Sleep(2000);
				var exists = cli.ExistsAsync("Key").Result;
				Assert.AreEqual(0, exists);
			}
		}
		[Test]
		public void PSetEx()
		{
			using (var cli = CreateClient())
			{
				cli.PSetExAsync("Key", 2000, "Value").Wait();
				Thread.Sleep(2000);
				var exists = cli.ExistsAsync("Key").Result;
				Assert.AreEqual(0, exists);
			}
		}
		[Test]
		public void SetNx()
		{
			using (var cli = CreateClient())
			{
				cli.SetAsync("Key", "Value").Wait();
				cli.SetNxAsync("Key", "NewValue").Wait();
				Assert.AreEqual("Value", cli.GetAsync<string>("Key").Result);
			}
		}
		[Test]
		public void ZAdd()
		{
			using (var cli = CreateClient())
			{
				Assert.AreEqual(2, cli.ZAddAsync("Key", new ZAddArgs(100.2, "Value"),
														new ZAddArgs(100, CreateModel())).Result);
			}
		}
		[Test]
		public void ZRem_StringsOnly()
		{
			using (var cli = CreateClient())
			{
				var val3 = "Value3";
				var val1 = "Value1";
				var val5 = "Value5";

				cli.ZAddAsync("zset1", new ZAddArgs(3, val3)).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, val1)).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(5, val5)).Wait();
				Assert.AreEqual(2, cli.ZRemAsync("zset1", val5, val1).Result);
				Assert.AreEqual(1, cli.ZCardAsync("zset1").Result);
			}
		}
		[Test]
		public void ZRem()
		{
			using (var cli = CreateClient())
			{
				var val3 = CreateModel();
				var val1 = "Value2";
				var val5 = DateTime.Now;

				cli.ZAddAsync("zset1", new ZAddArgs(3, val3)).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, val1)).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(5, val5)).Wait();
				Assert.AreEqual(2, cli.ZRemAsync("zset1", val5, val1).Result);
				Assert.AreEqual(1, cli.ZCardAsync("zset1").Result);
			}
		}

		[Test]
		public void ZIncrBy()
		{
			using (var cli = CreateClient())
			{
				var value = CreateModel();
				cli.ZAddAsync("Key", new ZAddArgs(100.0, value)).Wait();
				Assert.AreEqual(110.3, cli.ZIncrByAsync("Key", 10.3, value).Result);
			}
		}
		[Test]
		public void ZRange()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(1, "one")).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(2, "two")).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(3, "three")).Wait();

				var result = cli.ZRangeAsync("zset1", 1, 2).Result;

				Assert.AreEqual(2, result.Length);
				Assert.AreEqual("two", result[0].As<string>());
				Assert.AreEqual("three", result[1].As<string>());
			}
		}
		[Test]
		public void ZRank()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, "three")).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(3, "three")).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, "one")).Wait();

				Assert.AreEqual(1, cli.ZRankAsync("zset1", "three").Result.Value);
			}
		}
		[Test]
		public void ZRevRank()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, "three")).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, "one")).Wait();

				Assert.AreEqual(0, cli.ZRevRankAsync("zset1", "three").Result.Value);
			}
		}
		[Test]
		public void ZScore()
		{
			using (var cli = CreateClient())
			{
				cli.ZAddAsync("zset1", new ZAddArgs(3, "three")).Wait();
				cli.ZAddAsync("zset1", new ZAddArgs(1, "one")).Wait();

				Assert.AreEqual(3, cli.ZScoreAsync("zset1", "three").Result);
			}
		}
		private string ConnectionString
		{
			get { return ConfigurationManager.ConnectionStrings["Redis"].ConnectionString; }
		}
		private IRedisClient CreateClient()
		{
			var cli = RedisClient.ConnectAsync(ConnectionString).Result;
			cli.FlushDbAsync().Wait();
			return cli;
		}
	}
}

