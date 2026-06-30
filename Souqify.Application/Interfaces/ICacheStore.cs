

namespace Souqify.Application.Interfaces
{
    public interface ICacheStore
    {
        Task<T> GetDataAsync<T>(string key);

        public Task<bool> SetDataAsync<T>(string key, T value, TimeSpan expireDate);

        Task<bool> RemoveDataAsync(string key);
    }
}
