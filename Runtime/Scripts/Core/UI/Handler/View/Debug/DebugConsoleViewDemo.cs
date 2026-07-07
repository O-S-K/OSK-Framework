using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OSK
{
    public class DebugConsoleViewDemo : DebugConsoleView
    {
        private const string CommandPlaceholder = "Type command...";
        private const string LogPrefix = "Debug Demo";
        private static readonly string[] DemoCommandUsages = { "gold 1000", "gem 250", "spawn", "tab 2", "clear" };

        private Text _demoStatusText;
        private readonly Dictionary<string, Action<string>> _demoCommands = new Dictionary<string, Action<string>>();
        private InputField _damageInput;
        private InputField _objectNameInput;
        private Slider _speedSlider;
        private Dropdown _modeDropdown;
        private Dropdown _primitiveDropdown;
        private InputField _searchInput;
        private InputField _commandInput;
        private Toggle _godModeToggle;
        private Toggle _verboseToggle;
        private Button[] _debugTabs;
        private GameObject _demoObject;
        private int _gold;
        private int _gems;
        private int _spawnCount;
        private int _selectedTab;
        private float _damage = 12.5f;
        private float _moveSpeed = 6f;
        private int _modeIndex;
        private int _primitiveIndex;
        private string _objectName = "Debug Cube";
        private Vector3 _spawnPosition = new Vector3(1f, 2f, 3f);
        private Vector3 _objectPosition = new Vector3(0f, 1f, 4f);
        private Color _debugColor = new Color(0.35f, 0.58f, 0.95f, 1f);
        private Color _objectColor = new Color(0.2f, 0.8f, 0.65f, 1f);
        private bool _godMode;
        private bool _verboseLogs = true;
        private bool _demoCommandsReady;

        // Adds custom inspector sections with the builder API.
        protected override void BuildCustomSections(DebugConsoleView.DebugInspectorBuilder builder, RectTransform content)
        {
            builder.Folder(content, "Workspace")
                .Foldout()
                .TabBar(new[] { "Live", "Data", "Tools" }, _selectedTab, index =>
                {
                    _selectedTab = index;
                    Debug.Log("[Debug Demo] Tab: " + index);
                    UpdateDemoStatus();
                }, out _debugTabs)
                .SearchField("Search", string.Empty, value =>
                {
                    Debug.Log("[Debug Demo] Search: " + value);
                }, out _searchInput)
                .CommandInput("Run", CommandPlaceholder, DemoCommandUsages, ExecuteDemoCommand, out _commandInput);

            builder.Folder(content, "Demo Cheats")
                .Foldout()
                .Label(out _demoStatusText, 12, FontStyle.Bold, 72f)
                .Toggle("God Mode", false, value =>
                {
                    _godMode = value;
                    Debug.Log("[Debug Demo] God Mode: " + _godMode);
                    UpdateDemoStatus();
                }, out _godModeToggle)
                .Toggle("Verbose Logs", true, value =>
                {
                    _verboseLogs = value;
                    Debug.Log("[Debug Demo] Verbose Logs: " + _verboseLogs);
                    UpdateDemoStatus();
                }, out _verboseToggle)
                .Button("Add 100 Gold", () =>
                {
                    _gold += 100;
                    Debug.Log("[Debug Demo] Add gold. Total: " + _gold);
                    UpdateDemoStatus();
                })
                .Button("Spawn Fake Enemy", () =>
                {
                    _spawnCount++;
                    Debug.Log("[Debug Demo] Spawn fake enemy #" + _spawnCount);
                    UpdateDemoStatus();
                })
                .Button("Emit Warning", () => Debug.LogWarning("[Debug Demo] This is a sample warning."))
                .Button("Emit Error", () => Debug.LogError("[Debug Demo] This is a sample error."))
                .ConfirmButton("Reset Demo State", ResetDemoState);

            builder.Folder(content, "Editable Values")
                .FloatField("Damage", _damage, value =>
                {
                    _damage = value;
                    Debug.Log("[Debug Demo] Damage: " + _damage.ToString("0.##"));
                    UpdateDemoStatus();
                }, out _damageInput)
                .Vector3Field("Spawn Pos", _spawnPosition, value =>
                {
                    _spawnPosition = value;
                    Debug.Log("[Debug Demo] Spawn position: " + _spawnPosition);
                    UpdateDemoStatus();
                })
                .SliderField("Speed", _moveSpeed, 0f, 20f, value =>
                {
                    _moveSpeed = value;
                    Debug.Log("[Debug Demo] Speed: " + _moveSpeed.ToString("0.##"));
                    UpdateDemoStatus();
                }, out _speedSlider)
                .DropdownField("Mode", new[] { "Normal", "Hard", "Sandbox" }, _modeIndex, value =>
                {
                    _modeIndex = value;
                    Debug.Log("[Debug Demo] Mode index: " + _modeIndex);
                    UpdateDemoStatus();
                }, out _modeDropdown)
                .ColorField("Tint", _debugColor, value =>
                {
                    _debugColor = value;
                    Debug.Log("[Debug Demo] Tint: " + _debugColor);
                    UpdateDemoStatus();
                });

            builder.Folder(content, "Inventory / Economy")
                .Foldout()
                .InfoRow("Player Id", "100000012345", DebugConsoleView.DebugInspectorBuilder.AccentColor)
                .InfoRow("Session Long", "9876543210123", new Color(0.75f, 0.85f, 1f, 1f))
                .StatusBadge("Server", "ONLINE", new Color(0.1f, 0.55f, 0.25f, 1f))
                .StatusBadge("Sale Pack", "HOT", new Color(0.85f, 0.25f, 0.16f, 1f))
                .ProgressBar("Level", 72f, 100f, new Color(0.35f, 0.58f, 0.95f, 1f))
                .ProgressBar("Storage", 43f, 60f, new Color(0.95f, 0.62f, 0.22f, 1f))
                .ResourceBar("Gold", 1245000L, 2000000L, new Color(1f, 0.78f, 0.18f, 1f), new Color(1f, 0.66f, 0.12f, 1f))
                .ResourceBar("Gems", 8320L, 10000L, new Color(0.35f, 0.75f, 1f, 1f), new Color(0.22f, 0.62f, 1f, 1f))
                .ResourceBar("Energy", 47L, 120L, new Color(0.45f, 1f, 0.45f, 1f), new Color(0.22f, 0.82f, 0.3f, 1f))
                .ResourceBar("Dust", 9876543210L, 12000000000L, new Color(0.7f, 0.55f, 1f, 1f), new Color(0.58f, 0.38f, 0.95f, 1f))
                .Table(
                    new[] { "Item", "Qty", "Value" },
                    new[,]
                    {
                        { "Sword", "1", "2.5K" },
                        { "Potion", "24", "480" },
                        { "Key", "3", "Rare" }
                    });

            builder.Folder(content, "3D Object Demo")
                .Foldout(false)
                .StringField("Name", _objectName, value =>
                {
                    _objectName = value;
                    ApplyObjectState();
                    UpdateDemoStatus();
                }, out _objectNameInput)
                .DropdownField("Primitive", new[] { "Cube", "Sphere", "Capsule" }, _primitiveIndex, value =>
                {
                    _primitiveIndex = value;
                    CreateOrReplaceObject();
                    UpdateDemoStatus();
                }, out _primitiveDropdown)
                .Vector3Field("Position", _objectPosition, value =>
                {
                    _objectPosition = value;
                    ApplyObjectState();
                    UpdateDemoStatus();
                })
                .ColorField("Color", _objectColor, value =>
                {
                    _objectColor = value;
                    ApplyObjectState();
                    UpdateDemoStatus();
                })
                .Button("Create Or Apply", () =>
                {
                    CreateOrReplaceObject();
                    ApplyObjectState();
                    UpdateDemoStatus();
                })
                .Button("Read From Object", () =>
                {
                    PullObjectState();
                    UpdateDemoStatus();
                });

            CreateOrReplaceObject();
            UpdateDemoStatus();
        }

        // Refreshes custom text together with the base inspector.
        protected override void OnInspectorRefresh()
        {
            PullObjectState(false);
            UpdateDemoStatus();
        }

        // Updates the demo status text.
        private void UpdateDemoStatus()
        {
            if (_demoStatusText == null)
            {
                return;
            }

            string objectState = _demoObject != null
                ? " | Object: " + _demoObject.name + " " + _demoObject.transform.position.ToString("F1")
                : " | Object: none";

            _demoStatusText.text =
                "Gold: " + _gold +
                " | Gems: " + _gems +
                " | Spawns: " + _spawnCount +
                " | Damage: " + _damage.ToString("0.##") +
                " | Speed: " + _moveSpeed.ToString("0.##") +
                " | Mode: " + _modeIndex +
                " | Pos: " + _spawnPosition.ToString("F1") +
                " | God: " + (_godMode ? "ON" : "OFF") +
                " | Verbose: " + (_verboseLogs ? "ON" : "OFF") +
                objectState;
        }

        // Runs the command typed in the debug command input.
        private void ExecuteDemoCommand(string command)
        {
            EnsureDemoCommands();
            if (RunDebugCommand(command, _demoCommands, DemoCommandUsages, LogPrefix) && _commandInput != null)
            {
                _commandInput.text = string.Empty;
            }

            UpdateDemoStatus();
        }

        // Registers sample commands once, keeping command execution easy to extend.
        private void EnsureDemoCommands()
        {
            if (_demoCommandsReady)
            {
                return;
            }

            _demoCommandsReady = true;
            _demoCommands["gold"] = CommandGold;
            _demoCommands["gem"] = CommandGem;
            _demoCommands["spawn"] = CommandSpawn;
            _demoCommands["tab"] = CommandTab;
            _demoCommands["clear"] = CommandClear;
        }

        // Adds demo currency by command, for example: gold 1000.
        private void CommandGold(string args)
        {
            int amount;
            if (!int.TryParse(args, out amount))
            {
                WarnCommandUsage(LogPrefix, "gold 1000");
                return;
            }

            _gold += amount;
            Debug.Log("[Debug Demo] Command gold +" + amount);
        }

        // Adds demo gems by command, for example: gem 250.
        private void CommandGem(string args)
        {
            int amount;
            if (!int.TryParse(args, out amount))
            {
                WarnCommandUsage(LogPrefix, "gem 250");
                return;
            }

            _gems += amount;
            Debug.Log("[Debug Demo] Command gem +" + amount);
        }

        // Spawns or replaces the demo object.
        private void CommandSpawn(string args)
        {
            _spawnCount++;
            CreateOrReplaceObject();
            Debug.Log("[Debug Demo] Command spawn object.");
        }

        // Switches demo tab by command, for example: tab 2.
        private void CommandTab(string args)
        {
            int tab;
            if (!int.TryParse(args, out tab))
            {
                WarnCommandUsage(LogPrefix, "tab 0");
                return;
            }

            _selectedTab = Mathf.Clamp(tab, 0, 2);
            Debug.Log("[Debug Demo] Command tab " + _selectedTab);
        }

        // Resets the demo state from command input.
        private void CommandClear(string args)
        {
            ResetDemoState();
        }

        // Resets the demo state after the danger button confirm.
        private void ResetDemoState()
        {
            _gold = 0;
            _gems = 0;
            _spawnCount = 0;
            _damage = 12.5f;
            _moveSpeed = 6f;
            Debug.Log("[Debug Demo] State reset.");
            UpdateDemoStatus();
        }

        // Creates or recreates the demo primitive selected by the dropdown.
        private void CreateOrReplaceObject()
        {
            Vector3 lastPosition = _objectPosition;
            if (_demoObject != null)
            {
                lastPosition = _demoObject.transform.position;
                Destroy(_demoObject);
            }

            _demoObject = GameObject.CreatePrimitive(GetPrimitiveType());
            _objectPosition = lastPosition;
            ApplyObjectState();
            Debug.Log("[Debug Demo] Created primitive: " + _demoObject.name);
        }

        // Applies current UI state to the demo object.
        private void ApplyObjectState()
        {
            if (_demoObject == null)
            {
                return;
            }

            _demoObject.name = string.IsNullOrEmpty(_objectName) ? "Debug Object" : _objectName;
            _demoObject.transform.position = _objectPosition;
            Renderer renderer = _demoObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = _objectColor;
            }
        }

        // Reads live object state back into the debug status.
        private void PullObjectState(bool log = true)
        {
            if (_demoObject == null)
            {
                return;
            }

            _objectName = _demoObject.name;
            _objectPosition = _demoObject.transform.position;
            Renderer renderer = _demoObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                _objectColor = renderer.material.color;
            }

            if (_objectNameInput != null && _objectNameInput.text != _objectName)
            {
                _objectNameInput.text = _objectName;
            }

            if (log)
            {
                Debug.Log("[Debug Demo] Read object state: " + _objectName + " " + _objectPosition);
            }
        }

        // Converts dropdown index to a primitive type.
        private PrimitiveType GetPrimitiveType()
        {
            switch (_primitiveIndex)
            {
                case 1:
                    return PrimitiveType.Sphere;
                case 2:
                    return PrimitiveType.Capsule;
                default:
                    return PrimitiveType.Cube;
            }
        }
    }
}
