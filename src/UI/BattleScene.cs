using Godot;
using System.Diagnostics;
using ChampionsOfAshenveil.Engine;
using ChampionsOfAshenveil.Sim;
using ChampionsOfAshenveil.Engine.Items;

namespace ChampionsOfAshenveil.UI;

/// <summary>
/// Playable Battle scene.
/// Drives a turn-based match: shows unit panels, ability buttons, and event log.
/// Player 0 is human-controlled; Player 1 uses RandomBot.
/// </summary>
public partial class BattleScene : Control
{
    // ── Node refs ────────────────────────────────────────────────────────────
    private Label?         _turnLabel;
    private Label?         _phaseLabel;
    private RichTextLabel? _eventLog;
    private VBoxContainer? _p0UnitPanel;
    private VBoxContainer? _p1UnitPanel;
    private CreepLane?     _p0Creeps;
    private CreepLane?     _p1Creeps;
    private HeroProgressionPanel? _progressionPanel;
    private VBoxContainer? _actionPanel;
    private Button?        _btnEndTurn;
    private ItemHandPanel? _itemHand;
    private Panel?         _victoryOverlay;
    private Label?         _victoryLabel;

    // ── State ────────────────────────────────────────────────────────────────
    private GameState?   _gameState;
    private GameEngine?  _engine;

    private SeededRng?   _botRng;
    private string?      _selectedCasterId;
    private string?      _selectedAbilityId;
    private bool         _waitingForTarget;

    private const int HumanPlayerId = 0;
    private const int BotPlayerId   = 1;
    private readonly Stopwatch _matchClock = new();
    private readonly Stopwatch _turnClock = new();
    private int _humanTurns;
    private double _humanTurnSeconds;
    private int _humanActions;

    public override void _Ready()
    {
        BindNodes();
        InitMatch();
        RefreshUI();
    }

    // ─── Setup ───────────────────────────────────────────────────────────────

    private void BindNodes()
    {
        _turnLabel      = GetNode<Label>("TopBar/TurnLabel");
        _phaseLabel     = GetNode<Label>("TopBar/PhaseLabel");
        _eventLog       = GetNode<RichTextLabel>("BottomPanel/EventLog");
        _p0UnitPanel    = GetNode<VBoxContainer>("BattleArea/P0Side/UnitPanel");
        _p1UnitPanel    = GetNode<VBoxContainer>("BattleArea/P1Side/UnitPanel");
        _p0Creeps       = GetNode<CreepLane>("BattleArea/P0Side/CreepLane");
        _p1Creeps       = GetNode<CreepLane>("BattleArea/P1Side/CreepLane");
        _progressionPanel = GetNode<HeroProgressionPanel>("BattleArea/P0Side/ProgressionPanel");
        _progressionPanel.UpgradeRequested += (heroId, abilityId) =>
            ApplyActionAndRefresh(new UpgradeAbilityAction(HumanPlayerId, heroId, abilityId));
        _actionPanel    = GetNode<VBoxContainer>("ActionPanel");
        _btnEndTurn     = GetNode<Button>("ActionPanel/BtnEndTurn");
        _itemHand       = GetNode<ItemHandPanel>("ActionPanel/ItemHandPanel");
        _itemHand.PlayRequested += (itemId, targetId) =>
            ApplyActionAndRefresh(new PlayCardAction(HumanPlayerId, itemId, targetId));
        _victoryOverlay = GetNode<Panel>("VictoryOverlay");
        _victoryLabel   = GetNode<Label>("VictoryOverlay/VBox/VictoryLabel");

        _btnEndTurn!.Pressed += OnEndTurnPressed;
        _victoryOverlay!.Visible = false;
    }

    private void InitMatch()
    {
        var gm    = GameManager.Instance;
        var setup = SceneManager.Instance.PendingSetup ?? new MatchSetup();
        ulong seed = setup.Seed;

        _engine = new GameEngine(gm.Data, seed);
        _botRng = new SeededRng(seed ^ 0xABCDEF);
        var heroIds = gm.Data.Heroes.Keys.OrderBy(id => id).ToList();
        List<string> Team(string chosen) => new[] { chosen }.Concat(heroIds.Where(id => id != chosen).Take(2)).ToList();
        var deck = PrototypeItemDeck.Build(gm.Data);

        var config = new MatchConfig
        {
            Players =
            [
                new PlayerConfig { PlayerId = HumanPlayerId, Name = setup.Player0Name, HeroIds = Team(setup.Player0HeroId), DeckItemIds = deck },
                new PlayerConfig { PlayerId = BotPlayerId,   Name = setup.Player1Name, HeroIds = Team(setup.Player1HeroId), DeckItemIds = deck }
            ]
        };
        _gameState = MatchBuilder.BuildInitialState(config, gm.Data, seed);
        _matchClock.Start();
        _turnClock.Start();
        Log($"[color=cyan]═══  Champions of Ashenveil  ═══[/color]");
        Log($"[color=white]{setup.Player0Name}[/color] [color=gray]vs[/color] [color=tomato]{setup.Player1Name}[/color]");
    }

    // ─── UI Refresh ──────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (_gameState is null) return;

        _turnLabel!.Text  = $"Turn {_gameState.TurnNumber}";
        var self = _gameState.GetPlayer(HumanPlayerId);
        _phaseLabel!.Text = $"{(_gameState.ActivePlayer == HumanPlayerId ? "Your Turn" : "Enemy Turn")} | Gold {self.Gold} | Cards {self.Hand.Count}";

        BuildUnitPanel(_p0UnitPanel!, _gameState.GetPlayer(HumanPlayerId), HumanPlayerId);
        BuildUnitPanel(_p1UnitPanel!, _gameState.GetPlayer(BotPlayerId),   BotPlayerId);
        _p0Creeps!.Render(_gameState.GetPlayer(HumanPlayerId));
        _p1Creeps!.Render(_gameState.GetPlayer(BotPlayerId));
        _progressionPanel!.Render(_gameState.GetPlayer(HumanPlayerId), GameManager.Instance.Data,
            _gameState.ActivePlayer == HumanPlayerId && !_gameState.IsGameOver);
        BuildActionPanel();
    }

    private void BuildUnitPanel(VBoxContainer panel, PlayerState player, int playerId)
    {
        foreach (Node child in panel.GetChildren()) child.QueueFree();

        foreach (var unit in player.Units.Where(u => u.IsHero))
        {
            var row = new HBoxContainer();

            var nameLabel = new Label { Text = unit.Id.Split('_').Last().Capitalize() };
            nameLabel.CustomMinimumSize = new Vector2(90, 0);

            var hpBar = new ProgressBar
            {
                MinValue = 0, MaxValue = unit.MaxHp, Value = unit.CurrentHp,
                CustomMinimumSize = new Vector2(120, 22),
                ShowPercentage = false
            };
            hpBar.TooltipText = $"{unit.CurrentHp}/{unit.MaxHp} HP";

            var energyBar = new ProgressBar
            {
                MinValue = 0, MaxValue = Math.Max(unit.MaxEnergy, 1), Value = unit.CurrentEnergy,
                CustomMinimumSize = new Vector2(80, 22),
                ShowPercentage = false
            };
            energyBar.AddThemeColorOverride("font_color", new Color(0.3f, 0.6f, 1.0f));
            energyBar.TooltipText = $"{unit.CurrentEnergy}/{unit.MaxEnergy} Energy";

            var statusLabel = new Label
            {
                Text = BuildStatusText(unit),
                CustomMinimumSize = new Vector2(100, 0)
            };
            statusLabel.AddThemeFontSizeOverride("font_size", 11);

            row.AddChild(nameLabel);
            row.AddChild(hpBar);
            row.AddChild(energyBar);
            row.AddChild(statusLabel);

            if (unit.IsDead)
                row.Modulate = new Color(0.4f, 0.4f, 0.4f);

            // Ability select buttons for human player's units
            if (playerId == HumanPlayerId && !unit.IsDead && unit.ActionsTakenThisTurn == 0 && _gameState!.ActivePlayer == HumanPlayerId)
            {
                var abilities = GameManager.Instance.Data.Heroes.GetValueOrDefault(unit.DefinitionId)?.AbilityIds ?? [];
                foreach (var abilityId in abilities)
                {
                    var abilityDef = GameManager.Instance.Data.Abilities.GetValueOrDefault(abilityId);
                    if (abilityDef is null) continue;
                    if (unit.AbilityRanks.GetValueOrDefault(abilityId) <= 0) continue;
                    if (abilityDef.Adapted.Targeting == "passive") continue;

                    int cd = unit.GetAbilityCooldown(abilityId);
                    bool canAfford = unit.CurrentEnergy >= abilityDef.Adapted.EnergyCost;

                    var btn = new Button
                    {
                        Text = cd > 0
                            ? $"{abilityDef.Name} {unit.AbilityRanks.GetValueOrDefault(abilityId)} [{cd}]"
                            : $"{abilityDef.Name} {unit.AbilityRanks.GetValueOrDefault(abilityId)} ({abilityDef.Adapted.EnergyCost}E)",
                        Disabled = cd > 0 || !canAfford || unit.IsSilenced,
                        TooltipText = $"Cost: {abilityDef.Adapted.EnergyCost} energy | CD: {cd} turns"
                    };
                    string capturedUnitId    = unit.Id;
                    string capturedAbilityId = abilityId;
                    string capturedTargeting = abilityDef.Adapted.Targeting;

                    btn.Pressed += () => OnAbilityButtonPressed(capturedUnitId, capturedAbilityId, capturedTargeting);
                    row.AddChild(btn);
                }
            }

            // Attack button for human player's units
            if (playerId == HumanPlayerId && !unit.IsDead && unit.ActionsTakenThisTurn == 0 && _gameState!.ActivePlayer == HumanPlayerId && !unit.IsDisarmed)
            {
                var enemies = _gameState!.GetPlayer(BotPlayerId).AliveUnits.ToList();
                if (enemies.Count > 0)
                {
                    var atkBtn = new Button
                    {
                        Text = "⚔ Attack",
                        Modulate = new Color(1.0f, 0.5f, 0.5f)
                    };
                    string capturedAttacker = unit.Id;
                    atkBtn.Pressed += () => BuildAttackTargetButtons(capturedAttacker);
                    row.AddChild(atkBtn);
                }
            }

            panel.AddChild(row);
        }
    }

    private static string BuildStatusText(UnitState unit)
    {
        if (unit.IsDead) return "[DEAD]";
        var parts = new List<string>();
        if (unit.IsStunned)  parts.Add("STUN");
        if (unit.IsSilenced) parts.Add("SIL");
        if (unit.IsDisarmed) parts.Add("DISARM");
        if (unit.Effects.Count > 0 && parts.Count == 0) parts.Add($"{unit.Effects.Count} fx");
        return string.Join(" ", parts);
    }

    private void BuildActionPanel()
    {
        if (_gameState is null) return;
        _btnEndTurn!.Disabled = _gameState.IsGameOver;
        _btnEndTurn!.Text     = _gameState.ActivePlayer == HumanPlayerId ? "End Turn →" : "⏳ Enemy thinking...";
        _itemHand!.Render(_gameState, HumanPlayerId, GameManager.Instance.Data,
            _gameState.ActivePlayer == HumanPlayerId && !_gameState.IsGameOver);
    }

    // ─── Actions ─────────────────────────────────────────────────────────────

    private void OnAttackPressed(string attackerId, string targetId)
    {
        if (_gameState is null || _engine is null || _gameState.ActivePlayer != HumanPlayerId) return;

        var action = new AttackAction(HumanPlayerId, attackerId, targetId);
        ApplyActionAndRefresh(action);
    }

    private void BuildAttackTargetButtons(string attackerId)
    {
        if (_actionPanel is null || _gameState is null) return;
        foreach (Node n in _actionPanel.GetChildren())
            if (n.Name.ToString().StartsWith("TargetBtn_")) n.QueueFree();
        int index = 0;
        foreach (var enemy in _gameState.GetPlayer(BotPlayerId).AliveUnits)
        {
            var button = new Button { Name = $"TargetBtn_Attack_{index++}",
                Text = $"Attack {enemy.DefinitionId.Replace('_', ' ')} ({enemy.CurrentHp} HP)" };
            string targetId = enemy.Id;
            button.Pressed += () => OnAttackPressed(attackerId, targetId);
            _actionPanel.AddChild(button);
        }
    }

    private void OnAbilityButtonPressed(string casterId, string abilityId, string targeting)
    {
        if (_gameState is null || _engine is null || _gameState.ActivePlayer != HumanPlayerId) return;

        if (targeting is "self" or "self_area" or "passive")
        {
            var action = new UseAbilityAction(HumanPlayerId, casterId, abilityId, null);
            ApplyActionAndRefresh(action);
        }
        else
        {
            // Need a target — highlight enemies and wait
            _selectedCasterId  = casterId;
            _selectedAbilityId = abilityId;
            _waitingForTarget  = true;
            Log("[color=yellow]Select a target (click an enemy unit name below)...[/color]");
            BuildEnemyTargetButtons(casterId, abilityId);
        }
    }

    private void BuildEnemyTargetButtons(string casterId, string abilityId)
    {
        if (_actionPanel is null || _gameState is null) return;

        // Remove old target buttons
        foreach (Node n in _actionPanel.GetChildren())
            if (n.Name.ToString().StartsWith("TargetBtn_")) n.QueueFree();

        var enemies = _gameState.GetPlayer(BotPlayerId).AliveUnits.ToList();
        foreach (var enemy in enemies)
        {
            var btn = new Button
            {
                Name = $"TargetBtn_{enemy.Id}",
                Text = $"🎯 {enemy.Id.Split('_').Last().Capitalize()} ({enemy.CurrentHp}HP)",
                Modulate = new Color(1.0f, 0.3f, 0.3f)
            };
            string capturedTarget = enemy.Id;
            btn.Pressed += () =>
            {
                var action = new UseAbilityAction(HumanPlayerId, casterId, abilityId, capturedTarget);
                ApplyActionAndRefresh(action);
                _waitingForTarget = false;
                foreach (Node n in _actionPanel.GetChildren())
                    if (n.Name.ToString().StartsWith("TargetBtn_")) n.QueueFree();
            };
            _actionPanel.AddChild(btn);
        }
    }

    private void OnEndTurnPressed()
    {
        if (_gameState is null || _engine is null) return;
        if (_gameState.IsGameOver) return;
        _humanTurns++;
        _humanTurnSeconds += _turnClock.Elapsed.TotalSeconds;
        _turnClock.Reset();

        // Advance turn
        var advResult = _engine.AdvanceTurn(_gameState);
        _gameState = advResult.NewState;
        LogEvents(advResult.Events);

        if (_gameState.IsGameOver) { ShowVictory(); return; }

        RefreshUI();

        // If now bot's turn — run bot action
        if (_gameState.ActivePlayer == BotPlayerId)
            CallDeferred(MethodName.RunBotTurn);
    }

    private void RunBotTurn()
    {
        if (_gameState is null || _engine is null || _botRng is null) return;

        for (int i = 0; i < 18; i++)
        {
            var upgrade = ProgressionBot.ChooseUpgrade(_gameState, GameManager.Instance.Data);
            if (upgrade is null) break;
            ApplyActionAndRefresh(upgrade);
        }

        for (int i = 0; i < 3 && !_gameState.IsGameOver; i++)
        {
            var action = Sim.RandomBot.ChooseAction(_gameState, _botRng, GameManager.Instance.Data);
            if (action is PassAction) break;
            ApplyActionAndRefresh(action);
        }

        for (int i = 0; i < 2 && !_gameState.IsGameOver; i++)
        {
            var card = ItemCardBot.Choose(_gameState, GameManager.Instance.Data);
            if (card is null) break;
            ApplyActionAndRefresh(card);
        }

        // Bot ends its own turn
        if (!_gameState.IsGameOver)
        {
            var advResult = _engine.AdvanceTurn(_gameState);
            _gameState = advResult.NewState;
            LogEvents(advResult.Events);
            RefreshUI();
            _turnClock.Restart();
        }

        if (_gameState.IsGameOver) ShowVictory();
    }

    private void ApplyActionAndRefresh(GameAction action)
    {
        if (_gameState is null || _engine is null) return;

        var result = _engine.Apply(_gameState, action);
        if (action.PlayerId == HumanPlayerId && !result.Events.Any(e => e is InvalidActionEvent) &&
            action is AttackAction or UseAbilityAction or ChangeRowAction) _humanActions++;
        _gameState = result.NewState;
        LogEvents(result.Events);
        RefreshUI();

        if (_gameState.IsGameOver) ShowVictory();
    }

    // ─── Victory ─────────────────────────────────────────────────────────────

    private void ShowVictory()
    {
        if (_victoryOverlay is null || _gameState is null) return;
        _victoryOverlay.Visible = true;
        _matchClock.Stop();
        var winner = _gameState.Players.FirstOrDefault(p => p.PlayerId == _gameState.WinnerId);
        _victoryLabel!.Text = winner?.PlayerId == HumanPlayerId
            ? "🏆 Victory!"
            : "💀 Defeated.";
        Log($"[color=gold]═══  MATCH OVER — {_victoryLabel.Text}  ═══[/color]");
        var player = _gameState.GetPlayer(HumanPlayerId);
        Log($"[color=cyan]Playtest: {_gameState.TurnNumber} rounds | {_matchClock.Elapsed.TotalMinutes:F1} min | {_humanTurns} turns | {(_humanTurns == 0 ? 0 : _humanTurnSeconds / _humanTurns):F1}s avg turn | {_humanActions} Hero actions | {player.CardsPlayed} cards | {player.CreepKills} creep kills | gold +{player.GoldEarned}/-{player.GoldSpent} (farm {player.GoldFromFarming})[/color]");
    }

    // ─── Event log ───────────────────────────────────────────────────────────

    private void LogEvents(IReadOnlyList<GameEvent> events)
    {
        foreach (var e in events)
            Log(ColourEvent(e));
    }

    private static string ColourEvent(GameEvent e) => e switch
    {
        DamageEvent d   when d.WasLethal  => $"[color=red]💀 {Esc(d.Description)}[/color]",
        DamageEvent d                    => $"[color=tomato]{Esc(d.Description)}[/color]",
        HealEvent h                      => $"[color=lime]{Esc(h.Description)}[/color]",
        EnergyRestoredEvent er           => $"[color=aquamarine]{Esc(er.Description)}[/color]",
        CardPlayedEvent cp               => $"[color=yellow]{Esc(cp.Description)}[/color]",
        AbilityUsedEvent a               => $"[color=aquamarine]✨ {Esc(a.Description)}[/color]",
        EffectAppliedEvent ef            => $"[color=plum]{Esc(ef.Description)}[/color]",
        EffectExpiredEvent ex            => $"[color=gray]{Esc(ex.Description)}[/color]",
        UnitDiedEvent u                  => $"[color=red][b]☠ {Esc(u.Description)}[/b][/color]",
        GameOverEvent g                  => $"[color=gold][b]🏆 {Esc(g.Description)}[/b][/color]",
        TurnStartEvent t                 => $"[color=white]─── {Esc(t.Description)} ───[/color]",
        _                                => $"[color=gray]{Esc(e.Description)}[/color]"
    };

    private static string Esc(string s) => s.Replace("[", "〔").Replace("]", "〕");

    private void Log(string bbcode) => _eventLog?.AppendText($"\n{bbcode}");
}
