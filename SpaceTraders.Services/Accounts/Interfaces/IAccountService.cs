using SpaceTraders.Models;

namespace SpaceTraders.Services.Accounts.Interfaces;

public interface IAccountService
{
    Task<Account> GetAsync();
    Task RegisterAsync();
    Task SetAsync(Account account);
}