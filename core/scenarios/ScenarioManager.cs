using Godot;
using System.Collections.Generic;

namespace GameHub.Core.Scenarios;

/// <summary>
/// Singleton that manages all available background scenarios.
/// Scans for ScenarioDefinition resources and provides methods to load/unload them.
/// </summary>
public partial class ScenarioManager : Node
{
    public static ScenarioManager Instance { get; private set; }

    private Dictionary<string, ScenarioDefinition> _scenarios = new();
    private Node _currentScenarioInstance;

    /// <summary>
    /// All registered scenario IDs and their display names.
    /// </summary>
    public IReadOnlyDictionary<string, ScenarioDefinition> AvailableScenarios => _scenarios;

    // Hardcoded list of scenario resource paths for now.
    // In a production build, this would scan a directory.
    private static readonly string[] SCENARIO_PATHS = new[]
    {
        "res://core/scenarios/data/casino.tres",
        "res://core/scenarios/data/bar.tres",
        "res://core/scenarios/data/pirate_ship.tres",
        "res://core/scenarios/data/space.tres",
        "res://core/scenarios/data/cowboy.tres",
        "res://core/scenarios/data/brazil.tres",
        "res://core/scenarios/data/cyberpunk.tres",
    };

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadScenarios();
        }
        else
        {
            QueueFree();
        }
    }

    private void LoadScenarios()
    {
        foreach (string path in SCENARIO_PATHS)
        {
            if (ResourceLoader.Exists(path))
            {
                var def = ResourceLoader.Load<ScenarioDefinition>(path);
                if (def != null && !string.IsNullOrEmpty(def.ScenarioId))
                {
                    _scenarios[def.ScenarioId] = def;
                    GD.Print($"[ScenarioManager] Registered scenario: {def.DisplayName} ({def.ScenarioId})");
                }
            }
        }

        // If no .tres files exist yet, register placeholder definitions in code
        if (_scenarios.Count == 0)
        {
            RegisterDefaults();
        }

        GD.Print($"[ScenarioManager] Total scenarios available: {_scenarios.Count}");
    }

    private void RegisterDefaults()
    {
        RegisterDefault("casino",      "Cassino Royale",   new Color(0.95f, 0.85f, 0.5f),   1.2f);
        RegisterDefault("bar",         "Bar Clandestino",  new Color(0.7f, 0.5f, 0.3f),     0.6f);
        RegisterDefault("pirate_ship", "Navio Pirata",     new Color(0.4f, 0.6f, 0.8f),     0.9f);
        RegisterDefault("space",       "Estação Espacial", new Color(0.2f, 0.15f, 0.4f),    0.7f);
        RegisterDefault("cowboy",      "Saloon Faroeste",  new Color(0.9f, 0.75f, 0.55f),   1.0f);
        RegisterDefault("brazil",      "Boteco Brasileiro",new Color(0.1f, 0.6f, 0.2f),     1.0f);
        RegisterDefault("cyberpunk",   "Cyber Lounge",     new Color(0.6f, 0.1f, 0.8f),     0.8f);
    }

    private void RegisterDefault(string id, string name, Color ambientColor, float lightIntensity)
    {
        _scenarios[id] = new ScenarioDefinition
        {
            ScenarioId = id,
            DisplayName = name,
            Description = $"Cenário: {name}",
            AmbientColor = ambientColor,
            LightIntensity = lightIntensity
        };
    }

    /// <summary>
    /// Load and instance a scenario environment into the given parent node.
    /// </summary>
    public void LoadScenario(string scenarioId, Node parent)
    {
        UnloadCurrentScenario();

        if (!_scenarios.TryGetValue(scenarioId, out var def))
        {
            GD.PrintErr($"[ScenarioManager] Scenario '{scenarioId}' not found!");
            return;
        }

        // If the scenario has an actual scene, instance it
        if (!string.IsNullOrEmpty(def.EnvironmentScenePath) && ResourceLoader.Exists(def.EnvironmentScenePath))
        {
            var scene = ResourceLoader.Load<PackedScene>(def.EnvironmentScenePath);
            _currentScenarioInstance = scene.Instantiate();
            parent.AddChild(_currentScenarioInstance);
        }

        // Apply ambient color/lighting via CanvasModulate or WorldEnvironment
        ApplyAmbientSettings(def, parent);

        GD.Print($"[ScenarioManager] Loaded scenario: {def.DisplayName}");
    }

    /// <summary>
    /// Select a random scenario from the available pool.
    /// </summary>
    public string GetRandomScenarioId()
    {
        var keys = new List<string>(_scenarios.Keys);
        if (keys.Count == 0) return "casino";
        int idx = (int)(GD.Randi() % (uint)keys.Count);
        return keys[idx];
    }

    public void UnloadCurrentScenario()
    {
        if (_currentScenarioInstance != null && IsInstanceValid(_currentScenarioInstance))
        {
            _currentScenarioInstance.QueueFree();
            _currentScenarioInstance = null;
        }
    }

    private void ApplyAmbientSettings(ScenarioDefinition def, Node parent)
    {
        // In a real implementation:
        // 1. Find or create a CanvasModulate and set its color to def.AmbientColor
        // 2. Adjust DirectionalLight2D / PointLight2D intensity via def.LightIntensity
        // 3. Optionally load and play def.AmbientMusicPath through AudioManager
        GD.Print($"[ScenarioManager] Applied ambient: Color={def.AmbientColor}, Light={def.LightIntensity}");
    }
}
