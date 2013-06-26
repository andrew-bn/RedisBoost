using System;
using Moq;
using NUnit.Framework;
using RedisBoost.Core.Pipeline;
using RedisBoost.Core.Receiver;
using RedisBoost.Core.Sender;

namespace RedisBoost.Tests.Core
{
	[TestFixture]
	public class RedisPipelineTests
	{
		private Mock<IRedisSender> _redisSender;
		private Mock<IRedisReceiver> _redisReceiver;
		private RedisResponse _response;
		[SetUp]
		public void Setup()
		{
			_response = RedisResponse.CreateStatus("OK", null);
			_redisSender = new Mock<IRedisSender>();
			_redisSender.Setup(c => c.BytesInBuffer).Returns(0);
			_redisSender.Setup(c => c.Send(It.IsAny<SenderAsyncEventArgs>()))
				.Callback((SenderAsyncEventArgs args) => args.Completed(args)).Returns(true);
			_redisSender.Setup(c => c.Flush(It.IsAny<SenderAsyncEventArgs>()))
				.Callback((SenderAsyncEventArgs args) =>
				{
					args.Completed(args);
				}).Returns(true);


			_redisReceiver = new Mock<IRedisReceiver>();

			_redisReceiver.Setup(c => c.Receive(It.IsAny<ReceiverAsyncEventArgs>()))
				.Callback((ReceiverAsyncEventArgs args) =>
					{
						args.Response = _response;
						args.Completed(args);
					}).Returns(true);
			
		}
		[Test]
		public void ExecuteCommand_SendsRequestToRedis()
		{
			var req = new[] { new byte[] { 1 } };
			var p = CreatePipeline();
			Action<Exception, RedisResponse> callBack = (e, r) => { };
			p.ExecuteCommandAsync(req, callBack);
			_redisSender.Verify(c => c.Send(It.IsAny<SenderAsyncEventArgs>()));
		}
		[Test]
		public void ExecuteCommand_ReceivesResponse()
		{
			var req = new[] { new byte[] { 1 } };
			var p = CreatePipeline();
			p.ExecuteCommandAsync(req, (e, r) => { });
			_redisReceiver.Verify(c => c.Receive(It.IsAny<ReceiverAsyncEventArgs>()));
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
			_redisSender.Setup(c => c.Send(It.IsAny<SenderAsyncEventArgs>()))
			.Callback((SenderAsyncEventArgs args) =>
				{
					args.Error = exception;
					args.Completed(args);
				}).Returns(true);

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
			_redisReceiver.Setup(c => c.Receive(It.IsAny<ReceiverAsyncEventArgs>()))
				.Callback((ReceiverAsyncEventArgs args) =>
					{
						args.Error = new ApplicationException();
						args.Completed(args);
					}).Returns(true);

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
			p.OneWayMode();

			p.ExecuteCommandAsync(req, (e, r) => { actual = e; });
			Assert.IsTrue(actual is RedisException);
		}
		private RedisPipeline CreatePipeline()
		{
			return new RedisPipeline(null,_redisSender.Object,_redisReceiver.Object);
		}
	}
}
