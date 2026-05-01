# FlatRedBall2 — Todo

**No speculative items.** Every entry must be either (a) ready to work on now or (b) ready to discuss now. "Eventually," "someday," "maybe if it comes up" do not belong here — they're noise that buries real work and never gets revisited. If an idea is interesting but not actionable, let it surface again organically when a real use case appears; don't pre-emptively log it.

Open work only. When an item ships, delete it — don't leave a "landed" breadcrumb. Design decisions and historical context that outlive a TODO belong in skill files, XML docs, or commit messages, not here.

## Mouse / cursor injection in automation mode

`AutomationMode.ProcessInputCommand` handles `"key"`, `"gamepad"` (button), and `"axis"` (gamepad), but not the cursor — there's no `InjectCursor` on `InputManager` or pathway through `Cursor` to override `WorldPosition`/`PrimaryDown`/`PrimaryPressed`. Mouse-driven samples (e.g. Solitaire) can't be automation-tested without it. Add a synthetic-cursor input source mirroring the keyboard/gamepad pattern, plus an NDJSON command — likely `{"cmd":"input","type":"cursor","x":...,"y":...,"primary":true}` with coords in either screen or world space (decide which, or accept both via a `space` field).

## Entity-attached Gum visuals are not in Gum's update tree

`Entity.Add(GraphicalUiElement)` and `Entity.Add(FrameworkElement)` wrap the visual in a `GumRenderable`, register it for rendering, and drive its position from the entity's `AbsoluteX/Y` each frame — but never add it as a child of any Gum root. Consequences:

1. **Forms controls aren't live.** A `Button` added to an entity renders but doesn't receive cursor/click events, because Gum Forms input handling walks `Root.Children` (or `MainRoot`) to dispatch. The user sees a button-shaped thing that does nothing.
2. **Hot-reload skips them.** `GumHotReloadManager.PerformReload` only rebuilds `root.Children`. Entity-attached visuals are invisible to it. Today the workaround is to subscribe to `FlatRedBallService.GumHotReloadCompleted` and `RestartScreen(HotReload)`, which works but throws out everything.
3. **Animations/state interpolations on entity visuals may not tick** if Gum's per-frame update walks `Root` rather than tracking elements globally.

Possible directions:
- Add a hidden "entity-attached" Gum sub-root that the engine maintains as a child of `Root`, parented under it for update/input/hot-reload purposes but with layout that doesn't disturb world-space-driven positioning. Visuals added via `Entity.Add(...)` go in there.
- Or reach into Gum's Forms input system to register entity-attached `FrameworkElement`s as input-eligible without re-parenting.
- Either way: fixing this also fixes per-element hot-reload for entity visuals (no screen restart needed), so it's worth doing properly rather than papering over each symptom.

## Window resize misaligns world-space cards in Solitaire

Resizing the Solitaire window causes every card to draw at an offset from its slot. Cards
are positioned via `SlotWorldCenter` which converts a Gum slot's `AbsoluteLeft/Top` (canvas
space) into world coords by subtracting half the camera's orthogonal extents — but on resize
the canvas-to-world relationship shifts (Pattern A stretch-to-viewport), and either the slot
absolutes, the camera extents, or the cached anchor positions go stale. Repro: launch
Solitaire desktop, resize the window, observe cards drift relative to the green-felt slot
graphics. Decide whether the fix belongs in the sample (re-run layout on resize), in the
engine's world↔canvas helper, or in Gum's resize signaling.

## Deterministic randoms under automation mode

When automation mode is active, `FlatRedBallService.Random` (and any other engine-owned `GameRandom` paths) should seed deterministically so that recorded automation runs reproduce exactly. Game code that constructs its own `Random` / `GameRandom` should still get a well-defined seed when it asks the engine for one, so non-determinism doesn't sneak in through `new Random()` calls. Open question: how the seed is communicated (env var, automation-mode init field, dedicated `AutomationOptions.Seed`?) — settle that before implementing.
