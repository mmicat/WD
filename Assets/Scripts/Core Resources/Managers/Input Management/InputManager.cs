using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;
using WitchDoctor.CoreResources.Utils.Singleton;
using System.Linq;

namespace WitchDoctor.Managers.InputManagement
{
    public enum ControllerLayout
    {
        KeyboardAndMouse = 0,
        PS5 = 1,
        XBoxSerX = 2
    }

    [RequireComponent(typeof(PlayerInput))] // PlayerInput is important for manual control switching
    public class InputManager : MonoSingleton<InputManager>
    {
        private PlayerInput _playerInput;
        private List<InputDevice> _inputDevices;

        private readonly Dictionary<string, ControllerLayout> ControllerLayoutDict = new Dictionary<string, ControllerLayout>()
        {
            {"KeyboardNMouse", ControllerLayout.KeyboardAndMouse },
            {"PS5", ControllerLayout.PS5 },
            {"XBoxSerX", ControllerLayout.XBoxSerX }
        };

        public static Action onControlSchemeChange;
        public static BaseInputActions InputActions { get; private set; }
        public static BaseInputActions.PlayerActions Player => InputActions.Player;
        public static BaseInputActions.UIActions UI => InputActions.UI;

        public ControllerLayout CurrentControlScheme
        {
            get
            {
                if (ControllerLayoutDict.TryGetValue(
                        _playerInput.currentControlScheme,
                        out var controllerLayout))
                    return controllerLayout;
                else
                    throw new MissingReferenceException(
                        $"{_playerInput.currentControlScheme} does not have a corresponding control scheme accounted for!");
            }
        }

        public bool IsKeyboardAndMouse
        {
            get
            {
                return CurrentControlScheme == ControllerLayout.KeyboardAndMouse;
            }
        }

        #region Overrides
        public override void InitSingleton()
        {
            base.InitSingleton();

            InputActions = new BaseInputActions();

            _playerInput = GetComponent<PlayerInput>();
            if (!_playerInput)
                throw new MissingReferenceException($"Player Input component doesn't exist!");

            _inputDevices = new List<InputDevice>(InputSystem.devices);

            InputActions.Player.Enable();
            InputActions.UI.Enable(); // Even if this works maybe we should disable this later
            _playerInput.neverAutoSwitchControlSchemes = true;
            _playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            InputSystem.onDeviceChange += OnInputChanged;
            _playerInput.onControlsChanged += OnControlSchemeSwitched;

            PreventDualInputs();
            CheckForInputs();
        }

        public override void CleanSingleton()
        {
            InputSystem.onDeviceChange -= OnInputChanged;
            _playerInput.onControlsChanged -= OnControlSchemeSwitched;

            onControlSchemeChange = null;
            InputActions?.Player.Disable();
            InputActions?.UI.Disable();
            InputActions = null;
            _playerInput = null;
            _inputDevices?.Clear();
            _inputDevices = null;
        }
        #endregion

        private void OnInputChanged(InputDevice device, InputDeviceChange change)
        {
            // Debug.Log($"Device: {device}, Change: {change}");

            if (device.GetType() == typeof(DualSenseGamepadHID) || device.GetType() == typeof(XInputController))
            {
                //Debug.Log($"Something happened to gamepad {device}\n Change type: {change}");
            }

            switch (change)
            {
                case InputDeviceChange.Added:
                    Debug.Log("New device added: " + device);
                    _inputDevices.Add(device);
                    CheckForInputs();
                    // _playerInput.SwitchCurrentControlScheme(device);

                    break;

                case InputDeviceChange.Disconnected:
                    Debug.Log($"{device} disconnected");
                    CheckForInputs();
                    break;

                case InputDeviceChange.Reconnected:
                    Debug.Log($"{device} reconnected");
                    CheckForInputs();
                    //_playerInput?.SwitchCurrentControlScheme(device);
                    break;

                case InputDeviceChange.Removed:
                    Debug.Log("Device removed: " + device);
                    _inputDevices.Remove(device);
                    if (_inputDevices.Count == 0)
                        throw new MissingReferenceException("No Input Devices Found! Please connect a new input device");
                    CheckForInputs();
                    break;
            }

        }

        public void OnControlSchemeSwitched(PlayerInput currInput)
        {
            Debug.Log($"Player Input Changed to {currInput.currentControlScheme}");

            onControlSchemeChange?.Invoke();
            PreventDualInputs();
        }

        private void PreventDualInputs()
        {
            string bindingGroup = "";

            switch (InputManager.Instance.CurrentControlScheme)
            {
                case ControllerLayout.KeyboardAndMouse:
                    bindingGroup = InputActions.controlSchemes.First(x => x.name == "KeyboardNMouse").bindingGroup;
                    break;
                case ControllerLayout.PS5:
                    bindingGroup = InputActions.controlSchemes.First(x => x.name == "PS5").bindingGroup;
                    break;
                case ControllerLayout.XBoxSerX:
                    bindingGroup = InputActions.controlSchemes.First(x => x.name == "XBoxSerX").bindingGroup;
                    break;
            }

            InputActions.bindingMask = InputBinding.MaskByGroup(bindingGroup);

        }

        /// <summary>
        /// Checks for inputs, giving a preference 
        /// to Controllers over Keyboard and Mouse
        /// </summary>
        private void CheckForInputs()
        {
            if (_inputDevices.Count > 0)
            {
                var xInputIndex = _inputDevices.FindIndex(0, (x) => x is XInputController);
                var dualsenseIndex = _inputDevices.FindIndex(0, (x) => x is DualSenseGamepadHID);
                var keyboardIndex = _inputDevices.FindIndex(0, (x) => x is Keyboard);
                if (dualsenseIndex >= 0)
                {
                    _playerInput.SwitchCurrentControlScheme(
                        _inputDevices[dualsenseIndex]);
                }
                else if (xInputIndex >= 0)
                {
                    _playerInput.SwitchCurrentControlScheme(
                        _inputDevices[xInputIndex]);
                }
                else if (keyboardIndex >= 0)
                {
                    _playerInput.SwitchCurrentControlScheme(
                        _inputDevices[keyboardIndex],
                        _inputDevices.Find(x => x is Mouse));
                }
                else
                {
                    _playerInput.SwitchCurrentControlScheme(
                        _inputDevices[0]);
                }
            }
            else
                throw new MissingReferenceException("Input device list is empty! Please connect a device");
        }
    }
}