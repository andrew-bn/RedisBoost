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
using RedisBoost.Core.Serialization;

namespace RedisBoost
{
	public abstract class RedisResponse
	{
		private class ErrorResponse : RedisResponse
		{
			public string Message { get; private set; }

			public ErrorResponse(string message, IRedisSerializer serializer)
				: base(ResponseType.Error, serializer)
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
				: base(RedisBoost.ResponseType.Status, serializer)
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
				: base(RedisBoost.ResponseType.Integer, serializer)
			{
				Value = value;
			}
			public override string ToString()
			{
				return Value.ToString();
			}
		}

		internal RedisResponse(ResponseType responseType, IRedisSerializer serializer)
		{
			ResponseType = responseType;
			Serializer = serializer;
		}

		public ResponseType ResponseType { get; private set; }
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
				case ResponseType.Bulk:
					return value.AsBulk().Value;
				case ResponseType.Integer:
					return serializer.Serialize(value.AsInteger());
				case ResponseType.Status:
					return serializer.Serialize(value.AsStatus());
				default:
					throw new InvalidCastException("Unable to cast RedisResponse to byte[]. Response type: " + value.ResponseType);
			}
		}
	}
}
