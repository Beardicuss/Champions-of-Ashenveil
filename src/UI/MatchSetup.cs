namespace ChampionsOfAshenveil.UI;

/// <summary>
/// Data container passed between scenes via SceneManager.
/// Holds player hero selections and match seed.
/// </summary>
public class MatchSetup
{
    public string Player0HeroId { get; set; } = "deadwood";
    public string Player1HeroId { get; set; } = "accursed";
    public ulong  Seed          { get; set; } = 12345UL;
    public string Player0Name   { get; set; } = "Player 1";
    public string Player1Name   { get; set; } = "Player 2";
}
