#region Apache Licence, Version 2.0
/*
 Copyright 2013 Andrey Bulygin.

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

using System.Text;

namespace NBoosters.RedisBoost.Core
{
	internal class RedisDataAnalizer : IRedisDataAnalizer
	{
		public int ConvertToInt(string data)
		{
			return int.Parse(data);
		}
		public long ConvertToLong(string data)
		{
			return long.Parse(data);
		}
		public bool IsErrorReply(byte firstByte)
		{
			return firstByte == RedisConstants.Minus;
		}
		public bool IsBulkReply(byte firstByte)
		{
			return firstByte == RedisConstants.Dollar;
		}
		public bool IsMultiBulkReply(byte firstByte)
		{
			return firstByte == RedisConstants.Asterix;
		}
		public bool IsIntReply(byte firstByte)
		{
			return firstByte == RedisConstants.Colon;
		}
		public bool IsStatusReply(byte firstByte)
		{
			return firstByte == RedisConstants.Plus;
		}
		
		public string ConvertToString(byte[] line, int startIndex)
		{
			return Encoding.UTF8.GetString(line, startIndex, line.Length - startIndex);
		}

		public byte[] ConvertToByteArray(string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}

		public byte[] ConvertToByteArray(int value)
		{
			return ConvertToByteArray(value.ToString());
		}
		public byte[] ConvertToByteArray(long value)
		{
			return ConvertToByteArray(value.ToString());
		}
	}
}
