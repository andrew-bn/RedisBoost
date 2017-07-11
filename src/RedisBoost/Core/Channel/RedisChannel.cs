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
using System.Net;
using System.Threading.Tasks;
using RedisBoost.Misk;
using RedisBoost.Core.Pipeline;

namespace RedisBoost.Core.Channel
{
	internal class RedisChannel: IRedisChannel
	{
		private readonly IRedisPipeline _pipeline;
		private volatile ClientState _state;
		public ClientState State { get { return _state; }}

		public RedisChannel(IRedisPipeline pipeline)
		{
			_pipeline = pipeline;
		}

		#region read response
		public Task<MultiBulk> MultiBulkCommand(byte[][] args)
		{
			return ExecuteRedisCommand(args).ContinueWithIfNoError(
				t => t.Result.ResponseType != ResponseType.MultiBulk ? null : t.Result.AsMultiBulk());
		}

		public Task<string> StatusCommand(byte[][] args)
		{
			return ExecuteRedisCommand(args).ContinueWithIfNoError(
					t => t.Result.ResponseType != ResponseType.Status ? string.Empty : t.Result.AsStatus());
		}

		public Task<long> IntegerCommand(byte[][] args)
		{
			return ExecuteRedisCommand(args).ContinueWithIfNoError(t =>
					t.Result.ResponseType != ResponseType.Integer ? default(long) : t.Result.AsInteger());
		}

		public Task<long?> IntegerOrBulkNullCommand(byte[][] args)
		{
			return ExecuteRedisCommand(args).ContinueWithIfNoError(
				t => t.Result.ResponseType == ResponseType.Integer ? t.Result.AsInteger() : (long?)null);
		}

		public Task<Bulk> BulkCommand(params byte[][] args)
		{
			return ExecuteRedisCommand(args).ContinueWithIfNoError(
				t => t.Result.ResponseType != ResponseType.Bulk ? null : t.Result.AsBulk());
		}

		#endregion

		#region commands execution

		public Task<RedisResponse> ExecuteRedisCommand(byte[][] args)
		{
			var tcs = new TaskCompletionSource<RedisResponse>();
			_pipeline.ExecuteCommandAsync(args, (ex, r) => ProcessRedisResponse(tcs, ex, r));
			return tcs.Task;
		}

		public Task<RedisResponse> ReadDirectResponse()
		{
			var tcs = new TaskCompletionSource<RedisResponse>();
			_pipeline.ReadResponseAsync((ex, r) => ProcessRedisResponse(tcs, ex, r));
			return tcs.Task;
		}

		public Task SendDirectRequest(byte[][] args)
		{
			var tcs = new TaskCompletionSource<bool>();

			_pipeline.SendRequestAsync(args,
				(ex, r) =>
				{
					if (ex != null)
						tcs.SetException(ProcessException(ex));
					else tcs.SetResult(true);
				});

			return tcs.Task;
		}

		private void ProcessRedisResponse(TaskCompletionSource<RedisResponse> tcs, Exception ex, RedisResponse response)
		{
			if (ex != null)
				tcs.SetException(ProcessException(ex));
			else if (response == null)
				tcs.SetException(ProcessException(new ArgumentNullException(nameof(response))));
			else if (response.ResponseType == ResponseType.Error)
				tcs.SetException(new RedisException(response.AsError()));
			else
				tcs.SetResult(response);
		}

		#endregion

		#region connection
		public Task Connect(EndPoint endPoint)
		{
			var tcs = new TaskCompletionSource<bool>();
			_pipeline.OpenConnection(endPoint,
				ex =>
				{
					_state = ClientState.Connect;
					if (ex != null)
						tcs.SetException(ProcessException(ex));
					else
						tcs.SetResult(true);
				});
			return tcs.Task;
		}

		public Task Disconnect()
		{
			var tcs = new TaskCompletionSource<bool>();
			_pipeline.CloseConnection(
				ex =>
				{
					_state = ClientState.Disconnect;
					if (ex != null)
						tcs.SetException(ProcessException(ex));
					else
						tcs.SetResult(true);
				});
			return tcs.Task;
		}

		#endregion

		private Exception ProcessException(Exception ex)
		{
			DisposeIfFatalError(ex);
			if (!(ex is RedisException))
				ex = new RedisException(ex.Message, ex);
			return ex;
		}
		private void DisposeIfFatalError(Exception ex)
		{
			if (ex is RedisException) return;
			_state = ClientState.FatalError;
			Dispose();
		}

		public void EngageWith(AsyncSocket.ISocket socket)
		{
			_pipeline.EngageWith(socket);
		}

		public void EngageWith(Serialization.IRedisSerializer serializer)
		{
			_pipeline.EngageWith(serializer);
		}

		public void Dispose()
		{
			_pipeline.DisposeAndReuse();
		}

		public void ResetState()
		{
			_pipeline.ResetState();
		}

		public void SwitchToOneWay()
		{
			_pipeline.OneWayMode();
		}

		public void SetQuitState()
		{
			_state = ClientState.Quit;
		}
	}
}
