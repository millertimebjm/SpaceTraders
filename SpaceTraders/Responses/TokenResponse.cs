

namespace SpaceTraders.Models
{
    public class TokenResponse : IResponse
    {
        public string Token { get; set; }
        public User User { get; set; }
    }
}