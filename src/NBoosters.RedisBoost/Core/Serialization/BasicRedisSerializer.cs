using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.Core.Serialization
{
	internal class BasicRedisSerializer : IRedisSerializer
	{
		internal const string DATETIME_FORMAT = "yyyy-MM-dd'T'HH:mm:ss.fffffff";

		static readonly byte[] _null = new byte[0];
		private readonly IRedisSerializer _objectsSerializer;

		public BasicRedisSerializer(IRedisSerializer objectsSerializer)
		{
			_objectsSerializer = objectsSerializer;
		}


		public byte[] Serialize<T>(T value)
		{
			if (value == null)
				return _null;
			if (typeof(T) == typeof(string))
				return Serialize(value.ToString());
			if (typeof(T) == typeof(byte[]))
				return value as byte[];
			if (typeof(T).IsEnum)
				return Serialize(value.ToString());
			if (typeof (T) == typeof (DateTime))
				return Serialize((value as IFormattable).ToString(DATETIME_FORMAT, CultureInfo.InvariantCulture));

			var convertible = value as IConvertible;
			if (convertible != null)
				return Serialize(convertible.ToString(CultureInfo.InvariantCulture));

			return _objectsSerializer.Serialize(value);
		}

		private static byte[] Serialize(string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}
	}
}
