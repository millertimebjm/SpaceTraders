namespace SpaceTraders.Models;

public record ServerStatus(ServerResets ServerResets);

public record ServerResets(DateTime Next, string Frequency);