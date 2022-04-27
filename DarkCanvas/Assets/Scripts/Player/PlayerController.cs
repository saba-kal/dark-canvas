using DarkCanvas.Input;
using UnityEngine;

namespace DarkCanvas.Player
{
    /// <summary>
    /// Handles player movement and jumping.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float _playerSpeed = 2.0f;
        [SerializeField] private float _jumpHeight = 1.0f;
        [SerializeField] private float _gravityValue = -9.81f;

        private CharacterController _characterController;
        private InputManager _inputManager;
        private Vector3 _playerVelocity;
        private bool _playerIsGrounded;
        private Transform _cameraTransform;

        private void Start()
        {
            _characterController = GetComponent<CharacterController>();
            _inputManager = InputManager.Instance;
            _cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            _playerIsGrounded = _characterController.isGrounded;

            //Check if character grounded and falling at the same time.
            if (_playerIsGrounded && _playerVelocity.y < 0)
            {
                //Set y velocity to 0 so that the character does not fall through the ground.
                _playerVelocity.y = 0;
            }

            //Get movement input.
            var move2d = _inputManager.GetPlayerMovement();
            var move3d = new Vector3(move2d.x, 0f, move2d.y);
            move3d = _cameraTransform.forward * move3d.z + _cameraTransform.right * move3d.x;
            move3d.y = 0; //Y movement will be determined by jumping.

            _characterController.Move(move3d * Time.deltaTime * _playerSpeed);

            //Changes the height position of the character when jump is pressed.
            if (_inputManager.PlayerJumped() && _playerIsGrounded)
            {
                _playerVelocity.y += Mathf.Sqrt(_jumpHeight * -3.0f * _gravityValue);
            }

            //Apply gravity.
            _playerVelocity.y += _gravityValue * Time.deltaTime;

            _characterController.Move(_playerVelocity * Time.deltaTime);
        }
    }
}