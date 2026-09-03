# Game Hub - Multiplayer Pixel Art Card Game

Welcome to the **Game Hub** repository! This is a scalable, modular platform built in **Godot 4.3 (.NET/C#)** designed to host multiple high-quality 2D/3D pixel art multiplayer games, starting with a Poker Roguelike and a Truco Roguelike.

## 🛠 Prerequisites

To compile, run, and edit this project, you will need:

1. **Godot Engine 4.7 (.NET Version)**: [Download Godot 4.7 (.NET)](https://godotengine.org/download)
   > *Note: You must download the **.NET version** to compile C# scripts. The standard version will not work.*
2. **.NET 8.0 SDK**: [Download .NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
3. **IDE**: Visual Studio 2022, Visual Studio Code, or JetBrains Rider for C# editing.

## 🚀 How to Run the Game

### Method 1: Using the Godot Editor (Recommended for Developers)
1. Open Godot 4.7 (.NET).
2. Click **Import** and select the `project.godot` file inside this repository.
3. Click **Save & Open**.
4. In the top right corner of the editor, click **Build** to compile the C# solution for the first time.
5. Press **F5** (or click the Play icon ⏯️) to run the Game Hub.

### Method 2: Running via Command Line (Headless/CI)
Open your terminal in the project directory and run:
```bash
dotnet build
godot --path .
```

## 🎮 Features & Navigation

When the Hub launches, you will find:
* **Play Menu**: Access quick matches, LAN lobbies, or the **Tutorial** mode (where bots will teach you the mechanics).
* **Settings**: A comprehensive configuration menu supporting:
  * *Graphics & Visuals* (Resolution, Pixel Scaling, VFX toggles).
  * *Controls* (Keyboard/Mouse/Controller remapping).
  * *Audio* (Master, Music, SFX sliders).
  * *Profile* (Nickname and Avatar/Character selection).
  * *Accessibility* (Colorblind modes, text scaling, screen shake toggles).
* **Dynamic Scenarios**: Matches take place across various 2.5D backgrounds (Casino, Wild West, Pirate Ship, Space, Cyberpunk, Brazil).

## 📁 Architecture Overview

* `core/`: Global Singletons (Network, Audio, Save, Game Registry).
* `hub/`: Main menu UI, settings screens, and lobby management.
* `games/`: Isolated modules for each playable game (e.g., `poker_roguelike`, `truco_roguelike`).

## 🤝 Contributing
Make sure to build the C# solution locally before submitting changes. Avoid committing `/.godot/` and `/data/saves/` folders.
