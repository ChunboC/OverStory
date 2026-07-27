# Overstory — The Leprechaun Chase

A 3D platformer-chase game built in Unity for CS6457 Video Game Design.

A troll has stolen your pot of gold and is fleeing up the beanstalk to the clouds.
You play a leprechaun chasing him across a sky course of drifting cloud platforms,
gathering shamrocks to throw at him and slow him down. Catch the troll before the
timer runs out, or he escapes with your gold.

## Team

- Austin Daniel Abate
- Chunbo Cheng
- Moiz Ahmed Yousufi
- Ryan Andrew Pepin
- Syed Mikdad Uddin

## Running the Game

**Prebuilt binary:** run the executable in the `Build/` directory of the
submission package. No installation required.

**From source:** open the project in **Unity 6000.0.75f1** and press Play from
`Assets/Scenes/StartMenu.unity`. Starting from any other scene will skip the
run-state setup and the game flow will not behave correctly.

## Game Flow

```
StartMenu ──► TutorialVillage ──► Level1_Scene ──► MainScene ──► WinScene
                                                              └─► LoseScene
```

| Scene | Role |
| --- | --- |
| `StartMenu` | Title, credits panel, exit |
| `TutorialVillage` | Safe area. NPCs teach movement and throwing, and unlock the double jump and air dash before you are allowed out |
| `Level1_Scene` | Timed cloud-platform course with branching routes and collectibles |
| `MainScene` | The chase. The troll flees for the beanstalk while weather turns against you |
| `WinScene` / `LoseScene` | Outcome, with replay and menu options |

Progress carries across scenes: abilities unlocked in the tutorial stay unlocked,
and shamrocks collected in Level 1 become your throwing ammunition in the chase.

## Controls

| Action | Keyboard / Mouse | Gamepad |
| --- | --- | --- |
| Move | `W A S D` / Arrow keys | Left stick |
| Look | Mouse | Right stick |
| Jump / Double jump | `Space` | A |
| Air dash | `Left Shift` | X |
| Throw shamrock | Left mouse button | Right trigger |
| Interact / talk | `E` | Y |
| Pause | `P` | Start |
| Menu navigation | Mouse | D-pad / left stick + A |

Movement is analog: a light stick tilt walks, a full tilt runs. The jump is a
true double jump, and the air dash is limited to one per airborne period so it
cannot be chained to trivialise gaps.

## What We Implemented

Everything in `Assets/Scripts/` is our own code. Third-party content is limited
to art, audio, and animation assets — no prepackaged gameplay or controllers.

**Character control** — `PlayerController.cs`
Rigidbody movement with camera-relative input, analog walk/run blending, double
jump, a one-per-airborne air dash, extra fall gravity for jump weight, ground
detection with slope projection, and velocity inheritance so the player rides
moving platforms correctly. Mecanim animation is driven from input state.

**World and physics** — `KinematicPlatform.cs`, `CloudPlatformSpawner.cs`,
`CloudAnimation.cs`, `FallReset.cs`, `SlowingProjectile.cs`
Kinematic moving platforms that expose their velocity to riders, an editor tool
that procedurally generates cloud platform grids, animated cloud motion, a fall
boundary that respawns the player with a time penalty rather than ending the run,
and physics-driven thrown projectiles.

**Enemy AI** — `EnemyAI.cs`, `TrollSounds.cs`
NavMesh-driven troll with a seven-state machine (Idle, Run, DropObstacle, Jump,
EscapeRun, Escaped, Laugh). It picks a randomised route from a waypoint pool each
run, so no two chases are identical, then breaks for the beanstalk. It scales its
obstacle-dropping rate by how close the player is, traverses off-mesh links with a
scripted jump arc, and reacts to being hit by a shamrock by slowing down.

**Environmental AI** — `WeatherAI.cs`
A weather system that escalates over the course of a run, telegraphing lightning
strikes before they land and rolling in storms with wind that pushes the player
off-line.

**Tutorial NPCs** — `MovementTrainerNPC.cs`, `ShamrockTrainerNPC.cs`,
`ExitGuideNPC.cs`
Gated training. Each NPC watches for the player to actually demonstrate a skill
before unlocking the next one and opening the exit.

**Progression** — `RunProgress.cs`, `LevelGoal.cs`, `CollectiblePickup.cs`,
`CollectibleSpawner.cs`, `AreaCollectible.cs`
Cross-scene run state for unlocked abilities and banked shamrocks, level goal
triggers, and an editor tool that places collectibles along authored routes with
reward tiers, so harder lines pay out more.

**Game management** — `GameManager.cs`
Timer, pause menu, win/lose handling, fall penalties, scene transitions, and
controller-aware menu focus.

## Third-Party Assets

All third-party content is art, audio, or animation only. None of it includes
gameplay logic, character controllers, or AI.

**Models and art**

| Asset | Source | Used for |
| --- | --- | --- |
| Leprechaun model | ai3dgen | Player character |
| Cloud platform model | Meshy AI | Cloud platforms |
| Catgear Games — Model Pack 1 | Catgear Games | Tutorial NPCs (Chili, Eggy, Kiwi, Langsat) |
| Cursed Cozy Village — Free Starter Pack | SerwusStudio | Tutorial village environment |
| SkySeries Freebie | — | Skyboxes |
| Low Poly Trim Sheet Asset Collection | — | Environment surfaces |
| Stylized Cannon | — | Village cannon |
| Blink | — | Target dummy |
| Hovl Studio | — | Visual effects |
| OccaSoftware | — | Visual effects |
| MaximeBrunoni | — | Environment art |

**Animation**

Player animations from **Mixamo**. Troll animations (`Running`, `Jumping`,
`Taunting`) are third-party FBX clips; the Animator controllers, state machines,
layers, and masks are ours.

**Audio**

Music: "Folk Round" (Kevin MacLeod), "Emerald Isle" (Geoff Harvey), "Irish Jig"
(musictown), "Irish Bay" (Serge Pavkin), "Morning Weather Report",
"Underground Noir", "Village Background Music".

Sound effects: The Sound Guild FREE PACK Footsteps Vol. 2, plus individual
cannon, collectible, dash, jump, shoot, zap, and troll vocalisation samples.

> **Note for submission:** license text and source URLs for each pack still need
> to be filled in below before hand-in. Several packs ship a `README.txt` or
> `License.txt` in their folder under `Assets/` — those are the authoritative
> source. Do not guess attributions.

## Known Issues

- The Level 1 to chase transition depends on `MainScene` being enabled in
  **File → Build Settings**. If it is disabled, reaching the beanstalk at the end
  of Level 1 fails to load the next scene.
- `TutorialVillage` currently has no pause menu; pausing and quitting are
  available from Level 1 onward.
- `SampleScene` is a leftover Unity template scene and is not part of the game.

## Repository Layout

```
Assets/
  Scripts/     all gameplay code (ours)
  Scenes/      StartMenu, TutorialVillage, Level1_Scene, MainScene, Win/Lose
  Prefabs/     cloud platforms, collectibles, weather, projectiles
  Models/      leprechaun, troll, beanstalk, cloud platform
  Audio/       music and SFX
```
