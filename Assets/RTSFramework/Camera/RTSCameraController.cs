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

        public static RTSCameraController Instance { get; private set; }

        private Vector3 targetPosition;
        private Quaternion targetRotation;

        private Vector3 visualPosition;
        private Quaternion visualRotation;

        private Vector3 inputDirection;
        private float zoomInput;
        private float rotationInput;

        // Shake parameters
        private Vector3 shakeOffset;
        private Vector3 shakeRotationOffset;
        private float shakeTimeRemaining;
        private float shakeIntensity;
        private float shakeDuration;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Camera cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
            }

            targetPosition = transform.position;
            targetRotation = transform.rotation;
            visualPosition = transform.position;
            visualRotation = transform.rotation;
        }

        private void Update()
        {
            HandleInputs();
            CalculateMovement();
            CalculateZoom();
            CalculateRotation();
            UpdateShake();
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

        private void UpdateShake()
        {
            if (shakeTimeRemaining > 0f)
            {
                shakeTimeRemaining -= Time.unscaledDeltaTime;
                float currentPower = shakeIntensity * (shakeTimeRemaining / shakeDuration);

                // Add random vibration offsets
                shakeOffset = Random.insideUnitSphere * currentPower;
                shakeRotationOffset = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ) * (currentPower * 1.5f); // Scale rotation offset by 1.5 degrees per unit intensity
            }
            else
            {
                shakeOffset = Vector3.zero;
                shakeRotationOffset = Vector3.zero;
            }
        }

        public Vector3 GetLookAtPoint()
        {
            Vector3 forward = transform.forward;
            if (Mathf.Abs(forward.y) > 0.001f)
            {
                float distance = -transform.position.y / forward.y;
                return transform.position + forward * distance;
            }
            return transform.position;
        }

        public void TriggerShake(Vector3 explosionPos, float intensity, float duration, float shakeRadius)
        {
            float d = Vector3.Distance(explosionPos, GetLookAtPoint());
            float zoomFactor = (maxHeight - transform.position.y) / (maxHeight - minHeight); // 1 = zoomed in, 0 = zoomed out
            float appliedIntensity = 0f;

            // Dynamically calculate zoom requirements based on explosion radius
            // Larger explosions (radius >= 50m) shake the screen at any zoom level
            float minZoomRequired = Mathf.Clamp01(1f - (shakeRadius / 50f));

            if (d <= shakeRadius && zoomFactor >= minZoomRequired)
            {
                float distScale = Mathf.Clamp01(1f - (d / shakeRadius));
                float zoomScale = 1f;
                if (minZoomRequired < 1f)
                {
                    // Scale zoom factor from minZoomRequired to 1.0f
                    zoomScale = Mathf.Lerp(0.3f, 1.0f, (zoomFactor - minZoomRequired) / (1f - minZoomRequired));
                }
                appliedIntensity = intensity * distScale * zoomScale;
            }

            if (appliedIntensity > 0.01f)
            {
                // Take the maximum intensity if already shaking, and reset duration
                shakeIntensity = Mathf.Max(shakeIntensity, appliedIntensity);
                shakeDuration = duration;
                shakeTimeRemaining = duration;
            }
        }

        private void ApplyCameraTransforms()
        {
            if (Mathf.Abs(rotationInput) > 0.01f)
            {
                visualPosition = targetPosition;
                visualRotation = targetRotation;

                transform.position = visualPosition + shakeOffset;
                transform.rotation = visualRotation * Quaternion.Euler(shakeRotationOffset);
            }
            else
            {
                visualPosition = Vector3.Lerp(visualPosition, targetPosition, movementLerpSpeed * Time.deltaTime);
                visualRotation = Quaternion.Slerp(visualRotation, targetRotation, rotationLerpSpeed * Time.deltaTime);

                transform.position = visualPosition + shakeOffset;
                transform.rotation = visualRotation * Quaternion.Euler(shakeRotationOffset);
            }
        }

        public void SetTargetPosition(Vector3 newWorldPosition)
        {
            Vector3 forward = transform.forward;
            if (Mathf.Abs(forward.y) > 0.001f)
            {
                float distance = -targetPosition.y / forward.y;
                Vector3 lookOffset = forward * distance;
                targetPosition = newWorldPosition - lookOffset;
                targetPosition.y = Mathf.Clamp(targetPosition.y, minHeight, maxHeight);
            }
            else
            {
                targetPosition = new Vector3(newWorldPosition.x, targetPosition.y, newWorldPosition.z);
            }
        }
    }
}
