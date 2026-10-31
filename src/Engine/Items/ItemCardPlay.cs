using ChampionsOfAshenveil.Engine.Data;
using ChampionsOfAshenveil.Engine.Effects;

namespace ChampionsOfAshenveil.Engine.Items;

/// <summary>Plays HoN-derived equipment and consumable item cards from the hand.</summary>
public static class ItemCardPlay
{
    public static GameState Apply(GameState state, PlayCardAction action, DataRegistry data, List<GameEvent> events)
    {
        var player = state.GetPlayer(action.PlayerId);
        var target = action.TargetId is null ? null : state.FindUnit(action.TargetId);
        if (player.CardsPlayedThisTurn >= 2 || !player.Hand.Contains(action.CardId) ||
            !data.Items.TryGetValue(action.CardId, out var item) || player.Gold < item.Cost ||
            target is null || target.IsDead || target.PlayerId != action.PlayerId)
            return Reject(state, events, "Hand, gold, card limit, or allied target invalid.");

        GameState updated;
        if (item.CardType == "consumable")
        {
            if (!data.Effects.TryGetValue(item.UseEffectId, out var effect))
                return Reject(state, events, "Consumable effect missing.");
            if (item.Id == "mana_potion" && target.MaxEnergy <= 0)
                return Reject(state, events, "Target does not use energy.");
            if (EffectQuery.IsImmune(target, effect))
                return Reject(state, events, "Target is immune.");
            var (applied, effectEvents) = EffectApplicator.Apply(target, effect, state.TurnNumber, target.Id);
            updated = StateUtils.UpdateUnit(state, applied);
            events.AddRange(effectEvents);
        }
        else if (item.CardType == "equipment")
        {
            if (!target.IsHero || target.ItemIds.Count >= 2 || item.StatBonuses.Count == 0)
                return Reject(state, events, "Equipment requires an allied Hero with an empty slot.");
            updated = StateUtils.UpdateUnit(state, ItemApplicator.Apply(target, item));
        }
        else return Reject(state, events, "Unsupported item card type.");

        var hand = player.Hand.ToList();
        hand.Remove(action.CardId);
        updated = updated with { Players = updated.Players.Select(p => p.PlayerId == action.PlayerId
            ? p with { Gold = p.Gold - item.Cost, GoldSpent = p.GoldSpent + item.Cost,
                CardsPlayed = p.CardsPlayed + 1, CardsPlayedThisTurn = p.CardsPlayedThisTurn + 1,
                Hand = hand, DiscardPile = [.. p.DiscardPile, action.CardId] }
            : p).ToList() };
        events.Add(new GoldEvent(state.TurnNumber, $"Player {action.PlayerId} spends {item.Cost} gold.", action.PlayerId, item.Cost, true));
        events.Add(new CardPlayedEvent(state.TurnNumber,
            $"Player {action.PlayerId} uses {item.Name} on {target.Id}.", action.PlayerId, item.Id, target.Id));
        return updated;
    }

    private static GameState Reject(GameState state, List<GameEvent> events, string reason)
    {
        events.Add(new InvalidActionEvent(state.TurnNumber, "Card play invalid.", reason));
        return state;
    }
}
