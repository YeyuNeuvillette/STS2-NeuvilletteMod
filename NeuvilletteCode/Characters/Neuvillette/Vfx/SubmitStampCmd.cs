using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;

namespace Neuvillette.Characters.Neuvillette.Vfx;

/// <summary>
/// Presents and stamps a submitted card before delegating to the game's exhaust mechanics.
/// </summary>
public static class SubmitStampCmd
{
    private const float SubmittedCardGap = 42f;
    private const float ScreenEdgePadding = 48f;

    public static async Task Exhaust(
        PlayerChoiceContext choiceContext,
        CardModel card,
        CardModel? presentingCard = null)
    {
        var room = NCombatRoom.Instance;
        if (room?.Ui == null)
        {
            await CardCmd.Exhaust(choiceContext, card);
            return;
        }

        var oldPile = card.Pile;
        var cardNode = NCard.FindOnTable(card);
        var ownsPresentationNode = false;

        // Hand selection keeps the chosen card inside NSelectedHandCardContainer until the
        // source card finishes executing. NCard.FindOnTable therefore returns a real node,
        // but that holder continuously owns its central position. Take the node out of the
        // holder before laying it out as courtroom evidence.
        if (cardNode != null && LocalContext.IsMine(card) && oldPile?.Type == PileType.Hand)
        {
            cardNode = await DetachHandCardForPresentation(
                room,
                card,
                cardNode,
                presentingCard);
            ownsPresentationNode = cardNode != null;
        }
        else if (cardNode == null && LocalContext.IsMine(card))
        {
            cardNode = await CreateCenteredPreview(card, oldPile?.Type ?? PileType.None, presentingCard);
            ownsPresentationNode = cardNode != null;
        }

        if (cardNode == null)
        {
            await CardCmd.Exhaust(choiceContext, card);
            return;
        }

        await NSubmitStampVfx.Play(cardNode);

        if (!ownsPresentationNode)
        {
            await CardCmd.Exhaust(choiceContext, card);
            return;
        }

        // A draw/discard-pile card has no table node for CardPileCmd to reuse. Move the model silently,
        // restore the UI callbacks that silent movement omits, then feed our stamped preview to the
        // same full-card exhaust VFX used by the base game.
        var result = await CardCmd.Exhaust(choiceContext, card, skipVisuals: true);
        if (result is not { success: true })
        {
            cardNode.QueueFreeSafely();
            return;
        }

        if (oldPile != null)
        {
            oldPile.InvokeCardRemoved(card);
            oldPile.InvokeCardRemoveFinished();
            oldPile.InvokeContentsChanged();
        }
        card.Pile?.InvokeCardAddFinished();

        var exhaustVfx = NCardExhaustVfx.Create(cardNode);
        if (exhaustVfx == null)
        {
            cardNode.QueueFreeSafely();
            return;
        }

        room.Ui.AddChildSafely(exhaustVfx);
        NDebugAudioManager.Instance?.Play(TmpSfx.cardExhaust);
        _ = TaskHelper.RunSafely(exhaustVfx.PlayAnimation());
    }

    private static async Task<NCard?> DetachHandCardForPresentation(
        NCombatRoom room,
        CardModel card,
        NCard cardNode,
        CardModel? presentingCard)
    {
        var holder = room.Ui.Hand.GetCardHolder(card);
        if (holder == null || holder.CardNode != cardNode)
            return null;

        var originalGlobalPosition = cardNode.GlobalPosition;
        room.Ui.Hand.RemoveCardHolder(holder);
        room.Ui.AddChildSafely(cardNode);
        cardNode.GlobalPosition = originalGlobalPosition;
        cardNode.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);

        NSubmittedPreviewLayerGuard.Attach(cardNode, 300);
        return await MoveToPresentationSeat(room, cardNode, presentingCard, revealFromNothing: false);
    }

    private static async Task<NCard?> CreateCenteredPreview(
        CardModel card,
        PileType sourcePile,
        CardModel? presentingCard)
    {
        var room = NCombatRoom.Instance;
        var cardNode = NCard.Create(card);
        if (room?.Ui == null || cardNode == null)
            return null;

        room.Ui.AddChildSafely(cardNode);
        cardNode.UpdateVisuals(sourcePile, CardPreviewMode.Normal);
        NSubmittedPreviewLayerGuard.Attach(cardNode, 300);

        return await MoveToPresentationSeat(room, cardNode, presentingCard, revealFromNothing: true);
    }

    private static async Task<NCard?> MoveToPresentationSeat(
        NCombatRoom room,
        NCard cardNode,
        CardModel? presentingCard,
        bool revealFromNothing)
    {
        var targetPosition = GetSubmittedPreviewPosition(room, cardNode, presentingCard);
        if (revealFromNothing)
        {
            cardNode.GlobalPosition = targetPosition;
            cardNode.Scale = Vector2.One * 0.76f;
            cardNode.Modulate = new Color(1f, 1f, 1f, 0f);
        }

        var reveal = cardNode.CreateTween().SetParallel();
        reveal.TweenProperty(cardNode, "global_position", targetPosition, 0.18f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        reveal.TweenProperty(cardNode, "scale", Vector2.One, 0.16f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        reveal.TweenProperty(cardNode, "modulate", Colors.White, 0.10f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        return await reveal.AwaitFinished(cardNode) ? cardNode : null;
    }

    private static Vector2 GetSubmittedPreviewPosition(
        NCombatRoom room,
        NCard submittedCard,
        CardModel? presentingCard)
    {
        var activeCard = presentingCard == null
            ? null
            : room.Ui.PlayQueue.GetCardNode(presentingCard)
              ?? room.Ui.GetCardFromPlayContainer(presentingCard)
              ?? room.Ui.Hand.GetCard(presentingCard);

        activeCard ??= room.Ui.PlayContainer
            .GetChildren()
            .OfType<NCard>()
            .LastOrDefault(candidate => candidate.IsInsideTree() && candidate.Visible);

        if (activeCard == null)
            return PileType.Play.GetTargetPosition(submittedCard);

        // NCard/CardContainer are zero-sized controls whose visible 300x422 card is drawn
        // around the node origin. Layout must therefore use visual centers, never Control rects.
        var activeCenter = activeCard.Body.GlobalPosition;
        var activeSize = activeCard.GetCurrentSize();
        var viewportSize = room.GetViewportRect().Size;
        var submittedSize = NCard.defaultSize;

        var rightCenter = new Vector2(
            activeCenter.X + activeSize.X * 0.5f + SubmittedCardGap + submittedSize.X * 0.5f,
            activeCenter.Y);
        var leftCenter = new Vector2(
            activeCenter.X - activeSize.X * 0.5f - SubmittedCardGap - submittedSize.X * 0.5f,
            activeCenter.Y);

        var halfSubmittedSize = submittedSize * 0.5f;
        var hasRoomOnRight = rightCenter.X + halfSubmittedSize.X <= viewportSize.X - ScreenEdgePadding;
        var chosenCenter = hasRoomOnRight ? rightCenter : leftCenter;
        chosenCenter.X = Mathf.Clamp(
            chosenCenter.X,
            ScreenEdgePadding + halfSubmittedSize.X,
            Mathf.Max(
                ScreenEdgePadding + halfSubmittedSize.X,
                viewportSize.X - halfSubmittedSize.X - ScreenEdgePadding));
        chosenCenter.Y = Mathf.Clamp(
            chosenCenter.Y,
            ScreenEdgePadding + halfSubmittedSize.Y,
            Mathf.Max(
                ScreenEdgePadding + halfSubmittedSize.Y,
                viewportSize.Y - halfSubmittedSize.Y - ScreenEdgePadding));

        // Convert the desired visible-card center back to the NCard root's global position.
        return submittedCard.GlobalPosition + chosenCenter - submittedCard.Body.GlobalPosition;
    }
}

/// <summary>
/// Keeps a submitted preview above the play queue, then restores the pooled NCard's canvas state.
/// </summary>
internal partial class NSubmittedPreviewLayerGuard : Node
{
    private NCard? _card;
    private int _originalZIndex;
    private bool _originalZAsRelative;

    public static void Attach(NCard card, int zIndex)
    {
        var guard = new NSubmittedPreviewLayerGuard
        {
            _card = card,
            _originalZIndex = card.ZIndex,
            _originalZAsRelative = card.ZAsRelative
        };
        card.AddChildSafely(guard);
        card.ZAsRelative = false;
        card.ZIndex = zIndex;
    }

    public override void _ExitTree()
    {
        Callable.From(RestoreAfterCardReturnsToPool).CallDeferred();
    }

    private void RestoreAfterCardReturnsToPool()
    {
        if (!GodotObject.IsInstanceValid(this) || IsQueuedForDeletion())
            return;

        if (_card != null && GodotObject.IsInstanceValid(_card) && !_card.IsInsideTree())
        {
            _card.ZIndex = _originalZIndex;
            _card.ZAsRelative = _originalZAsRelative;
            this.QueueFreeSafelyNoPool();
        }
    }
}
