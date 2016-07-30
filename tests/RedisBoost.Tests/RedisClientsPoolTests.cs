using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RedisBoost.Core.Pool;
using RedisBoost.Core.Serialization;

namespace RedisBoost.Tests
{
	[TestClass]
	public class RedisClientsPoolTests
	{
		private Mock<IPooledRedisClient> _redisClient;
		private Func<RedisConnectionStringBuilder, BasicRedisSerializer, IPooledRedisClient> _clientsFactory;
		private string _connectionString;
		public RedisClientsPoolTests()
		{
			_connectionString = "data source=127.0.0.1";

			_redisClient = new Mock<IPooledRedisClient>();
			_redisClient.Setup(c => c.PrepareClientConnection())
						.Returns(Task<IRedisClient>.Factory.StartNew(() => { return _redisClient.Object; }));
			_redisClient.Setup(c => c.QuitAsync())
						.Returns(Task<string>.Factory.StartNew(() => { return "OK"; }));
			_redisClient.Setup(c => c.ConnectionString)
						.Returns(_connectionString);
			_redisClient.Setup(c => c.State).Returns(ClientState.Connect);
			_clientsFactory = (sb, s) => _redisClient.Object;

		}
		[TestMethod]
		public void CreateClient_PoolIsEmtpy_CallsFactoryToCreateClient()
		{
			var connectionStringBuilder = new RedisConnectionStringBuilder(_connectionString);
			var factoryWasCalled = false;
			_clientsFactory = (sb, s) =>
				{
					factoryWasCalled = sb == connectionStringBuilder;
					return _redisClient.Object;
				};
			//act
			CreatePool().CreateClientAsync(connectionStringBuilder).Wait();
			//assert
			Assert.IsTrue(factoryWasCalled);
		}
		[TestMethod]
		[ExpectedException(typeof(Exception))]
		public void CreateClient_PoolIsEmtpy_FactoryThrowsException()
		{
			var connectionStringBuilder = new RedisConnectionStringBuilder(_connectionString);
			_clientsFactory = (sb, s) => { throw new Exception("some exception"); };
			//act
			CreatePool().CreateClientAsync(connectionStringBuilder).Wait();
		}
		[TestMethod]
		public void CreateClient_PoolIsEmtpy_PreparesClientConnection()
		{
			CreatePool().CreateClientAsync(_connectionString).Wait();
			//assert
			_redisClient.Verify(c => c.PrepareClientConnection());
		}

		[TestMethod]
		public void CreateClient_Twice_CallsFactoryToCreateClient()
		{
			var factoryWasCalled = 0;
			_clientsFactory = (sb, s) =>
			{
				factoryWasCalled++;
				return _redisClient.Object;
			};
			//act
			CreatePool().CreateClientAsync(_connectionString).Wait();
			CreatePool().CreateClientAsync(_connectionString).Wait();
			//assert
			Assert.AreEqual(2, factoryWasCalled);
		}
		[TestMethod]
		public void ReturnClient_StatusIsQuit_DestroysClient()
		{
			_redisClient.Setup(c => c.State).Returns(ClientState.Quit);
			CreatePool().ReturnClient(_redisClient.Object);
			_redisClient.Verify(c => c.Destroy());
		}
		[TestMethod]
		public void ReturnClient_StatusIsDisconnect_DestroysClient()
		{
			_redisClient.Setup(c => c.State).Returns(ClientState.Disconnect);
			CreatePool().ReturnClient(_redisClient.Object);
			_redisClient.Verify(c => c.Destroy());
		}
		[TestMethod]
		public void ReturnClient_StatusIsSubscription_DestroysClient()
		{
			_redisClient.Setup(c => c.State).Returns(ClientState.Subscription);
			CreatePool().ReturnClient(_redisClient.Object);
			_redisClient.Verify(c => c.Destroy());
		}
		[TestMethod]
		public void ReturnClient_StatusIsFatalError_DestroysClient()
		{
			_redisClient.Setup(c => c.State).Returns(ClientState.FatalError);
			CreatePool().ReturnClient(_redisClient.Object);
			_redisClient.Verify(c => c.Destroy());
		}
		[TestMethod]
		public void TimeoutExpired_QuitCommandCalled()
		{
			CreatePool(timeout: 100).ReturnClient(_redisClient.Object);
			Thread.Sleep(1000);
			_redisClient.Verify(c => c.QuitAsync());
		}
		[TestMethod]
		public void TimeoutExpired_DestroyCalled()
		{
			CreatePool(timeout: 100).ReturnClient(_redisClient.Object);
			Thread.Sleep(1000);
			_redisClient.Verify(c => c.Destroy());
		}

		[TestMethod]
		public void TimeoutExpired_DestroyExceptionOccured_NextClientCreatesWithoutExceptions()
		{
			_redisClient.Setup(c => c.Destroy()).Throws(new Exception("some exception"));
			var pool = CreatePool(timeout: 100);
			pool.ReturnClient(_redisClient.Object);
			Thread.Sleep(1000);
			_redisClient.Verify(c => c.Destroy());
			var cli = pool.CreateClientAsync(_connectionString).Result;
			Assert.IsNotNull(cli);
		}

		[TestMethod]
		public void DisposePool_QuitCommandCalled()
		{
			var pool = CreatePool();
			pool.ReturnClient(_redisClient.Object);
			pool.Dispose();
			_redisClient.Verify(c => c.QuitAsync());
		}
		[TestMethod]
		public void DisposePool_DestroyCalled()
		{
			var pool = CreatePool();
			pool.ReturnClient(_redisClient.Object);
			pool.Dispose();
			_redisClient.Verify(c => c.Destroy());
		}
		[TestMethod]
		public void ReturnClient_AfterPoolDispose_QuitCalled()
		{
			var pool = CreatePool();
			pool.Dispose();
			pool.ReturnClient(_redisClient.Object);
			_redisClient.Verify(c => c.QuitAsync());
		}

		[TestMethod]
		public void ReturnClient_PoolIsOversized_QuitCalled()
		{
			var pool = CreatePool(maxPoolSize: 1);
			pool.ReturnClient(_redisClient.Object);
			pool.ReturnClient(_redisClient.Object);
			_redisClient.Verify(c => c.QuitAsync());
		}
		[TestMethod]
		public void ReturnClient_PoolIsOversized_DestroyCalled()
		{
			var pool = CreatePool(maxPoolSize: 100);
			for (int i = 0; i < 100; i++)
				pool.ReturnClient(_redisClient.Object);
			pool.ReturnClient(_redisClient.Object);
			_redisClient.Verify(c => c.Destroy(), Times.Once());
		}
		[TestMethod]
		public void ReturnClient_AfterPoolDispose_DestroyExpected()
		{
			var pool = CreatePool();
			pool.Dispose();
			pool.ReturnClient(_redisClient.Object);
			_redisClient.Verify(c => c.Destroy());
		}
		[TestMethod]
		public void QuitClient_OperationTimeout_DestroyIsCalled()
		{
			_redisClient.Setup(c => c.QuitAsync())
				.Returns(Task<string>.Factory.StartNew(() =>
				{
					Thread.Sleep(1000000);
					return "";
				}));

			var pool = CreatePool();
			pool.Dispose();
			pool.ReturnClient(_redisClient.Object);
			_redisClient.Verify(c => c.Destroy());
		}
		[TestMethod]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void ReturnClient_AfterPoolDispose()
		{
			var pool = CreatePool();
			pool.Dispose();
			pool.CreateClientAsync(_connectionString).Wait();
		}
		private RedisClientsPool CreatePool(int timeout = 1000, int maxPoolSize = 2, int quitTimeout = 5000)
		{
			return new RedisClientsPool(maxPoolSize, timeout, quitTimeout, _clientsFactory);
		}

	}
}

