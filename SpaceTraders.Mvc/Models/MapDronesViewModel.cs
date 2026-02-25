// const drones = [
//         { x: 400, y: 400, targetIdx: 1, speed: 3, size: 12 },
//         { x: 1000, y: 1000, targetIdx: 2, speed: 1.5, size: 8 }
//     ]; *@

namespace SpaceTraders.Mvc.Models;

public record MapDronesViewModel(
    int X,
    int Y,
    string Label,
    int Size
);