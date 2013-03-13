using System;
using System.Collections.Generic;
using System.Linq;
using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost
{
	public class MultiBulk : RedisResponse, IEnumerable<RedisResponse>
	{
		internal RedisResponse[] Parts { get; private set; }

		internal MultiBulk(RedisResponse[] parts, IRedisSerializer serializer)
			: base(RedisResponseType.MultiBulk, serializer)
		{
			Parts = parts;
		}
		public int Length
		{
			get { return Parts.Length; }
		}
		public RedisResponse this[int index]
		{
			get { return Parts[index]; }
		}
		public T[] AsArray<T>()
		{
			return Parts.Select(p=>p.As<T>()).ToArray();
		}
		public static implicit operator byte[][](MultiBulk value)
		{
			return ToArray<byte[]>(value.Parts);
		}
		private static T[] ToArray<T>(RedisResponse[] response)
		{
			var result = new T[response.Length];
			for (int i = 0; i < result.Length; i++)
			{
				if (response[i].ResponseType != RedisResponseType.Bulk)
					throw new InvalidCastException("MultiBulk reply contains non bulk parts. Unable to convert non bulk part to "+typeof(T).Name);
				result[i] = response[i].AsBulk().As<T>();
			}
			return result;
		}

		public IEnumerator<RedisResponse> GetEnumerator()
		{
			return ((IEnumerable<RedisResponse>)Parts).GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}