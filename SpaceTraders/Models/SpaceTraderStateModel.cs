

namespace SpaceTraders.Models
{
    public class SpaceTraderStateModel
    {
        public User User { get; set; }
        public bool Complete { get; set; } = false;
        public string Status { get; set; } = "";
        public string Token { get; set; } = "";
    }
}