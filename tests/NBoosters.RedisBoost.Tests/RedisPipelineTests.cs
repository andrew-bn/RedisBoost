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

namespace NBooster.RedisBoost.Tests
{
	[TestFixture]
	public class RedisPipelineTests
	{
		private Mock<IRedisChannel> _redisChannel;
		private RedisResponse _response;
		[SetUp]
		public void Setup()
		{
			_response = RedisResponse.CreateStatus("OK");
			_redisChannel = new Mock<IRedisChannel>();
			_redisChannel.Setup(c => c.BufferIsEmpty)
			             .Returns(true);
			_redisChannel.Setup(c => c.SendAsync(It.IsAny<byte[][]>()))
			             .Returns(Task.Run(() => { ; }));
			_redisChannel.Setup(c => c.ReadResponseAsync())
						 .Returns(Task.Run(() => _response));
			_redisChannel.Setup(c => c.Flush())
			             .Returns(Task.Run(() => { ; }));
		}
		[Test]
		public void ExecuteCommand_SendsRequestToRedis()
		{
			var req = new byte[] {1};
			var p = CreatePipeline();
			p.ExecuteCommandAsync(req).Wait();
			_redisChannel.Verify(c=>c.SendAsync(req));
		}
		[Test]
		public void ExecuteCommand_ReceivesResponse()
		{
			var req = new byte[] { 1 };
			var p = CreatePipeline();
			p.ExecuteCommandAsync(req).Wait();
			_redisChannel.Verify(c => c.ReadResponseAsync());
		}
		[Test]
		public void ExecuteCommand_ReturnsValidResult()
		{
			var req = new byte[] { 1 };
			var p = CreatePipeline();
			var res = p.ExecuteCommandAsync(req).Result;
			Assert.AreEqual(_response, res);
		}

		[Test]
		[ExpectedException(typeof(AggregateException))]
		public void ExecuteCommand_SendFailed_ValidExceptionReturned()
		{
			var exception = new ApplicationException("Exception message");
			_redisChannel.Setup(c => c.SendAsync(It.IsAny<byte[][]>()))
			             .Returns(Task.Run(() => { throw exception; }));
			var req = new byte[] { 1 };
			var p = CreatePipeline();
			p.ExecuteCommandAsync(req).Wait();
		}

		[Test]
		[ExpectedException(typeof(AggregateException))]
		public void ExecuteCommand_ReceivesFailed_ExceptionExpected()
		{
			_redisChannel.Setup(c => c.ReadResponseAsync())
					 .Returns(Task.Run(() => { throw new Exception();return _response;}));
			var req = new byte[] { 1 };
			var p = CreatePipeline();
			p.ExecuteCommandAsync(req).Wait();
		}
		[Test]
		[ExpectedException(typeof(AggregateException))]
		public void ExecuteCommand_AfterClosePipeline_ExceptionExpected()
		{
			var req = new byte[] { 1 };
			var p = CreatePipeline();
			p.ClosePipeline();
			p.ExecuteCommandAsync(req).Wait();
		}
		private RedisPipeline CreatePipeline()
		{
			return new RedisPipeline(_redisChannel.Object);
		}
	}
}
