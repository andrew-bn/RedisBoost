using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NBoosters.RedisBoost;
using NBoosters.RedisBoost.Core;
using NBoosters.RedisBoost.Core.Pipeline;
using NUnit.Framework;

namespace NBoosters.RedisBoost.Tests
{
	[TestFixture]
	public class RedisPipelineTests
	{
		private Mock<IRedisChannel> _redisChannel;
		private RedisResponse _response;
		[SetUp]
		public void Setup()
		{
			_response = RedisResponse.CreateStatus("OK", null);
			_redisChannel = new Mock<IRedisChannel>();
			_redisChannel.Setup(c => c.BufferIsEmpty)
						 .Returns(true);
			_redisChannel.Setup(c => c.SendAsync(It.IsAny<byte[][]>(), It.IsAny<Action<Exception>>()))
				.Callback((byte[][] request, Action<Exception> callback) => callback(null));
			_redisChannel.Setup(c => c.ReadResponseAsync(It.IsAny < Action<Exception, RedisResponse>>()))
				.Callback((Action<Exception, RedisResponse> callback)=>callback(null,_response));
			_redisChannel.Setup(c => c.Flush(It.IsAny < Action<Exception>>()))
				.Callback((Action<Exception> callback)=> callback(null));
		}
		[Test]
		public void ExecuteCommand_SendsRequestToRedis()
		{
			var req = new [] { new byte[]{1}  };
			var p = CreatePipeline();
			Action<Exception, RedisResponse> callBack = (e, r) => { };
			p.ExecuteCommandAsync(req, callBack);
			_redisChannel.Verify(c => c.SendAsync(req, It.IsAny<Action<Exception>>()));
		}
		[Test]
		public void ExecuteCommand_ReceivesResponse()
		{
			var req = new[] { new byte[] { 1 } };
			var p = CreatePipeline();
			p.ExecuteCommandAsync(req, (e, r) => { });
			_redisChannel.Verify(c => c.ReadResponseAsync(It.IsAny<Action<Exception, RedisResponse>>()));
		}
		[Test]
		public void ExecuteCommand_ReturnsValidResult()
		{
			var req = new[] { new byte[] { 1 } };
			var p = CreatePipeline();
			RedisResponse res = null;
			p.ExecuteCommandAsync(req, (e, r) => { res = r; });
			Assert.AreEqual(_response, res);
		}

		[Test]
		public void ExecuteCommand_SendFailed_ValidExceptionReturned()
		{
			var exception = new ApplicationException("Exception message");
			_redisChannel.Setup(c => c.SendAsync(It.IsAny<byte[][]>(), It.IsAny<Action<Exception>>()))
			.Callback((byte[][] request, Action<Exception> callback) => { callback(exception); });

			var req = new[] { new byte[] { 1 } };
			var p = CreatePipeline();
			Exception actual = null;
			p.ExecuteCommandAsync(req, (e, r) => { actual = e; });
			Assert.AreEqual(exception, actual);
		}

		[Test]
		public void ExecuteCommand_ReceivesFailed_ExceptionExpected()
		{
			Exception actual = null;
			_redisChannel.Setup(c => c.ReadResponseAsync(It.IsAny<Action<Exception, RedisResponse>>()))
				.Callback((Action<Exception, RedisResponse> callback) => { callback(new ApplicationException(),null );});

			var req = new[] { new byte[] { 1 } };
			var p = CreatePipeline();
			p.ExecuteCommandAsync(req, (e, r) => { actual = e; });
			Assert.IsTrue(actual is ApplicationException);
		}
		[Test]
		public void ExecuteCommand_AfterClosePipeline_ExceptionExpected()
		{
			var req = new[] { new byte[] { 1 } };
			Exception actual = null;
			var p = CreatePipeline();
			p.ClosePipeline();

			p.ExecuteCommandAsync(req, (e, r) => { actual = e; });
			Assert.IsTrue(actual is RedisException);
		}
		private RedisPipeline CreatePipeline()
		{
			return new RedisPipeline(_redisChannel.Object);
		}
	}
}
