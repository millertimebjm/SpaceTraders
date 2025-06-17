
namespace SpaceTraders
{
    class Program
    {
        static void Main(string[] args)
        {
            ISpaceTrader spaceTraderService = new SpaceTraderService();
            spaceTraderService.Run();
        }
    }
}