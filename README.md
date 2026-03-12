# DOTS_RTS

## Requirements

To open and work with this Unity DOTS project, ensure you have the following installed:

- **Unity 6000.3 LTS** or later
- **Entities package** (`com.unity.entities`)
- **Entities Graphics package** (`com.unity.entities.graphics`)
- **Burst compiler** (`com.unity.burst`)
- **.NET Framework 9.x** or higher
- **2D Sprite** (`com.unity.2d.sprite`)
- **Visual Studio IDE** (`com.unity.ide.visualstudio`)
- **Input System** (`com.unity.inputsystem`)
- **Physics** (`com.unity.physics`)
- **Universal Render Pipeline** (`com.unity.render-pipelines.universal`)
- **Visual Studio 2019** / **Visual Studio Code** (both require the **Unity** extension)

## Opening the Project

While first opening the project in the **Unity Editor**, the project might throw exceptions persistently while it loads. This is unavoidable expected behaviour since some parts of the code run whenever the GUI updates, and it updates constantly during compilation.

Some of these exceptions may also come from unsuccessful package installation. Ensure all of the required packages are installed with the latest version available. To do this, go to **Window > Package Management > Package Management**.

Although there are currently no full-fledged gameplay scenes, testing is currently done in the ***MainTestScene***, located at `Assets/Scenes/MainTestScene.unity`.

To open the project's code from Unity:

1. Set up the desired IDE in **Edit > Preferences > External Tools > External Script Editor**
2. Go to **Assets > Open C# Project**

## Troubleshooting

**Compilation errors:**
- Ensure all packages are updated in the Package Manager
- Clear the `Library` folder and reimport if compilation errors persist

**IDE doesn't recognize Unity registry code elements:**
- Ensure the project is open in the Unity Editor and has been run at least once after opening (this must be done again every time the project is opened)
- **Visual Studio:** Regenerate project files if IntelliSense fails
- **Visual Studio Code:** Open solution `DOTS_RTS_Prototype\DOTS_RTS_Prototype.slnx` and wait (usually takes 2–4 minutes to load; may take longer on lower-end PCs or if many extensions are installed)

## Disclaimer

The project in its current state is entirely provisional and due to change considerably. Almost all of the graphical assets are currently placeholders obtained from free collections from [Synty Studios](https://assetstore.unity.com/publishers/5217).

Many current features may be refactored, replaced, or removed entirely in the future. To track development, visit the [DOTS-TFC repository](https://github.com/auslamg/DOTS-TFC) if you are tagged as a Collaborator, or request access.