using RedisBoost.Misk;
using Xunit;

namespace RedisBoost.Tests.Core
{
	public class BuffersPoolTests
	{
		private const int MaxPoolSize = 10;
		private const int BufferSize = 1000;
		[Fact]
		public static void TryGet_SetUpsBuffer()
		{
			byte[] result;
			CreatePool().TryGet(out result, null);

			Assert.NotNull(result);
			Assert.Equal(BufferSize, result.Length);
		}
		[Fact]
		public static void TryGet_HasBuffer_ReturnsTrue()
		{
			byte[] result;
			var success = CreatePool().TryGet(out result, null);

			Assert.True(success);
		}
		[Fact]
		public static void TryGet_PoolIsFull_ReturnsFalse()
		{
			byte[] result;
			var success = CreatePool(0).TryGet(out result, null);

			Assert.False(success);
		}
		[Fact]
		public static void TryGet_PoolIsFull_ItemReturned_CallbackCalled()
		{
			byte[] result;
			bool called = false;
			var pool = CreatePool(0);

			pool.TryGet(out result, (b) => { called = true; });

			pool.Release(new byte[BufferSize]);
			Assert.True(called);
		}
		[Fact]
		public static void TryGet_PoolIsFull_CallbackNotCalled()
		{
			byte[] result;
			bool called = false;
			var pool = CreatePool(0);

			pool.TryGet(out result, (b) => { called = true; });
			Assert.False(called);
		}
		private static BuffersPool CreatePool(int poolSize = MaxPoolSize)
		{
			return new BuffersPool(BufferSize, poolSize);
		}
	}
}
