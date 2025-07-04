namespace SpaceTraders.Models;

public record NavigateResponse(
    string Symbol,
    Registration Registration,
    Nav Nav
);