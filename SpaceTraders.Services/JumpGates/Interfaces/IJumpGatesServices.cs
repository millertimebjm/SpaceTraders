using SpaceTraders.Models;

namespace SpaceTraders.Services.JumpGates.Interfaces;

public interface IJumpGatesServices
{
    Task<JumpGate> GetAsync(string symbol);
}