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
using System.Globalization;
using System.Text;
using RedisBoost.Core;

namespace RedisBoost.Misk
{
	internal static class DataConvertionEnxtensions
	{
		public static Exception UnwrapAggregation(this Exception ex)
		{
			var aggrException = ex as AggregateException;
			if (aggrException != null)
				ex = aggrException.Flatten().InnerException;
			return ex;
		}

		internal static bool IsErrorReply(this byte firstByte)
		{
			return firstByte == RedisConstants.Minus;
		}
		internal static bool IsBulkReply(this byte firstByte)
		{
			return firstByte == RedisConstants.Dollar;
		}
		internal static bool IsMultiBulkReply(this byte firstByte)
		{
			return firstByte == RedisConstants.Asterix;
		}
		internal static bool IsIntReply(this byte firstByte)
		{
			return firstByte == RedisConstants.Colon;
		}
		internal static bool IsStatusReply(this byte firstByte)
		{
			return firstByte == RedisConstants.Plus;
		}
		internal static int ToInt(this string value)
		{
			return int.Parse(value, CultureInfo.InvariantCulture);
		}
		internal static long ToLong(this string value)
		{
			return long.Parse(value, CultureInfo.InvariantCulture);
		}
		internal static string AsString(this byte[] value)
		{
			return Encoding.UTF8.GetString(value);
		}
		internal static byte[] ToBytes(this string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}
		internal static byte[] ToBytes(this int value)
		{
			return ToBytes(value.ToString(CultureInfo.InvariantCulture));
		}
		internal static byte[] ToBytes(this long value)
		{
			return ToBytes(value.ToString(CultureInfo.InvariantCulture));
		}
		internal static byte[] ToBytes(this double value)
		{
			return ToBytes(value.ToString("R", CultureInfo.InvariantCulture));
		}
		internal static byte[] ToBytes(this BitOpType value)
		{
			var result = RedisConstants.And;
			if (value == BitOpType.Not)
				result = RedisConstants.Not;
			else if (value == BitOpType.Or)
				result = RedisConstants.Or;
			else if (value == BitOpType.Xor)
				result = RedisConstants.Xor;

			return result;
		}
		internal static byte[] ToBytes(this Subcommand subcommand)
		{
			var result = RedisConstants.RefCount;
			if (subcommand == Subcommand.IdleTime)
				result = RedisConstants.IdleTime;
			else if (subcommand == Subcommand.Encoding)
				result = RedisConstants.ObjEncoding;

			return result;
		}
		internal static byte[] ToBytes(this Aggregation aggregation)
		{
			var aggr = RedisConstants.Sum;
			if (aggregation == Aggregation.Max)
				aggr = RedisConstants.Max;
			if (aggregation == Aggregation.Min)
				aggr = RedisConstants.Min;
			return aggr;
		}
	}
}
