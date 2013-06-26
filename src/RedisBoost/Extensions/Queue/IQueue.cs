#region Apache Licence, Version 2.0
/*
 Copyright 2013 Andrey Bulygin.

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

namespace RedisBoost.Extensions.Queue
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
