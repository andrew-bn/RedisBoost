using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBoosters.RedisBoost.ConsoleLoadTest
{
	class Program
	{
		private const string PAYLOAD =
			@"Chapter 1 – Down the Rabbit Hole: 
Alice is feeling bored while sitting on the riverbank with her sister, 
when she notices a talking, clothed White Rabbit with a pocket watch run past. 
She follows it down a rabbit hole when suddenly she falls a long way to a 
curious hall with many locked doors of all sizes. She finds a small key to a 
door too small for her to fit through, but through it she sees an attractive garden. 
She then discovers a bottle on a table labelled ""DRINK ME"", 
the contents of which cause her to shrink too small to reach the key which she 
has left on the table. A cake with ""EAT ME"" on it causes her to grow to 
such a tremendous size her head hits the ceiling.";

		private const string PAYLOAD2 =
			@"Chapter 2 – The Pool of Tears: 
Alice is unhappy and cries as her tears flood the hallway. 
After shrinking down again due to a fan she had picked up, 
Alice swims through her own tears and meets a Mouse, 
who is swimming as well. She tries to make small talk 
with him in elementary French (thinking he may be a French mouse) 
but her opening gambit ""Où est ma chatte?"" (that is ""Where is my cat?"") 
offends the mouse.";

		private static volatile bool _stop;
		private static volatile bool _running;
		private static IRedisClient _client;
		private static string ConnectionString
		{
			get { return ConfigurationManager.ConnectionStrings["Redis"].ConnectionString; }
		}
		static void Main(string[] args)
		{
			_client = RedisClient.ConnectAsync(ConnectionString).Result;
		
			Task.Run(()=>SyncMode());
			Task.Run(() => Pipelining());

			Console.Read();
			_stop = true;
			SpinWait.SpinUntil(() => !_running);
		}
		private static void Pipelining()
		{
			while (!_stop)
			{
				_client.SetAsync("someKey", PAYLOAD);
				_client.HSetAsync("otherKey", "field1", PAYLOAD);
				_client.HSetAsync("otherKey", "field2", PAYLOAD2);

				var getResult = _client.GetAsync("someKey");
				var hgetResult = _client.HGetAllAsync("otherKey");

				if (getResult.Result.As<string>() != PAYLOAD) Stop("GetAsync operation returned invalid result = " + getResult.Result.As<string>());
				if (PAYLOAD2 != hgetResult.Result[1].As<string>()) Stop("HGetAllAsync operation returned invalid result = " + hgetResult.Result[1].As<string>());
				if (PAYLOAD != hgetResult.Result[3].As<string>()) Stop("HGetAllAsync operation returned invalid result = " + hgetResult.Result[3].As<string>());
			}
		}
		private static void SyncMode()
		{
			_running = true;
			while (!_stop)
			{
				var setResult = _client.SetAsync("someKey", PAYLOAD).Result;
				if (setResult != "OK") Stop("SetAsync operation returned invalid result = "+setResult);
				var getResult = _client.GetAsync("someKey").Result.As<string>();
				if(getResult != PAYLOAD) Stop("GetAsync operation returned invalid result = " + getResult);

				_client.HSetAsync("otherKey", "field1", PAYLOAD).Wait();
				_client.HSetAsync("otherKey", "field2", PAYLOAD2).Wait();

				var hgetResult = _client.HGetAllAsync("otherKey").Result;
				if (PAYLOAD2 != hgetResult[1].As<string>()) Stop("HGetAllAsync operation returned invalid result = " + hgetResult[1].As<string>());
				if (PAYLOAD != hgetResult[3].As<string>()) Stop("HGetAllAsync operation returned invalid result = " + hgetResult[3].As<string>());
			}
			_running = false;
		}
		private static void Stop(string msg)
		{
			_stop = true;
			_running = false;
			Console.WriteLine(msg);
		}
	}
}
