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

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace RedisBoost.Core.Serialization
{
	public class BasicRedisSerializer : IRedisSerializer
	{
		internal static readonly byte[] Null = new byte[] { 0 };
		internal const string DatetimeFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff";
		private static readonly ConcurrentDictionary<Type, DataContractSerializer> _basicSerializers = new ConcurrentDictionary<Type, DataContractSerializer>();

		#region serializatoin
		public virtual byte[] Serialize(object value)
		{
			if (value == null)return Null;

			var type = value.GetType();

			if (type == typeof(string))
				return SerializeString(value.ToString());
			if (type == typeof(byte[]))
				return value as byte[];
			if (type.IsEnum)
				return SerializeString(value.ToString());
			if (type == typeof(DateTime))
				return SerializeString((value as IFormattable).ToString(DatetimeFormat, CultureInfo.InvariantCulture));
			if (type == typeof(Guid))
				return SerializeString(value.ToString());
			if (type == typeof (int) || type == typeof (long) || type == typeof (byte) || type == typeof (short) ||
			    type == typeof (uint) || type == typeof (ulong) || type == typeof (sbyte) || type == typeof (ushort) ||
			    type == typeof (bool) || type == typeof (decimal) || type == typeof (double) || type == typeof(char))
				return SerializeString((value as IConvertible).ToString(CultureInfo.InvariantCulture));

			var result = SerializeComplexValue(type, value);

			if (result.SequenceEqual(Null))
				throw new SerializationException("Serializer returned unexpected result. byte[]{0} value is reserved for NULL");

			return result;
		}

		protected virtual byte[] SerializeComplexValue(Type type, object value)
		{
			var serializer = _basicSerializers.GetOrAdd(type, t => new DataContractSerializer(t));

			using (var ms = new MemoryStream())
			{
				serializer.WriteObject(ms,value);
				return ms.ToArray();
			}
		}
		#endregion
		#region deserialization
		public virtual object Deserialize(Type type, byte[] value)
		{
			if (value == null || value.SequenceEqual(Null)) return null;

			if (type == typeof(string))
				return DeserializeToString(value);
			if (type == typeof(byte[]))
				return value;
			if (type.IsEnum)
				return DeserializeToEnum(DeserializeToString(value), type);
			if (type == typeof (DateTime))
				return DateTime.ParseExact(DeserializeToString(value), DatetimeFormat, CultureInfo.InvariantCulture);
			if (type == typeof (Guid))
				return Guid.Parse(DeserializeToString(value));
			if (type == typeof(int))
				return int.Parse(DeserializeToString(value), CultureInfo.InvariantCulture);
			if (type == typeof(long))
				return long.Parse(DeserializeToString(value), CultureInfo.InvariantCulture);
			if (type == typeof(byte))
				return byte.Parse(DeserializeToString(value), CultureInfo.InvariantCulture);
			if (type == typeof(short))
				return short.Parse(DeserializeToString(value), CultureInfo.InvariantCulture);
			if (type == typeof(uint))
				return uint.Parse(DeserializeToString(value), CultureInfo.InvariantCulture);
			if (type == typeof(ulong))
				return ulong.Parse(DeserializeToString(value), CultureInfo.InvariantCulture);
			if (type == typeof(sbyte))
				return sbyte.Parse(DeserializeToString(value), CultureInfo.InvariantCulture);
			if (type == typeof(ushort))
				return ushort.Parse(DeserializeToString(value), CultureInfo.InvariantCulture);
			if (type == typeof(bool))
				return bool.Parse(DeserializeToString(value));
			if (type == typeof(decimal))
				return decimal.Parse(DeserializeToString(value), CultureInfo.InvariantCulture);
			if (type == typeof(double))
				return double.Parse(DeserializeToString(value), CultureInfo.InvariantCulture);
			if (type == typeof(char))
				return DeserializeToString(value)[0];

			return DeserializeComplexValue(type, value);
		}
		protected virtual object DeserializeComplexValue(Type type, byte[] value)
		{
			var serializer = _basicSerializers.GetOrAdd(type, t => new DataContractSerializer(t));
			using (var ms = new MemoryStream(value))
			{
				return serializer.ReadObject(ms);
			}
		}
		#endregion
		private static object DeserializeToEnum(string value, Type enumType)
		{
			try
			{
				return Enum.Parse(enumType, value, true);
			}
			catch (Exception ex)
			{
				throw new SerializationException("Invalid enum value. Enum type: " + enumType.Name,ex);
			}
		}
		private static byte[] SerializeString(string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}
		private static string DeserializeToString(byte[] value)
		{
			return Encoding.UTF8.GetString(value);
		}
	}
}
