# DOTS_RTS

## Project Overview
DOTS_RTS is a Unity real-time strategy prototype built around Unity DOTS, data-driven gameplay, and ECS system flows.

The project is currently focused on validating scalable RTS gameplay loops and architecture rather than shipping-level content. While the implementation continues to evolve, the current build already supports practical gameplay testing in the main test scene.

## Purpose
This project exists to prototype and iterate on:

- ECS-based unit and building behavior
- Data-oriented combat pipelines (melee, ranged, and projectile impact)
- Targeting and movement interactions in large-entity scenarios
- Registry-driven gameplay data and animation configuration

## Most Prominent Features

- Unity DOTS architecture with systems and component-driven behavior
- Data lookup and registry pattern for entities, units, buildings, and animations
- Target acquisition flow with manual targeting support and automated scans
- Attack pipelines with event-driven spawning and projectile movement
- Training/spawning workflows for unit production from buildings
- Main playable test scene for runtime gameplay verification

## Requirements

To open and work with this Unity DOTS project, ensure you have the following installed:

- **Unity 6000.3.11f1** (project authoring version; other 6000.3 versions may work but are not guaranteed)
- **Entities package** (`com.unity.entities`)
- **Entities Graphics package** (`com.unity.entities.graphics`)
- **Burst-compatible DOTS stack** (Burst is used by DOTS dependencies)
- **2D Sprite** (`com.unity.2d.sprite`)
- **Visual Studio IDE** (`com.unity.ide.visualstudio`)
- **Input System** (`com.unity.inputsystem`)
- **Physics** (`com.unity.physics`)
- **Universal Render Pipeline** (`com.unity.render-pipelines.universal`)
- **A supported C# IDE** (Visual Studio or Visual Studio Code, both with Unity support/extensions)
- **C# tooling for your chosen IDE** (needed if you plan to edit scripts outside the Unity Inspector)

## Step-by-Step Setup (No Command-Line)

### 1. Install Unity Hub
1. Download and install Unity Hub from the official Unity website.
2. Sign in with your Unity account.

### 2. Install the Required Unity Editor
1. Open Unity Hub.
2. Go to **Installs**.
3. Add **Unity 6000.3.11f1**.
4. Include any platform modules you need for your target build platforms.

### 3. Install C# Tooling (If You Will Edit Code)
You only need this step if you plan to edit scripts in an external IDE.

1. **Visual Studio path:**
	Install Visual Studio with Unity support and C#/.NET desktop tooling enabled.
2. **Visual Studio Code path:**
	Install Visual Studio Code and add Unity-compatible C# extensions.
3. If your C# extension asks for a .NET SDK/runtime, install the version it requests.
4. Reopen the IDE after installing extensions or SDK/runtime components.

### 4. Open the Project in Unity Hub
1. In Unity Hub, select **Add**.
2. Choose the project folder that contains this repository.
3. Open it using Unity **6000.3.11f1**.

### 4.1 Select the Correct Folder (Important)
When Unity Hub asks for the project folder, select:

- `DOTS_RTS_Prototype`

Do **not** select the parent repository folder (`DOTS`) directly. The Unity project files are inside `DOTS_RTS_Prototype`.

## Where the Project Files Are

For absolute beginners, this is the most important folder map:

- `DOTS_RTS_Prototype/Assets`: Game content (scenes, prefabs, scripts, ScriptableObjects, materials, textures)
- `DOTS_RTS_Prototype/Assets/Scenes`: Playable and test scenes
- `DOTS_RTS_Prototype/Assets/Scripts`: Runtime gameplay code and systems
- `DOTS_RTS_Prototype/Packages/manifest.json`: Package dependency list
- `DOTS_RTS_Prototype/ProjectSettings`: Unity project configuration
- `DOTS_RTS_Prototype/DOTS_RTS_Prototype.slnx`: C# solution generated for IDE workflows

If you are looking for gameplay testing, start at:

- `DOTS_RTS_Prototype/Assets/Scenes/MainTestScene.unity`

### 5. Let the Initial Import Finish
1. Wait until Unity finishes script compilation and asset import.
2. During first open, you may see transient exceptions while compilation/import is still in progress.
3. If exceptions persist after import completes, verify package installation in Package Manager.

### 6. Verify Required Packages
1. In Unity, open **Window > Package Manager**.
2. Confirm all required packages listed in the Requirements section are installed.
3. If any package failed to install, update/install it from Package Manager and wait for recompile.

### 7. Open the Main Gameplay Test Scene
1. In the Project window, open **Assets/Scenes/MainTestScene.unity**.
2. Use this scene for current gameplay testing (the project does not yet include full production scenes).

### 8. Configure and Open the Code Workspace
1. Set your external script editor in **Edit > Preferences > External Tools > External Script Editor**.
2. Select **Assets > Open C# Project** to generate/open project files in your IDE.

## Unity Navigation Guide (Absolute Beginners)

When Unity opens, these panels are the ones you will use most:

- **Hierarchy**: Shows all objects in the currently open scene.
- **Scene view**: Visual editor viewport where you move around and select objects.
- **Game view**: What the running game camera currently sees.
- **Inspector**: Properties/components of the selected object.
- **Project window**: All files under `Assets`.
- **Console**: Errors, warnings, and logs.

Basic navigation workflow:

1. Open a scene from the **Project** window (`Assets/Scenes/...`).
2. Select objects in **Hierarchy** to inspect/edit them in **Inspector**.
3. Use **Play** (top center toolbar) to run the scene.
4. Watch **Console** for issues during and after entering Play Mode.
5. Stop Play Mode before making persistent scene/prefab edits.

Useful beginner menu paths:

- **Window > Package Manager**: Install/update Unity packages
- **Window > General > Console**: Open console if it is hidden
- **Window > General > Hierarchy**: Restore hierarchy panel
- **Window > General > Inspector**: Restore inspector panel
- **Edit > Preferences > External Tools**: Set script editor

## Troubleshooting

### Compilation errors
- Ensure all required packages are installed and updated in Package Manager.
- If errors persist, clear the **Library** folder and reimport the project.

### IDE does not recognize Unity code symbols
- Ensure Unity has fully opened and the project has run at least once after opening.
- This initialization may be needed each time the project is reopened.
- **Visual Studio:** Regenerate project files if IntelliSense fails.
- **Visual Studio Code:** Open **DOTS_RTS_Prototype\DOTS_RTS_Prototype.slnx** and wait for full load (commonly 2 to 4 minutes, sometimes longer on lower-end PCs or with many extensions).

## Disclaimer

The project in its current state is entirely provisional and due to change considerably. Almost all of the graphical assets are currently placeholders obtained from free collections from [Synty Studios](https://assetstore.unity.com/publishers/5217).

Many current features may be refactored, replaced, or removed entirely in the future. To track development, visit the [DOTS-TFC repository](https://github.com/auslamg/DOTS-TFC) if you are tagged as a Collaborator, or request access.