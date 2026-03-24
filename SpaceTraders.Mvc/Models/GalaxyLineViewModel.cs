using SpaceTraders.Models;

namespace SpaceTraders.Mvc.Models;

public record GalaxyLineViewModel(
    int StartX,
    int StartY,
    int EndX,
    int EndY,
    string Color,
    int Width,
    int[] Dotted,
    string Start,
    string End
);