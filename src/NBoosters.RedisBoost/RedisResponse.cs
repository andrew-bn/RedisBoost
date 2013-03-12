using System;
using System.Collections.Generic;
using System.Linq;
using NBoosters.RedisBoost.Core.Serialization;

namespace NBoosters.RedisBoost
{
	public abstract class RedisResponse
	{
		private class ErrorResponse : RedisResponse
		{
			public string Message { get; private set; }

			public ErrorResponse(string message, IRedisSerializer serializer)
				: base(RedisResponseType.Error, serializer)
			{
				Message = message;
			}
			public override string ToString()
			{
				return Message;
			}
		}
		private class StatusResponse : RedisResponse
		{
			public string Status { get; private set; }

			public StatusResponse(string status, IRedisSerializer serializer)
				: base(RedisResponseType.Status, serializer)
			{
				Status = status;
			}
			public override string ToString()
			{
				return Status;
			}
		}
		private class IntegerResponse : RedisResponse
		{
			public long Value { get; private set; }

			public IntegerResponse(long value, IRedisSerializer serializer)
				: base(RedisResponseType.Integer, serializer)
			{
				Value = value;
			}
			public override string ToString()
			{
				return Value.ToString();
			}
		}

		internal RedisResponse(RedisResponseType responseType, IRedisSerializer serializer)
		{
			ResponseType = responseType;
			Serializer = serializer;
		}

		public RedisResponseType ResponseType { get; private set; }
		internal IRedisSerializer Serializer { get; private set; }

		internal static RedisResponse CreateError(string message, IRedisSerializer serializer)
		{
			return new ErrorResponse(message, serializer);
		}
		internal static RedisResponse CreateStatus(string status, IRedisSerializer serializer)
		{
			return new StatusResponse(status, serializer);
		}
		internal static RedisResponse CreateInteger(long value, IRedisSerializer serializer)
		{
			return new IntegerResponse(value, serializer);
		}
		internal static RedisResponse CreateBulk(byte[] value, IRedisSerializer serializer)
		{
			return new Bulk(value, serializer);
		}
		internal static RedisResponse CreateMultiBulk(RedisResponse[] value, IRedisSerializer serializer)
		{
			return new MultiBulk(value, serializer);
		}

		public string AsError()
		{
			return ((ErrorResponse)this).Message;
		}
		public string AsStatus()
		{
			return ((StatusResponse)this).Status;
		}
		public long AsInteger()
		{
			return ((IntegerResponse)this).Value;
		}
		public Bulk AsBulk()
		{
			return (Bulk)this;
		}
		public MultiBulk AsMultiBulk()
		{
			return (MultiBulk)this;
		}

		public T As<T>()
		{
			return (T)Serializer.Deserialize(typeof(T), this);
		}
		public static implicit operator byte[](RedisResponse value)
		{
			var serializer = value.Serializer;
			switch (value.ResponseType)
			{
				case RedisResponseType.Bulk:
					return value.AsBulk().Value;
				case RedisResponseType.Integer:
					return serializer.Serialize(value.AsInteger());
				case RedisResponseType.Status:
					return serializer.Serialize(value.AsStatus());
				default:
					throw new InvalidCastException("Unable to cast RedisResponse to byte[]. Response type: " + value.ResponseType);
			}
		}
	}
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
					throw new InvalidCastException("MultiBulk reply contains non bulk parts. Unable to convert non bulk part to byte[]");
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

	public class Bulk : RedisResponse
	{
		public byte[] Value { get; private set; }

		public Bulk(byte[] value, IRedisSerializer serializer)
			: base(RedisResponseType.Bulk, serializer)
		{
			Value = value;
		}
		public int Length
		{
			get { return Value==null?0:Value.Length; }
		}
		public override string ToString()
		{
			return Value.ToString();
		}
	}

}
