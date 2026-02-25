// { x: 400, y: 400, label: "(3, W, F, J)", color: "#00d4ff" },
//         { x: 1600, y: 400, label: "(12, X, R, P)", color: "#ff0077" },
//         { x: 1000, y: 1600, label: "(7, G, M, S)", color: "#ccff00" },
//         { x: 1000, y: 1000, label: "CENTRAL HUB", color: "#ffffff" }

namespace SpaceTraders.Mvc.Models;

public record MapStaticObjectsViewModel(
    int X,
    int Y,
    string Label,
    string Color
);