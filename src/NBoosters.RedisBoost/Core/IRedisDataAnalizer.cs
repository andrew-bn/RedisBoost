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

namespace NBoosters.RedisBoost.Core
{
	internal interface IRedisDataAnalizer
	{
		int ConvertToInt(string data);
		long ConvertToLong(string data);
		bool IsErrorReply(byte firstByte);

		string ConvertToString(byte[] line, int startIndex);

		byte[] ConvertToByteArray(string value);

		byte[] ConvertToByteArray(int value);
		byte[] ConvertToByteArray(long value);

		bool IsBulkReply(byte firstByte);

		bool IsIntReply(byte firstByte);

		bool IsStatusReply(byte firstByte);

		bool IsMultiBulkReply(byte firstByte);
	}
}
