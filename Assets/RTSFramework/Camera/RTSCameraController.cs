using UnityEngine;
using UnityEngine.InputSystem;

namespace RTSFramework.CameraSystem
{
    public class RTSCameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 20f;
        [SerializeField] private float movementLerpSpeed = 5f;
        [SerializeField] private bool useScreenEdgePanning = true;
        [SerializeField] private float screenEdgeThreshold = 15f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 50f;
        [SerializeField] private float minHeight = 5f;
        [SerializeField] private float maxHeight = 40f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private float rotationLerpSpeed = 10f;

        private Vector3 targetPosition;
        private Quaternion targetRotation;

        private Vector3 inputDirection;
        private float zoomInput;
        private float rotationInput;

        private void Start()
        {
            targetPosition = transform.position;
            targetRotation = transform.rotation;
        }

        private void Update()
        {
            HandleInputs();
            CalculateMovement();
            CalculateZoom();
            CalculateRotation();
            
            ApplyCameraTransforms();
        }

        private void HandleInputs()
        {
            if (RTSFramework.Input.RTSCheatConsole.Instance != null && RTSFramework.Input.RTSCheatConsole.Instance.IsOpen)
            {
                inputDirection = Vector3.zero;
                zoomInput = 0f;
                rotationInput = 0f;
                return;
            }

            // Keyboard Movement
            float x = 0f;
            float z = 0f;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) z = 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) z = -1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x = -1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x = 1f;

                // Rotation Input
                rotationInput = 0f;
                if (keyboard.qKey.isPressed) rotationInput = 1f;
                if (keyboard.eKey.isPressed) rotationInput = -1f;
            }

            // Screen Edge Panning
            var mouse = Mouse.current;
            if (useScreenEdgePanning && mouse != null)
            {
                Vector2 mousePos = mouse.position.ReadValue();
                if (mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height)
                {
                    if (mousePos.x < screenEdgeThreshold) x = -1f;
                    else if (mousePos.x > Screen.width - screenEdgeThreshold) x = 1f;

                    if (mousePos.y < screenEdgeThreshold) z = -1f;
                    else if (mousePos.y > Screen.height - screenEdgeThreshold) z = 1f;
                }
            }

            inputDirection = new Vector3(x, 0, z).normalized;

            // Zoom Input
            if (mouse != null)
            {
                float scrollY = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scrollY) > 0.1f)
                {
                    zoomInput = -Mathf.Sign(scrollY);
                }
                else
                {
                    zoomInput = 0f;
                }
            }
        }

        private void CalculateMovement()
        {
            if (inputDirection.sqrMagnitude > 0)
            {
                // Move relative to camera's forward/right (projected onto XZ plane)
                Vector3 forward = transform.forward;
                forward.y = 0f;
                forward.Normalize();

                Vector3 right = transform.right;
                right.y = 0f;
                right.Normalize();

                Vector3 direction = (forward * inputDirection.z + right * inputDirection.x).normalized;
                targetPosition += direction * (moveSpeed * Time.deltaTime);
            }
        }

        private void CalculateZoom()
        {
            if (Mathf.Abs(zoomInput) > 0.01f)
            {
                float step = zoomSpeed * 0.05f;
                if (step < 1f) step = 2.5f;

                float desiredY = targetPosition.y + zoomInput * step;
                float clampedY = Mathf.Clamp(desiredY, minHeight, maxHeight);
                float deltaY = clampedY - targetPosition.y;

                if (Mathf.Abs(transform.forward.y) > 0.001f)
                {
                    float deltaX = deltaY * (transform.forward.x / transform.forward.y);
                    float deltaZ = deltaY * (transform.forward.z / transform.forward.y);

                    targetPosition.x += deltaX;
                    targetPosition.y = clampedY;
                    targetPosition.z += deltaZ;
                }

                zoomInput = 0f;
            }
        }

        private void CalculateRotation()
        {
            if (Mathf.Abs(rotationInput) > 0.01f)
            {
                float angle = rotationInput * rotationSpeed * Time.deltaTime;

                // 1. Rotate orientation around global vertical axis (prevents roll/banking)
                targetRotation = Quaternion.Euler(0f, angle, 0f) * targetRotation;

                // 2. Orbit position around the look point on the ground using target forward (avoids lag/drift)
                Vector3 targetForward = targetRotation * Vector3.forward;
                if (Mathf.Abs(targetForward.y) > 0.001f)
                {
                    float distance = -targetPosition.y / targetForward.y;
                    Vector3 pivot = targetPosition + targetForward * distance;

                    Vector3 dir = targetPosition - pivot;
                    dir = Quaternion.Euler(0f, angle, 0f) * dir;
                    targetPosition = pivot + dir;
                }
            }
        }

        private void ApplyCameraTransforms()
        {
            // If rotating, snap position and rotation instantly to prevent lag/inertia drift
            if (Mathf.Abs(rotationInput) > 0.01f)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
            else
            {
                // Smoothly lerp towards target position and rotation when moving/zooming normally
                transform.position = Vector3.Lerp(transform.position, targetPosition, movementLerpSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.deltaTime);
            }
        }
    }
}
