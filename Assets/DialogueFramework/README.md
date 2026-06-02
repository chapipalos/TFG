# Dialogue Framework

A complete, node-based dialogue system for Unity with built-in quest tracking, branching player replies, and an event-driven runtime — all wrapped in a clean visual editor.

Designed for narrative-driven games where dialogues need to react to the world: quest states, completed objectives, player choices, and events from any system in your game.

**Created by Chapi.**

---

## Features

### Visual node editor
- Custom GraphView-based editor with drag-and-drop nodes, automatic save/load, and live dropdowns that always reflect the current state of your asset.
- Conversations grouped into named sections inside a single asset. Switch between conversations from a dropdown in the editor — the canvas filters automatically.
- Dedicated tabs for **Nodes**, **Actors** and **Quests**.

### Branching dialogue
- One generic output per node, or multiple **player reply** ports for branching choices.
- **Skip-to-end typewriter** with smoothed progress bar.
- **Keyboard / gamepad reply selection** via the Unity Input System (next, previous, submit).
- Replies are highlighted visually without the EventSystem auto-firing them.

### Quest system
- Five quest states: `NotStarted`, `Active`, `InProgress`, `Completed`, `Failed`.
- Multiple objectives per quest. Quest auto-promotes to `InProgress` on first objective completed, and auto-completes when all are done.
- Per-quest progress tracking (`x / total`).
- Public events `OnQuestStatusChanged` and `OnObjectiveChanged` for UI integration.

### Node-driven flow control
- **Quest requirements** — a node is only reachable if the quest is in a specific status.
- **Objective requirements** — a node is only reachable if a specific objective is completed (or not).
- **Node effects** — actions that fire when the player reaches the node: start/complete/fail quests, complete specific objectives.
- Multiple requirements/effects per node, all evaluated as AND.

### Event-driven runtime
- Static **GameEventBus** for global game events identified by name. No ScriptableObjects required.
- **Event-to-objective bindings** in the QuestManager: any event from your game (`OnIronCollected`, `OnEnemyDefeated`, etc.) can automatically complete an objective. Configured entirely from the Inspector with cascading dropdowns.

### Multiple conversations per asset
- A single `GraphData` asset can hold any number of conversations.
- Quests and actors are shared across all conversations.
- Each `DialogueManager` in your scene references a conversation by name (e.g. `"Aldric_Forge"`).

### Bundled UI
- Ready-to-use quest journal panel (`QuestController`) with active / in-progress / completed sections, objective toggles, and live updates.
- Ready-to-use dialogue panel with typewriter, actor name, replies, and skip button.
- Example generator that scaffolds a complete quest with conditional branches you can study and modify.

---

## Requirements

- Unity **2022.3 LTS** or newer (uses UI Toolkit / GraphView).
- **TextMeshPro** (auto-imported by Unity).
- **Input System** package (for the keyboard/gamepad dialogue controller).

---

## Installation

1. Import the `DialogueFramework` package from the Asset Store, or copy the folder into `Assets/`.
2. Wait for Unity to compile.
3. Open `Window → Dialogue Framework` to launch the editor.

Optional: run `Tools → Dialogue Framework → Generate Example Graph` to create a fully wired sample scene with a quest, dialogue branches, and event bindings.

---

## Quick start

### 1 — Create a graph asset

`Window → Dialogue Framework → New`

This opens the editor with a fresh `GraphData` asset. The editor has three tabs:

- **Nodes** — dialogue nodes for each conversation.
- **Actors** — characters who speak in dialogues.
- **Quests** — quest definitions with objectives.

### 2 — Define your data

In **Actors**, click `Add Actor` and give them names (e.g. `"Aldric"`, `"Mira"`).

In **Quests**, click `Add Quest`, set a title and description, and add objectives with their descriptions.

### 3 — Create a conversation

In the **Nodes** tab, click `+ New Conversation` and rename it to something descriptive (e.g. `"Aldric_Forge"`). This name is how the runtime finds the conversation.

### 4 — Build the dialogue

Right-click on the canvas → `Create node`. Each node has:

- **Name / Dialogue** — what the actor says.
- **Actor** — who is speaking (or `None`).
- **Objective requirements** — objectives that must be completed (or not) for this node to be reachable.
- **Quest requirements** — quest statuses required for this node.
- **Player replies** — branching choices, each with its own output port.
- **Node effects** — what happens when the player reaches this node (`Quest Start`, `Quest Complete`, `Quest Fail`, `Objective Complete`).

Drag from output ports to input ports to connect nodes.

### 5 — Set up the scene

Add three GameObjects to your scene:

| GameObject | Component | Purpose |
|---|---|---|
| `Managers/QuestManager` | `QuestManager` | Tracks quest state. Add **objective bindings** in the inspector to auto-complete objectives from game events. |
| `UI/DialogueCanvas` | `DialogueManager` | Runs the dialogue UI. Set its `m_ConversationName` to the conversation you want to play (e.g. `"Aldric_Forge"`). |
| `UI/DialogueCanvas/Input` | `DialogueController` | Handles keyboard/gamepad input. Assign the three `InputActionReference` fields. |

Assign your `GraphData` asset to both managers.

### 6 — Trigger dialogues from your game

```csharp
// From any script in your scene
dialogueManager.StartDialogue();
```

### 7 — Trigger events from gameplay

```csharp
// Picked up an item, defeated an enemy, entered a zone…
GameEventBus.Raise("OnIronCollected");
```

The `QuestManager` listens to these events and updates objectives automatically, based on the bindings configured in its inspector.

---

## Example: the blacksmith quest

The bundled example (`Tools → Dialogue Framework → Generate Example Graph`) scaffolds a complete narrative loop:

1. Player talks to Aldric — quest **not started** → Aldric offers the quest with two reply options.
2. Player accepts → quest goes to `Active`. Aldric explains where the iron is.
3. In the world, the player picks up iron → game code calls `GameEventBus.Raise("OnIronCollected")`.
4. Player talks to Aldric again → matching objective is completed, quest goes to `InProgress`. Aldric thanks the player and the quest auto-completes.
5. Player talks to Aldric one more time → post-quest dialogue.

Each branch is gated by a different combination of quest status and objective completion. The full graph is generated automatically as a reference you can pull apart to learn the system.

---

## Architecture overview

```
GraphData (ScriptableObject)
 ├─ Actors            (shared across conversations)
 ├─ Quests            (shared across conversations)
 ├─ Conversations     (named groups of nodes)
 ├─ Nodes             (filtered by conversation in editor and runtime)
 └─ Links             (connections between nodes)

Runtime
 ├─ DialogueManager     (one per NPC / scene element, references a conversation)
 ├─ DialogueController  (Input System integration)
 ├─ QuestManager        (singleton, tracks quest state and objective progress)
 ├─ QuestController     (UI panel — journal with active / in progress / completed)
 └─ GameEventBus        (static, name-based event channel)
```

---

## Extending the system

- **New effect types** — add a value to `NodeEffectType` and a case in `DialogueManager.ExecuteNodeEffects`. The editor regenerates the dropdown automatically.
- **New requirement types** — extend `NodeData` with another list (`questRequirements`, `objectiveRequirements` are the templates to follow) and add the matching evaluation to `NodeConditionsMet`.
- **Custom UI** — `DialogueManager` and `QuestController` are pure MonoBehaviours with explicit references. Replace the prefabs or rewrite the UI entirely without touching the data layer.

---

## License

This asset is released under Asset Store standard EULA. Source code is included.

---

## Support

For bug reports, feature requests, or questions, contact **Chapi**.
