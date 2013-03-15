using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBoosters.RedisBoost.Extensions.Queue
{
	public interface IQueue<T>
	{
		int Count { get; }
		/// <summary>
		/// Adds value to queue
		/// </summary>
		/// <param name="value"></param>
		/// <returns>Queue size</returns>
		int Enqueue(T value);
		/// <summary>
		/// Dequeues value
		/// </summary>
		/// <returns></returns>
		T Dequeue();
		/// <summary>
		/// Dequeues value. If queue is empty false returned
		/// </summary>
		/// <param name="result"></param>
		/// <returns></returns>
		bool TryDequeue(out T result);
		/// <summary>
		/// Get an element on the top of the queue without dequeuing it
		/// </summary>
		/// <returns></returns>
		T Peek();
		/// <summary>
		/// Peeks an element from queue. If queue is empty false returned
		/// </summary>
		/// <param name="result"></param>
		/// <returns></returns>
		bool TryPeek(out T result);

		void Clear();
	}
}
