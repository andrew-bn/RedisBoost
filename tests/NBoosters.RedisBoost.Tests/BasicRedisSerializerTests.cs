using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core.Serialization;
using NUnit.Framework;

namespace NBoosters.RedisBoost.Tests
{
	[TestFixture]
	public class BasicRedisSerializerTests
	{
		[Test]
		public void Serialize_ByteArray()
		{
			var value = new byte[] {1, 2, 3, 4, 5};
			var res = CreateSerializer().Serialize(value);
			Assert.IsTrue( value.SequenceEqual(res));
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
			string value = null;
			var res = CreateSerializer().Serialize(value);
			Assert.IsEmpty(res);
		}

		[Test]
		public void Serialize_DateTime()
		{
			var value = DateTime.Now;
			var res = CreateSerializer().Serialize(value);
			Assert.IsTrue(Encoding.UTF8.GetBytes(value.ToString(BasicRedisSerializer.DATETIME_FORMAT, CultureInfo.InvariantCulture)).SequenceEqual(res));
		}

		private BasicRedisSerializer CreateSerializer()
		{
			return new BasicRedisSerializer(null);
		}
	}
}
