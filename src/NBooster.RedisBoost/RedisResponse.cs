namespace NBooster.RedisBoost
{
	public abstract class RedisResponse
	{
		private class ErrorRedisResponse : RedisResponse
		{
			public string Message { get; private set; }

			public ErrorRedisResponse(string message)
				: base(RedisResponseType.Error)
			{
				Message = message;
			}
			public override string ToString()
			{
				return Message;
			}
		}
		private class StatusRedisResponse : RedisResponse
		{
			public string Status { get; private set; }

			public StatusRedisResponse(string status)
				: base(RedisResponseType.Status)
			{
				Status = status;
			}
			public override string ToString()
			{
				return Status;
			}
		}
		private class IntegerRedisResponse : RedisResponse
		{
			public long Value { get; private set; }

			public IntegerRedisResponse(long value)
				: base(RedisResponseType.Integer)
			{
				Value = value;
			}
			public override string ToString()
			{
				return Value.ToString();
			}
		}
		private class BulkRedisResponse : RedisResponse
		{
			public byte[] Value { get; private set; }

			public BulkRedisResponse(byte[] value)
				: base(RedisResponseType.Bulk)
			{
				Value = value;
			}
			public override string ToString()
			{
				return Value.ToString();
			}
		}
		private class MultiBulkRedisResponse : RedisResponse
		{
			public RedisResponse[] Parts { get; private set; }

			public MultiBulkRedisResponse(RedisResponse[] parts)
				: base(RedisResponseType.MultiBulk)
			{
				Parts = parts;
			}
		}

		internal RedisResponse(RedisResponseType responseType)
		{
			ResponseType = responseType;
		}

		public RedisResponseType ResponseType { get; private set; }

		internal static RedisResponse CreateError(string message)
		{
			return new ErrorRedisResponse(message);
		}
		internal static RedisResponse CreateStatus(string status)
		{
			return new StatusRedisResponse(status);
		}
		internal static RedisResponse CreateInteger(long value)
		{
			return new IntegerRedisResponse(value);
		}
		internal static RedisResponse CreateBulk(byte[] value)
		{
			return new BulkRedisResponse(value);
		}
		internal static RedisResponse CreateMultiBulk(RedisResponse[] value)
		{
			return new MultiBulkRedisResponse(value);
		}

		public string AsError()
		{
			return ((ErrorRedisResponse) this).Message;
		}
		public string AsStatus()
		{
			return ((StatusRedisResponse)this).Status;
		}
		public long AsInteger()
		{
			return ((IntegerRedisResponse)this).Value;
		}
		public byte[] AsBulk()
		{
			return ((BulkRedisResponse)this).Value;
		}
		public RedisResponse[] AsMultiBulk()
		{
			return ((MultiBulkRedisResponse)this).Parts;
		}
	}

}
