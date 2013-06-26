using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using RedisBoost.Core.Serialization;

namespace RedisBoost.Tests
{
	[TestFixture]
	public class BasicRedisSerializerTests
	{
		public class ToSerialize
		{
			public string Prop1 { get; set; }
			public int Prop2 { get; set; }
		}
		public enum FooEnum {val1,val2}

		[Test]
		public void Serialize_ByteArray()
		{
			var value = new byte[] {1, 2, 3, 4, 5};
			var res = CreateSerializer().Serialize(value);
			Assert.IsTrue( value.SequenceEqual(res));
		}

		[Test]
		public void Serialize_Enum()
		{
			var value = FooEnum.val1;
			var res = CreateSerializer().Serialize(value);
			Assert.IsTrue(Encoding.UTF8.GetBytes(value.ToString()).SequenceEqual(res));
		}

		[Test]
		public void Serialize_Int()
		{
			var value = 56;
			var res = CreateSerializer().Serialize(value);
			Assert.IsTrue(Encoding.UTF8.GetBytes(value.ToString()).SequenceEqual(res));
		}

		[Test]
		public void Serialize_Float()
		{
			var value = 56.4;
			var res = CreateSerializer().Serialize(value);
			Assert.IsTrue(Encoding.UTF8.GetBytes(value.ToString(CultureInfo.InvariantCulture)).SequenceEqual(res));
		}

		[Test]
		public void Serialize_String()
		{
			var value = "string value";
			var res = CreateSerializer().Serialize(value);
			Assert.IsTrue(Encoding.UTF8.GetBytes(value).SequenceEqual(res));
		}

		[Test]
		public void Serialize_Null()
		{
			var res = CreateSerializer().Serialize(null);
			Assert.IsTrue(res.SequenceEqual(BasicRedisSerializer.Null));
		}

		[Test]
		public void Serialize_DateTime()
		{
			var value = DateTime.Now;
			var res = CreateSerializer().Serialize(value);
			Assert.IsTrue(Encoding.UTF8.GetBytes(value.ToString(BasicRedisSerializer.DatetimeFormat, CultureInfo.InvariantCulture)).SequenceEqual(res));
		}

		[Test]
		public void Serialize_Guid()
		{
			var value = Guid.NewGuid();
			var res = CreateSerializer().Serialize(value);
			Assert.IsTrue(Encoding.UTF8.GetBytes(value.ToString()).SequenceEqual(res));
		}
		
		[Test]
		public void Serialize_Complex()
		{
			var expected = @"<BasicRedisSerializerTests.ToSerialize xmlns=""http://schemas.datacontract.org/2004/07/RedisBoost.Tests"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Prop1>prop1</Prop1><Prop2>23</Prop2></BasicRedisSerializerTests.ToSerialize>";
			var value = new ToSerialize {Prop1 = "prop1", Prop2 = 23};
			var res = CreateSerializer().Serialize(value);
			Assert.IsTrue(Encoding.UTF8.GetBytes(expected).SequenceEqual(res));
		}

		[Test]
		public void Deserialize_ByteArray()
		{
			var value = new byte[] { 1, 2, 3, 4, 5 };
			var res = (byte[])CreateSerializer().Deserialize(typeof(byte[]),value);
			Assert.IsTrue(value.SequenceEqual(res));
		}

		[Test]
		public void Deserialize_String()
		{
			var expected = "expected value";
			var value = CreateSerializer().Serialize(expected);
			var res = (string)CreateSerializer().Deserialize(typeof(string), value);
			Assert.AreEqual(expected,res);
		}

		[Test]
		public void Deserialize_Int()
		{
			var expected = 2343;
			var value = CreateSerializer().Serialize(expected);
			var res = (int)CreateSerializer().Deserialize(typeof(int), value);
			Assert.AreEqual(expected, res);
		}

		[Test]
		public void Deserialize_Float()
		{
			var expected = 2343.23;
			var value = CreateSerializer().Serialize(expected);
			var res = (double)CreateSerializer().Deserialize(typeof(double), value);
			Assert.AreEqual(expected, res);
		}

		[Test]
		public void Deserialize_Char()
		{
			var expected = 'a';
			var value = CreateSerializer().Serialize(expected);
			var res = (char)CreateSerializer().Deserialize(typeof(char), value);
			Assert.AreEqual(expected, res);
		}

		[Test]
		public void Deserialize_Guid()
		{
			var expected = Guid.NewGuid();
			var value = CreateSerializer().Serialize(expected);
			var res = (Guid)CreateSerializer().Deserialize(typeof(Guid), value);
			Assert.AreEqual(expected, res);
		}

		[Test]
		public void Deserialize_DateTime()
		{
			var expected = DateTime.Now;
			var value = CreateSerializer().Serialize(expected);
			var res = (DateTime)CreateSerializer().Deserialize(typeof(DateTime), value);
			Assert.AreEqual(expected, res);
		}

		[Test]
		public void Deserialize_Enum()
		{
			var expected = FooEnum.val2;
			var value = CreateSerializer().Serialize(expected);
			var res = (FooEnum)CreateSerializer().Deserialize(typeof(FooEnum), value);
			Assert.AreEqual(expected, res);
		}

		[Test]
		public void Deserialize_Null()
		{
			object expected = null;
			var value = CreateSerializer().Serialize(expected);
			var res = CreateSerializer().Deserialize(typeof(object), value);
			Assert.AreEqual(null, res);
		}

		[Test]
		public void Deserialize_Complex()
		{
			var expected = new ToSerialize { Prop1 = "prop1", Prop2 = 23 };
			var value = CreateSerializer().Serialize(expected);
			var res = (ToSerialize) CreateSerializer().Deserialize(typeof (ToSerialize), value);

			Assert.AreEqual(expected.Prop1, res.Prop1);
			Assert.AreEqual(expected.Prop2, res.Prop2);
		}

		private BasicRedisSerializer CreateSerializer()
		{
			return new BasicRedisSerializer();
		}
	}
}
