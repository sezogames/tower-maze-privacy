using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace TowerMaze
{
    public sealed class PlayerInputHandler : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private bool controlsFlipped;

        public bool ClimbHeld { get; private set; }
        public float HorizontalInput { get; private set; }
        public float VerticalInput { get; private set; }
        public bool HasStartIntent { get; private set; }

        public void Initialize(GameConfig gameConfig)
        {
            config = gameConfig;
            controlsFlipped = false;
        }

        public void SetControlsFlipped(bool flipped)
        {
            controlsFlipped = flipped;
        }

        private void Update()
        {
            float dragAxis = 0f;
            float verticalDragAxis = 0f;
            bool pointerHeld = false;
            float keyboardVertical = 0f;
            bool backgroundPointerPressedThisFrame = false;

            Touchscreen touch = Touchscreen.current;
            if (touch != null)
            {
                TouchControl primaryTouch = touch.primaryTouch;
                if (primaryTouch.press.wasPressedThisFrame && !IsPointerOverUi(primaryTouch.touchId.ReadValue()))
                {
                    backgroundPointerPressedThisFrame = true;
                }

                if (primaryTouch.press.isPressed)
                {
                    pointerHeld = true;
                    dragAxis += primaryTouch.delta.ReadValue().x / Mathf.Max(1f, Screen.width);
                    verticalDragAxis += primaryTouch.delta.ReadValue().y / Mathf.Max(1f, Screen.height);
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                pointerHeld = true;
                dragAxis += mouse.delta.ReadValue().x / Mathf.Max(1f, Screen.width);
                verticalDragAxis += mouse.delta.ReadValue().y / Mathf.Max(1f, Screen.height);
            }

            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi())
            {
                backgroundPointerPressedThisFrame = true;
            }

            Keyboard keyboard = Keyboard.current;
            float keyboardAxis = 0f;
            bool keyboardStartPressed = false;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    keyboardAxis -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    keyboardAxis += 1f;
                }

                if (keyboard.spaceKey.isPressed || keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    pointerHeld = true;
                    keyboardVertical += 1f;
                }

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    pointerHeld = true;
                    keyboardVertical -= 1f;
                }

                keyboardStartPressed =
                    keyboard.aKey.wasPressedThisFrame ||
                    keyboard.dKey.wasPressedThisFrame ||
                    keyboard.leftArrowKey.wasPressedThisFrame ||
                    keyboard.rightArrowKey.wasPressedThisFrame ||
                    keyboard.spaceKey.wasPressedThisFrame ||
                    keyboard.wKey.wasPressedThisFrame ||
                    keyboard.upArrowKey.wasPressedThisFrame ||
                    keyboard.sKey.wasPressedThisFrame ||
                    keyboard.downArrowKey.wasPressedThisFrame;
            }

            ClimbHeld = pointerHeld;
            float sensitivity = config != null ? config.dragSensitivity : 2.4f;
            float controlDirection = controlsFlipped ? -1f : 1f;
            HorizontalInput = Mathf.Clamp(-((dragAxis * sensitivity) + keyboardAxis) * controlDirection, -1f, 1f);
            VerticalInput = ResolveVerticalInput(pointerHeld, verticalDragAxis * sensitivity, keyboardVertical) * controlDirection;
            HasStartIntent = keyboardStartPressed || backgroundPointerPressedThisFrame;
        }

        private float ResolveVerticalInput(bool pointerHeld, float verticalDragInput, float keyboardVerticalInput)
        {
            if (Mathf.Abs(keyboardVerticalInput) > 0.01f)
            {
                return Mathf.Clamp(keyboardVerticalInput, -1f, 1f);
            }

            float deadZone = config != null ? config.verticalDragDeadZone : 0.08f;
            if (Mathf.Abs(verticalDragInput) > deadZone)
            {
                return Mathf.Clamp(verticalDragInput, -1f, 1f);
            }

            return pointerHeld ? 1f : 0f;
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId);
        }
    }
}
