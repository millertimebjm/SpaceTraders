using SpaceTraders.Models;
using SpaceTraders.Models.Results;

namespace SpaceTraders.Services.Accounts.Interfaces;

public interface IAccountApiService
{
    //Task<Account> GetAsync();
    Task<AccountRegistrationResult> RegisterAsync();
}