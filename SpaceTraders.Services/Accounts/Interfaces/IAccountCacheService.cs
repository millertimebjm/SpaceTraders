using SpaceTraders.Models;

namespace SpaceTraders.Services.Accounts.Interfaces;

public interface IAccountCacheService
{
    Task<Account> GetAsync();
    Task SetAsync(Account account);
}