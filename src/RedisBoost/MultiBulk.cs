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
using System.Collections.Generic;
using System.Linq;
using RedisBoost.Core.Serialization;

namespace RedisBoost
{
	public class MultiBulk : RedisResponse, IEnumerable<RedisResponse>
	{
		internal RedisResponse[] Parts { get; private set; }

		public bool IsNull
		{
			get { return Parts == null; }
		}

		internal MultiBulk(RedisResponse[] parts, IRedisSerializer serializer)
			: base(ResponseType.MultiBulk, serializer)
		{
			Parts = parts;
		}

		public int Length
		{
			get
			{
				ThrowIfMultiBulkNull();
				return Parts.Length;
			}
		}
		public RedisResponse this[int index]
		{
			get
			{
				ThrowIfMultiBulkNull();
				return Parts[index];
			}
		}
		public T[] AsArray<T>()
		{
			return IsNull? null: Parts.Select(p => p.As<T>()).ToArray();
		}

		public static implicit operator byte[][](MultiBulk value)
		{
			return value.IsNull? null: ToArray<byte[]>(value.Parts);
		}
		private static T[] ToArray<T>(RedisResponse[] response)
		{
			var result = new T[response.Length];
			for (int i = 0; i < result.Length; i++)
			{
				if (response[i].ResponseType != ResponseType.Bulk)
					throw new InvalidCastException("MultiBulk reply contains non bulk parts. Unable to convert non bulk part to " + typeof(T).Name);
				result[i] = response[i].AsBulk().As<T>();
			}
			return result;
		}

		public IEnumerator<RedisResponse> GetEnumerator()
		{
			ThrowIfMultiBulkNull();
			return ((IEnumerable<RedisResponse>)Parts).GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private void ThrowIfMultiBulkNull()
		{
			if (IsNull)
				throw new RedisException("This is NULL multi-bulk reply");
		}
	}
}