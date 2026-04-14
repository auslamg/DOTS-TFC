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
- **Cinemachine** (`com.unity.cinemachine`)
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

## Recommended Editor Layout

To improve workflow and match the intended development setup for this project, it is recommended to use the provided custom Unity Editor layout.

### Layout File Location

The layout file is included in the repository at:
DOTS\DOTS_RTS_Prototype\Layouts\DOTS_Dev_layout.wlt

### How to Load the Layout in Unity

1. Open the project in Unity.
2. Go to the top-right corner of the Unity Editor.
3. Click the **Layout** dropdown.
4. Select **Load Layout...**.
5. Navigate to: DOTS_RTS_Prototype/Layouts/
6. Select **DOTS_Dev_layout.wlt**.

### What This Layout Provides

This layout is optimized for DOTS debugging and data-oriented scalable development:

- **Scene + Game view visibility** for quick iteration and playtesting  
- **Hierarchy + Entity hierarchy (adaptive)** at the lefmost part of the screen
- **Inspector (Authoring + Runtime switch) and Components tab** at the rightmost part of the screen
- **Dedicated ScriptableObject and Prefab Project windows** at the center, right of the Scene/Game view
- **Console + Systems + Archetypes** at the bottom center
- **Project assets** at the bottom left part of the screen

## Viewing the Game on an Android Device (Unity Remote)

If you want to see and interact with the Game view directly on your Android phone while running the project in the Unity Editor, you can use **Unity Remote**.

This is useful for testing touch input and getting a more realistic feel of gameplay without building the project every time.

### Requirements

- An Android device  
- A USB cable  
- The **Unity Remote 5** app installed from the Google Play Store  
- USB debugging enabled on your Android device  

### Step-by-Step Setup

#### 1. Enable Developer Mode on Android
1. Open **Settings > About Phone**.  
2. Tap **Build Number** multiple times until Developer Mode is enabled.  
3. Go to **Settings > Developer Options**.  
4. Enable **USB Debugging**.  

#### 2. Install Unity Remote
1. On your Android device, install **Unity Remote 5** from the Play Store.  
2. Open the app (it will wait for a connection).  

#### 3. Connect Your Device
1. Plug your Android device into your computer via USB.  
2. If prompted on your phone, allow USB debugging access.  

#### 4. Configure Unity Editor
1. In Unity, go to **Edit > Project Settings > Editor**.  
2. Find the **Unity Remote** section.  
3. Set:
   - **Device** → *Any Android Device*  
   - **Resolution** → *Downsize* (recommended for performance)  
   - **Joystick Source** → *Remote*  

#### 5. Enter Play Mode
1. Press the **Play** button in Unity.  
2. The Game view will now stream to your Android device.  
3. Touch input on your phone will be sent back to the Unity Editor.  

### Notes and Limitations

- Unity Remote streams compressed frames, so **visual quality and performance are reduced**.  
- This is intended for **input testing**, not performance validation.  
- Some rendering features (especially with DOTS/URP) may not appear exactly the same as in a full build.  
- If the device does not connect:
  - Ensure USB debugging is enabled  
  - Try reconnecting the cable  
  - Restart Unity and the app  

### When to Use vs Build

- Use **Unity Remote** for:
  - Quick iteration  
  - Touch input testing  
  - UI interaction checks  

- Use a **full Android build** for:
  - Performance testing  
  - Final visuals validation  
  - Device-specific issues  

## Troubleshooting

### Compilation errors
- Ensure all required packages are installed and updated in Package Manager.
- If using *Any android device* in **Edit > Project Settings > Editor > Unity Remote > Device**, ensure an android device is plugged in and properly configured.
- If errors persist, clear the **Library** folder and reimport the project.

### IDE does not recognize Unity code symbols
- Ensure Unity has fully opened and the project has run at least once after opening.
- This initialization may be needed each time the project is reopened.
- **Visual Studio:** Regenerate project files if IntelliSense fails.
- **Visual Studio Code:** Open **DOTS_RTS_Prototype\DOTS_RTS_Prototype.slnx** and wait for full load (commonly 2 to 4 minutes, sometimes longer on lower-end PCs or when many extensions are installed).
- Storing the project inside the C:/ drive might decrease loading times.
- Multiple instances of the **.NET Host** task may cause trouble with IDE Unity code recognition. Ending them with the Task Manager will solve the issue.


## Disclaimer

The project in its current state is entirely provisional and due to change considerably. Almost all of the graphical assets are currently placeholders obtained from free collections from [Synty Studios](https://assetstore.unity.com/publishers/5217).

Many current features may be refactored, replaced, or removed entirely in the future. To track development, visit the [DOTS-TFC repository](https://github.com/auslamg/DOTS-TFC) if you are tagged as a Collaborator, or request access.