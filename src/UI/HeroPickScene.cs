using Godot;

namespace ChampionsOfAshenveil.UI;

/// <summary>
/// Hero selection screen.
/// Both players pick one hero from the roster, then start the match.
/// </summary>
public partial class HeroPickScene : Control
{
	private Label?         _p0Label;
	private Label?         _p1Label;
	private HBoxContainer? _p0Grid;
	private HBoxContainer? _p1Grid;
	private Button?        _btnStart;

	private string _p0SelectedHero = "deadwood";
	private string _p1SelectedHero = "accursed";

	public override void _Ready()
	{
		_p0Label  = GetNode<Label>("VBox/P0Row/P0Label");
		_p1Label  = GetNode<Label>("VBox/P1Row/P1Label");
		_p0Grid   = GetNode<HBoxContainer>("VBox/P0Row/P0Grid");
		_p1Grid   = GetNode<HBoxContainer>("VBox/P1Row/P1Grid");
		_btnStart = GetNode<Button>("VBox/BtnStart");

		_btnStart!.Pressed += OnStartPressed;

		BuildHeroGrid(_p0Grid!, 0);
		BuildHeroGrid(_p1Grid!, 1);
		UpdateLabels();
	}

	private void BuildHeroGrid(HBoxContainer grid, int playerId)
	{
		var heroes = GameManager.Instance.Data.Heroes;
		foreach (var (id, def) in heroes.OrderBy(h => h.Key))
		{
			var btn = new Button
			{
				Text              = def.Name,
				CustomMinimumSize = new Vector2(110, 48),
				TooltipText       = $"HP: {def.Adapted.MaxHp}  Energy: {def.Adapted.MaxEnergy}\nDMG: {def.Adapted.BaseDamageMin}-{def.Adapted.BaseDamageMax}\nArmor: {def.Adapted.Armor}"
			};
			string capturedId = id;
			btn.Pressed += () => OnHeroPicked(playerId, capturedId);
			grid.AddChild(btn);
		}
	}

	private void OnHeroPicked(int playerId, string heroId)
	{
		if (playerId == 0)
		{
			_p0SelectedHero = heroId;
			var def = GameManager.Instance.Data.Heroes[heroId];
			_p0Label!.Text = $"Player 1: {def.Name}";
		}
		else
		{
			_p1SelectedHero = heroId;
			var def = GameManager.Instance.Data.Heroes[heroId];
			_p1Label!.Text = $"Player 2: {def.Name}";
		}
	}

	private void UpdateLabels()
	{
		var heroes = GameManager.Instance.Data.Heroes;
		_p0Label!.Text = $"Player 1: {heroes.GetValueOrDefault(_p0SelectedHero)?.Name ?? _p0SelectedHero}";
		_p1Label!.Text = $"Player 2: {heroes.GetValueOrDefault(_p1SelectedHero)?.Name ?? _p1SelectedHero}";
	}

	private void OnStartPressed()
	{
		SceneManager.Instance.StartCustomMatch(new MatchSetup
		{
			Player0HeroId = _p0SelectedHero,
			Player1HeroId = _p1SelectedHero,
			Player0Name   = "Player 1",
			Player1Name   = "Player 2 (Bot)",
			Seed          = (ulong)Time.GetTicksMsec()
		});
	}
}
