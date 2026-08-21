using UnityEngine;
using UnityEngine.InputSystem;

namespace ActionRPG.Input
{
    public class InputHandler : MonoBehaviour
    {
        public static InputHandler Instance { get; private set; }

        public Vector2 MoveInput { get; private set; }
        public Vector2 MousePosition { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool BlockHeld { get; private set; }
        public bool Weapon1Pressed { get; private set; }
        public bool Weapon2Pressed { get; private set; }
        public bool GameplayInputEnabled { get; private set; } = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!GameplayInputEnabled)
            {
                ClearGameplayInput();
                return;
            }

            // Read Movement (WASD / Arrow Keys)
            Vector2 move = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) move.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) move.y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) move.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) move.x += 1f;

                Weapon1Pressed = Keyboard.current.digit1Key.wasPressedThisFrame;
                Weapon2Pressed = Keyboard.current.digit2Key.wasPressedThisFrame;
            }
            MoveInput = move.sqrMagnitude > 1f ? move.normalized : move;

            // Read Mouse
            if (Mouse.current != null)
            {
                MousePosition = Mouse.current.position.ReadValue();
                AttackPressed = Mouse.current.leftButton.wasPressedThisFrame;
                AttackHeld = Mouse.current.leftButton.isPressed;
                BlockHeld = Mouse.current.rightButton.isPressed;
            }
            else
            {
                AttackPressed = false;
                AttackHeld = false;
                BlockHeld = false;
            }
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            GameplayInputEnabled = enabled;
            if (!enabled) ClearGameplayInput();
        }

        private void ClearGameplayInput()
        {
            MoveInput = Vector2.zero;
            AttackPressed = false;
            AttackHeld = false;
            BlockHeld = false;
            Weapon1Pressed = false;
            Weapon2Pressed = false;
        }
    }
}
