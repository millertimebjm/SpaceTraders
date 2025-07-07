namespace SpaceTraders.Models;

public record SurveyResult(
    Cooldown Cooldown,
    IEnumerable<Survey> Surveys
);