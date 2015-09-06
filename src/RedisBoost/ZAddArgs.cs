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

namespace RedisBoost
{
	public struct ZAddArgs
	{
		internal object Member;
		internal bool IsArray;
		internal long IntScore;
		internal double DoubleScore;
		internal bool UseIntValue;

		public ZAddArgs(long score, byte[] member)
		{
			UseIntValue = true;
			IntScore = score;
			DoubleScore = 0;
			Member = member;
			IsArray = true;
		}

		public ZAddArgs(double score, byte[] member)
		{
			UseIntValue = false;
			IntScore = 0;
			DoubleScore = score;
			Member = member;
			IsArray = true;
		}
		public ZAddArgs(long score, object member)
		{
			UseIntValue = true;
			IntScore = score;
			DoubleScore = 0;
			Member = member;
			IsArray = false;
		}

		public ZAddArgs(double score, object member)
		{
			UseIntValue = false;
			IntScore = 0;
			DoubleScore = score;
			Member = member;
			IsArray = false;
		}
	}
}
