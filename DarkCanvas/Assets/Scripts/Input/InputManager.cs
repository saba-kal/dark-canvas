using DarkCanvas.Common;
using UnityEngine;

namespace DarkCanvas.Input
{
    /// <summary>
    /// Centralized location for managing player input.
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        private PlayerControls _playerControls;

        protected override void Awake()
        {
            base.Awake();
            _playerControls = new PlayerControls();
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnEnable()
        {
            _playerControls.Enable();
        }

        private void OnDisable()
        {
            _playerControls.Disable();
        }

        /// <summary>
        /// Gets a 2D vector of the player's movement input.
        /// </summary>
        public Vector2 GetPlayerMovement()
        {
            return _playerControls.Player.Movement.ReadValue<Vector2>();
        }

        /// <summary>
        /// Gets the difference vector of the mouse position between the previous and current frame.
        /// </summary>
        public Vector2 GetMouseDelta()
        {
            return _playerControls.Player.Look.ReadValue<Vector2>();
        }

        /// <summary>
        /// Tells whether or not the player pressed the jump button.
        /// </summary>
        public bool PlayerJumped()
        {
            return _playerControls.Player.Jump.triggered;
        }

        /// <summary>
        /// Tells whether or not the player pressed the fly up button.
        /// </summary>
        public bool PlayerFlewUp()
        {
            return _playerControls.Player.FlyUp.ReadValue<float>() > 0.5f;
        }

        /// <summary>
        /// Tells whether or not the player pressed the fly down button.
        /// </summary>
        public bool PlayerFlewDown()
        {
            return _playerControls.Player.FlyDown.ReadValue<float>() > 0.5f;
        }
    }
}