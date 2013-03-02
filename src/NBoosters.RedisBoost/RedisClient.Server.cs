using System.Threading.Tasks;
using NBoosters.RedisBoost.Core;

namespace NBoosters.RedisBoost
{
	public partial class RedisClient
	{
		public Task<string> FlushDbAsync()
		{
			return StatusResponseCommand(RedisConstants.FlushDb);
		}
		public Task<string> FlushAllAsync()
		{
			return StatusResponseCommand(RedisConstants.FlushAll);
		}
		public Task<string> BgRewriteAofAsync()
		{
			return StatusResponseCommand(RedisConstants.BgRewriteAof);
		}
		public Task<string> BgSaveAsync()
		{
			return StatusResponseCommand(RedisConstants.BgSave);
		}
		public Task<byte[]> ClientListAsync()
		{
			return BulkResponseCommand(RedisConstants.Client,RedisConstants.List);
		}
		internal Task<string> ClientSetNameAsync(byte[] connectionName)
		{
			return StatusResponseCommand(RedisConstants.Client, RedisConstants.SetName, connectionName);
		}
		internal Task<byte[]> ClientGetNameAsync()
		{
			return BulkResponseCommand(RedisConstants.Client, RedisConstants.GetName);
		}
		public Task<long> DbSizeAsync()
		{
			return IntegerResponseCommand(RedisConstants.DbSize);
		}
		public Task<RedisResponse> ConfigGetAsync(string parameter)
		{
			return SendCommandAndReadResponse(RedisConstants.Config,RedisConstants.Get,ConvertToByteArray(parameter));
		}
		public Task<string> ConfigSetAsync(string parameter,byte[] value)
		{
			return StatusResponseCommand(RedisConstants.Config, RedisConstants.Set, ConvertToByteArray(parameter), value);
		}
		public Task<string> ConfigResetStatAsync()
		{
			return StatusResponseCommand(RedisConstants.Config, RedisConstants.ResetStat);
		}
		public Task<string> ClientKillAsync(string ip, int port)
		{
			return StatusResponseCommand(RedisConstants.Client, RedisConstants.Kill,
				ConvertToByteArray(string.Format("{0}:{1}",ip,port)));
		}
		public Task<byte[]> InfoAsync()
		{
			return InfoAsync(null);
		}

		public Task<byte[]> InfoAsync(string section)
		{
			return section == null
					? BulkResponseCommand(RedisConstants.Info)
					: BulkResponseCommand(RedisConstants.Info, ConvertToByteArray(section));
		}
		public Task<long> LastSaveAsync()
		{
			return IntegerResponseCommand(RedisConstants.LastSave);
		}
		public Task<string> SaveAsync()
		{
			return StatusResponseCommand(RedisConstants.Save);
		}
		public Task<string> ShutDownAsync()
		{
			return StatusResponseCommand(RedisConstants.ShutDown);
		}
		public Task<string> ShutDownAsync(bool save)
		{
			return StatusResponseCommand(RedisConstants.ShutDown,save?RedisConstants.Save:RedisConstants.NoSave);
		}
		public Task<string> SlaveOfAsync(string host,int port)
		{
			return StatusResponseCommand(RedisConstants.SlaveOf,ConvertToByteArray(host), ConvertToByteArray(port));
		}
		public Task<byte[][]> TimeAsync()
		{
			return MultiBulkResponseCommand(RedisConstants.Time);
		}
	}
}
