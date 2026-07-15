using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RTSFramework.UI
{
    [RequireComponent(typeof(RawImage))]
    public class RTSMinimap : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private Camera minimapCamera;
        private RectTransform rectTransform;
        
        private Vector3 mapMin;
        private Vector3 mapMax;
        private bool isInitialized;

        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();

            // 1. Detect terrain dimensions
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                mapMin = terrain.transform.position;
                mapMax = mapMin + terrain.terrainData.size;
            }
            else
            {
                // Default fallback
                mapMin = new Vector3(-100f, 0f, -100f);
                mapMax = new Vector3(100f, 0f, 100f);
            }

            float mapWidth = mapMax.x - mapMin.x;
            float mapHeight = mapMax.z - mapMin.z;
            Vector3 center = mapMin + new Vector3(mapWidth / 2f, 100f, mapHeight / 2f);

            // 2. Spawn Minimap Camera
            GameObject camObj = new GameObject("MinimapCamera");
            camObj.transform.position = center;
            camObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Looking straight down

            minimapCamera = camObj.AddComponent<Camera>();
            minimapCamera.orthographic = true;
            
            // Set orthographic size to fit the entire terrain
            minimapCamera.orthographicSize = Mathf.Max(mapWidth, mapHeight) / 2f;
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f); // Dark background

            // Configure culling mask: Render Default (Terrain) and Minimap layers
            int defaultLayer = 0;
            int minimapLayer = LayerMask.NameToLayer("Minimap");
            int cullingMask = (1 << defaultLayer);
            if (minimapLayer != -1)
            {
                cullingMask |= (1 << minimapLayer);
            }
            else
            {
                cullingMask |= (1 << 8); // fallback
            }
            minimapCamera.cullingMask = cullingMask;

            // 3. Create Render Texture
            RenderTexture rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;

            minimapCamera.targetTexture = rt;

            // 4. Output Render Texture to RawImage component
            GetComponent<RawImage>().texture = rt;

            isInitialized = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            MoveCameraToClick(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            MoveCameraToClick(eventData);
        }

        private void MoveCameraToClick(PointerEventData eventData)
        {
            if (!isInitialized || rectTransform == null) return;

            // Convert screen position to local point inside the RawImage RectTransform
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                // Normalize coordinates relative to RawImage size (0 to 1)
                float u = (localPoint.x - rectTransform.rect.x) / rectTransform.rect.width;
                float v = (localPoint.y - rectTransform.rect.y) / rectTransform.rect.height;

                u = Mathf.Clamp01(u);
                v = Mathf.Clamp01(v);

                // Map U and V to the world coordinates
                float worldX = Mathf.Lerp(mapMin.x, mapMax.x, u);
                float worldZ = Mathf.Lerp(mapMin.z, mapMax.z, v);

                Vector3 targetWorldPos = new Vector3(worldX, 0f, worldZ);

                // Set RTS Camera target position
                var camController = Object.FindAnyObjectByType<RTSFramework.CameraSystem.RTSCameraController>();
                if (camController != null)
                {
                    camController.SetTargetPosition(targetWorldPos);
                }
            }
        }

        private void OnDestroy()
        {
            if (minimapCamera != null && minimapCamera.gameObject != null)
            {
                Destroy(minimapCamera.gameObject);
            }
        }
    }
}
