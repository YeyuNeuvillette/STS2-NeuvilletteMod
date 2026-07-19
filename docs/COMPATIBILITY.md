# Neuvillette compatibility contract

## Public API

Use `Neuvillette.Api.NeuvilletteApi` instead of patching Neuvillette internals.

- Event contributors receive an immutable snapshot and return a new option list.
- Sticker contributors must return a stable candidate order on every peer. Registration order is preserved.
- Act 4 contributors can extend map and room configuration, handle terminal rewards, or provide a combat background.
- Map marker events are isolated: an exception from one subscriber is logged and does not stop other subscribers.
- Every registration returns `IDisposable`; dispose it when the contributing mod unloads.

Public interfaces are additive compatibility surfaces. Patch classes, feature services, reflection adapters, and content implementation details are internal and may change between releases.

## Multiplayer rules

- Shared settings are written only by the single-player instance or multiplayer host.
- Gameplay randomness must use `RunState.Rng` or a deterministic `Rng` keyed by the run seed and stable coordinates.
- Candidate collections must be placed in a stable order before deterministic shuffling.
- Gameplay state changes use game commands and are awaited. Fire-and-forget work is limited to local screen progression already synchronized by the game.
- Combat-scoped sticker state uses a `ConditionalWeakTable` and is explicitly removed after combat.
- UI services may inspect synchronized state but must not mutate run, combat, card, relic, or creature state.

## Patch risk inventory

| Area | Patch style | Risk | Compatibility behavior |
| --- | --- | --- | --- |
| Vanilla event options | Postfix | Low | Copies option lists and preserves options added by earlier patches. Public contributors run last. |
| Four Quadrants marker | Scoped Prefix/Postfix | Medium | Skips room creation only at the point carrying this mod's quest marker. |
| Neow options | Character-scoped Prefix | Medium | Replaces generation only for Neuvillette without run modifiers. |
| Act 4 map and rooms | Scoped Postfix | Medium | Runs only for `NeuvilletteAct`; private game members are accessed through the compatibility adapter. |
| Act 4 rewards/background/music | Scoped Prefix | Medium | Skips vanilla only for the exact Act 4 terminal or visual case handled by the service. |
| Wish shop | Postfix | Low | Adds one unique entry and composes with other price/refill hooks. |
| Multiplayer infinite HP bar | Scoped Prefix | Medium | Skips width calculation only for a creature in a non-normal HP display state. |
| Healing observation | Prefix | Low | Records HP only when the target owns `AssistArrestPower`; it does not alter the command. |

`GameCompatibility.Validate` checks every private member used by the mod. Missing members produce one warning and the dependent feature falls back to vanilla behavior where possible.

The former global `RoomSet.FromSave` filter was removed because it could delete unresolved content IDs belonging to other mods during load-order races.
