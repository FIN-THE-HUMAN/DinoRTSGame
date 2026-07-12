using UnityEngine;
using UnityEngine.UI;
using RTSFramework.Selection;
using RTSFramework.Fog;

namespace RTSFramework.Combat
{
    [RequireComponent(typeof(Health))]
    public class WorldSpaceUIController : MonoBehaviour
    {
        [Header("UI Position")]
        [SerializeField] private float heightOffset = 2.2f;
        [SerializeField] private Vector2 barSize = new Vector2(1.2f, 0.15f);

        [Header("Visibility")]
        [SerializeField] private float visibleDurationAfterDamage = 4f;

        private Health health;
        private ISelectable selectable;

        private GameObject canvasObj;
        private Image healthFill;
        private float lastDamageTime = -10f;
        private bool isInitialized;

        private void Awake()
        {
            health = GetComponent<Health>();
            selectable = GetComponent<ISelectable>();
        }

        private void Start()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                heightOffset = col.bounds.size.y + 0.3f;
            }

            SetupWorldSpaceCanvas();

            // Set initial fill state
            UpdateFillAmount(health.CurrentHealth, health.MaxHealth);

            health.OnHealthChanged += HandleHealthChanged;
            isInitialized = true;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnHealthChanged -= HandleHealthChanged;
            }
            if (canvasObj != null)
            {
                Destroy(canvasObj);
            }
        }

        private void OnDisable()
        {
            if (canvasObj != null)
            {
                canvasObj.SetActive(false);
            }
        }

        private void SetupWorldSpaceCanvas()
        {
            // 1. Create Canvas GameObject (unparented to avoid non-uniform scale distortion and rotation shearing!)
            canvasObj = new GameObject("HealthBarCanvas", typeof(RectTransform));
            canvasObj.transform.position = transform.position + Vector3.up * heightOffset;

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            var canvasRt = canvasObj.GetComponent<RectTransform>();
            canvasRt.sizeDelta = barSize;
            canvasRt.localScale = Vector3.one;

            // 2. Create Background border box
            GameObject bgObj = new GameObject("Background", typeof(RectTransform));
            bgObj.transform.SetParent(canvasObj.transform, false);
            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.12f, 0.12f, 0.12f, 0.8f);
            
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            // 3. Create Fill Container
            GameObject fillAreaObj = new GameObject("FillArea", typeof(RectTransform));
            fillAreaObj.transform.SetParent(canvasObj.transform, false);
            var fillAreaRt = fillAreaObj.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.sizeDelta = new Vector2(-0.04f, -0.04f); // 2px border inside background

            // 4. Create Fill Image
            GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            healthFill = fillObj.AddComponent<Image>();
            
            // Set color: Green for Player, Red for Hostile/Neutrals
            healthFill.color = (selectable != null && selectable.IsPlayerOwned) ? 
                new Color(0.2f, 0.85f, 0.3f) : new Color(0.9f, 0.22f, 0.18f);
            
            var fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.sizeDelta = Vector2.zero;

            // Start inactive
            canvasObj.SetActive(false);
        }

        private void HandleHealthChanged(float current, float max)
        {
            lastDamageTime = Time.time;
            UpdateFillAmount(current, max);
        }

        private void UpdateFillAmount(float current, float max)
        {
            if (healthFill == null) return;
            float pct = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            
            var fillRt = healthFill.GetComponent<RectTransform>();
            if (fillRt != null)
            {
                fillRt.anchorMax = new Vector2(pct, 1f);
            }
        }

        private void LateUpdate()
        {
            if (!isInitialized || canvasObj == null) return;

            // Check if selected
            bool isSelected = false;
            if (SelectionManager.Instance != null && selectable != null)
            {
                for (int i = 0; i < SelectionManager.Instance.SelectedObjects.Count; i++)
                {
                    if (SelectionManager.Instance.SelectedObjects[i] == selectable)
                    {
                        isSelected = true;
                        break;
                    }
                }
            }

            // Check if took damage recently
            bool wasDamagedRecently = Time.time - lastDamageTime < visibleDurationAfterDamage;

            // Check if visible through Fog of War (neutral or enemy objects must be revealed)
            bool isVisibleInFog = true;
            if (FogOfWarManager.Instance != null && selectable != null && !selectable.IsPlayerOwned)
            {
                isVisibleInFog = FogOfWarManager.Instance.GetVisibility(transform.position) > 0.9f;
            }

            bool shouldBeVisible = (isSelected || wasDamagedRecently) && !health.IsDead && isVisibleInFog;
            canvasObj.SetActive(shouldBeVisible);

            if (shouldBeVisible)
            {
                // Force billboard rotation towards main camera
                Camera cam = Camera.main;
                if (cam != null)
                {
                    canvasObj.transform.LookAt(canvasObj.transform.position + cam.transform.rotation * Vector3.forward, cam.transform.rotation * Vector3.up);
                }

                // Dynamically update position (crucial during construction scaling)
                var col = GetComponent<Collider>();
                if (col != null)
                {
                    heightOffset = col.bounds.size.y + 0.3f;
                }
                canvasObj.transform.position = transform.position + Vector3.up * heightOffset;
            }
        }
    }
}
