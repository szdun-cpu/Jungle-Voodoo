# CLAUDE.md — Jungle-Voodoo

AI assistant reference for the Jungle-Voodoo mobile MMO strategy game.

---

## Project Overview

**Jungle-Voodoo** is a mobile MMO strategy game for iOS and Android — think Mafia City / Grand Mafia but with a **jungle voodoo theme**. Players build a jungle base (hut compound), train voodoo-themed troops, raid other players on the Cursed Wilds world map, and join Tribes led by Witch Doctors.

**Stack:**
- Engine: Unity 2022 LTS or Unity 6 (C#)
- Backend: PlayFab (Azure) — authentication, player data, CloudScript for server-authoritative combat
- Architecture: Service Locator + ScriptableObjects
- Asset loading: Unity Addressables (essential — keeps APK small)
- UI: Unity uGUI + TextMesh Pro
- Third-party: Newtonsoft.Json (via UPM), UniTask (async), DOTween (animations, add from Asset Store)

---

## Directory Structure

```
Assets/
├── Scripts/
│   ├── Core/          GameManager, ServiceLocator, SceneLoader, Constants
│   ├── Data/          ScriptableObject definitions + PlayerProfile model
│   ├── Systems/       ResourceSystem, BuildingSystem, TroopSystem, TimerSystem, CombatSystem
│   ├── Networking/    PlayFabManager, AuthService, PlayerDataService
│   ├── UI/            UIManager, HUDController, panel controllers
│   ├── Map/           WorldMapManager, TileData
│   └── Utilities/     Singleton<T>, ObjectPool, Extensions
├── Data/ScriptableObjects/   .asset files (BuildingData, TroopData, ResourceData, HeroData)
├── Scenes/            Boot, MainMenu, Base, WorldMap
├── Prefabs/           Buildings, Troops, UI, Effects
├── Art/               Sprites, Animations, Materials
├── Audio/             Music, SFX
└── _ThirdParty/       DOTween and other Asset Store packages
Packages/
└── manifest.json      UPM dependencies (Addressables, PlayFab SDK, UniTask, etc.)
```

---

## Theme: Jungle-Voodoo Vocabulary

| Generic MMO Term | In This Game |
|---|---|
| Headquarters | Great Hut |
| Alliance / Clan | Tribe |
| Guild Leader | High Shaman |
| Team Leader / Hero | Witch Doctor |
| Infantry T1 | Zombie Shambler |
| Infantry T2 | Cursed Warrior |
| Infantry T3 | Swamp Revenant |
| Ranged T1 | Bone Thrower |
| Ranged T3 | Hex Archer |
| Cavalry | Spirit Beast |
| Caster | Voodoo Witch / Death Witch |
| Siege | Voodoo Doll |
| Scout | Shadow Wraith |
| Primary Currency | Spirit Energy |
| Military Material | Bones |
| Food / Sustenance | Dark Herbs |
| Premium Material | Dark Essence |
| Paid Currency | Voodoo Tokens |
| Attack | Ritual Raid |
| Scout Action | Shadow Gaze |
| Research | Dark Arts |
| Upgrade | Ritual Empowerment |
| World Map | The Cursed Wilds |
| Player Territory | Sacred Ground |

**Always use theme vocabulary in display strings, UI labels, and player-facing text.**
Use generic names only in code (e.g. `TroopSystem`, not `ZombieSystem`).

---

## Core Architecture

### Service Locator
All major systems are registered at startup in `GameManager.Bootstrap()` and accessed via:
```csharp
var res = ServiceLocator.Instance.Get<ResourceSystem>();
```
Never use `FindObjectOfType` or direct singletons for game systems.

### GameManager Boot Flow
1. `Boot` scene loads → `GameManager.Awake()` runs
2. `Bootstrap()` instantiates and registers all services
3. `InitializeGame()` coroutine: Login → LoadPlayerData → SetState(MainMenu)

### ScriptableObjects
All game balance lives in `.asset` files, not in code. Define new balance values there, reference them by loading via Addressables. Keys use `Constants.*` — never raw strings.

### Event Pattern
Systems communicate via `System.Action<T>` events, not direct method calls:
```csharp
_resourceSystem.OnResourceChanged += HandleResourceChanged;
```
Unsubscribe in `OnDestroy`. Never use UnityEvent for system-to-system communication.

---

## Systems Quick Reference

| System | Responsibility |
|---|---|
| `TimerSystem` | Server-aligned countdown timers for construction, training, march |
| `ResourceSystem` | Amounts, caps, production ticks, spending |
| `BuildingSystem` | Placement, upgrades, applies production bonuses |
| `TroopSystem` | Training queues, march creation, return/healing |
| `CombatSystem` | Sends march to PlayFab CloudScript; applies result |
| `WorldMapManager` | Tile grid rendering, chunk loading, tile selection |
| `UIManager` | Addressable panel stack (open/close/back) |

---

## Namespaces

| Folder | Namespace |
|---|---|
| Scripts/Core | `JungleVoodoo.Core` |
| Scripts/Data | `JungleVoodoo.Data` |
| Scripts/Systems | `JungleVoodoo.Systems` |
| Scripts/Networking | `JungleVoodoo.Networking` |
| Scripts/UI | `JungleVoodoo.UI` |
| Scripts/Map | `JungleVoodoo.Map` |
| Scripts/Utilities | `JungleVoodoo.Utilities` |

---

## Coding Conventions

- **Naming**: PascalCase for classes/methods/properties; `_camelCase` for private fields
- **No magic strings**: All keys/IDs in `Assets/Scripts/Core/Constants.cs`
- **No direct cross-system calls**: Use events or ServiceLocator
- **Async**: UniTask for network calls; coroutines only for Unity lifecycle needs
- **Object reuse**: Use `ObjectPool` for anything spawned frequently (troops, VFX, map markers)
- **Serialization**: Newtonsoft.Json (`JsonConvert`) — not Unity's `JsonUtility` (handles Dictionaries)

---

## PlayFab Setup (first-time)

1. Create a free account at developer.playfab.com
2. Create a new Title — note your **Title ID**
3. Set `_playFabTitleId` on the `GameManager` GameObject in the Boot scene
4. CloudScript: upload `CloudScript/main.js` (to be created) via the PlayFab dashboard
5. Enable **Players > Allow client-side profile data** in Title Settings

---

## Running the Project

1. Install Unity 2022 LTS or Unity 6
2. Open the `/home/user/Jungle-Voodoo` folder as a Unity project
3. Unity will resolve `Packages/manifest.json` automatically (may take a few minutes)
4. **DOTween**: import from the Asset Store manually → place in `Assets/_ThirdParty/`
5. Set your PlayFab Title ID on `GameManager` in the Boot scene
6. Press Play in the Boot scene

---

## Git Conventions

- Branch for this session: `claude/add-claude-documentation-YnhZs`
- Never push directly to `main`
- Commit message format: imperative mood, present tense ("Add ResourceSystem", not "Added")
- Keep `.meta` files — Unity requires them

---

## Development Phases

| Phase | Status | Description |
|---|---|---|
| 1 — Scaffolding | **Done** | Project structure, all C# scripts, manifest |
| 2 — Unity Setup | Pending | Scenes, prefabs, UI Canvas, GameManager GameObject |
| 3 — PlayFab Config | Pending | Title setup, CloudScript for combat |
| 4 — Base Gameplay | Pending | Building placement, resource ticks, troop training |
| 5 — World Map / PvP | Pending | Tile rendering, march system, Ritual Raids |
| 6 — Tribes | Pending | Alliance system, shared territory, tribe chat |
| 7 — Monetization | Pending | VoodooTokens shop, Witch Doctor battle pass, events |

---

## AI Assistant Guidelines

- **Read before editing** — always read a file before modifying it
- **No unsolicited refactors** — only change what is asked
- **Use Constants.* for all string keys** — never introduce raw string literals
- **Route system calls through ServiceLocator** — no `FindObjectOfType`, no new singletons
- **Use theme vocabulary** in any player-facing strings (see table above)
- **Confirm before destructive actions** (git force-push, file deletion, dropping data)
- **Never push to a branch other than the one designated at session start**
