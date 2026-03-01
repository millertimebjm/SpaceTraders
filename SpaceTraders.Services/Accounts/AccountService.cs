using SpaceTraders.Models;
using SpaceTraders.Services.Accounts.Interfaces;

namespace SpaceTraders.Services.Accounts;

public class AccountService(IAccountApiService _accountApiService, IAccountCacheService _accountCacheService) : IAccountService
{
    public async Task<Account> GetAsync()
    {
        return await _accountCacheService.GetAsync();
    }

    public async Task RegisterAsync()
    {
        var accountRegistrationResult = await _accountApiService.RegisterAsync();
        var account = new Account(accountRegistrationResult.Agent.Symbol, accountRegistrationResult.Token);
        await _accountCacheService.SetAsync(account);
    }

    public async Task SetAsync(Account account)
    {
        await _accountCacheService.SetAsync(account);
    }
}