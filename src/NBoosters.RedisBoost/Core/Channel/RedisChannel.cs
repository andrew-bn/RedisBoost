using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NBoosters.RedisBoost.Core.Pipeline;
using NBoosters.RedisBoost.Misk;

namespace NBoosters.RedisBoost.Core.Channel
{
	internal class RedisChannel: IRedisChannel
	{
		private readonly IRedisPipeline _pipeline;
		private volatile ClientState _state;

		public RedisChannel(IRedisPipeline pipeline)
		{
			_pipeline = pipeline;
		}

		#region read response
		public Task<MultiBulk> MultiBulkCommand(byte[][] args)
		{
			return ExecutePipelinedCommand(args).ContinueWithIfNoError(
				t => t.Result.ResponseType != ResponseType.MultiBulk ? null : t.Result.AsMultiBulk());
		}

		public Task<string> StatusCommand(byte[][] args)
		{
			return ExecutePipelinedCommand(args).ContinueWithIfNoError(
					t => t.Result.ResponseType != ResponseType.Status ? string.Empty : t.Result.AsStatus());
		}

		public Task<long> IntegerCommand(byte[][] args)
		{
			return ExecutePipelinedCommand(args).ContinueWithIfNoError(t =>
					t.Result.ResponseType != ResponseType.Integer ? default(long) : t.Result.AsInteger());
		}

		public Task<long?> IntegerOrBulkNullCommand(byte[][] args)
		{
			return ExecutePipelinedCommand(args).ContinueWithIfNoError(
				t => t.Result.ResponseType == ResponseType.Integer ? t.Result.AsInteger() : (long?)null);
		}

		public Task<Bulk> BulkCommand(params byte[][] args)
		{
			return ExecutePipelinedCommand(args).ContinueWithIfNoError(
				t => t.Result.ResponseType != ResponseType.Bulk ? null : t.Result.AsBulk());
		}

		#endregion

		#region commands execution

		private Task<RedisResponse> ExecutePipelinedCommand(params byte[][] args)
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
	}
}
