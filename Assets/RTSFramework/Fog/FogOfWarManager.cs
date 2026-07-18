using System.Collections.Generic;
using UnityEngine;

namespace RTSFramework.Fog
{
    public class FogOfWarManager : MonoBehaviour
    {
        private static FogOfWarManager instance;
        public static bool HasInstance => instance != null;

        public static FogOfWarManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Object.FindAnyObjectByType<FogOfWarManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("FogOfWarManager");
                        instance = go.AddComponent<FogOfWarManager>();
                    }
                }
                return instance;
            }
        }

        [Header("Map Settings")]
        [SerializeField] private Vector2 mapMin = new Vector2(-100f, -100f);
        [SerializeField] private Vector2 mapMax = new Vector2(100f, 100f);
        [SerializeField] private int textureResolution = 128;

        [Header("Cheat State")]
        [SerializeField] private bool isCheatRevealed = false;

        private Texture2D fogTexture;
        private Color32[] pixels;
        private List<FogRevealer> activeRevealers = new List<FogRevealer>();
        private GameObject overlayPlane;
        private MeshRenderer overlayRenderer;

        public bool IsCheatRevealed => isCheatRevealed;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Start()
        {
            // Auto-detect terrain dimensions to dynamically map bounds
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                mapMin = new Vector2(terrain.transform.position.x, terrain.transform.position.z);
                mapMax = new Vector2(terrain.transform.position.x + terrain.terrainData.size.x, terrain.transform.position.z + terrain.terrainData.size.z);
                Debug.Log($"FogOfWarManager: Bounds mapped from Terrain bounds: {mapMin} to {mapMax}");
            }

            InitializeTexture();
            CreateOverlayPlane();
        }

        private void InitializeTexture()
        {
            fogTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
            fogTexture.filterMode = FilterMode.Bilinear;
            fogTexture.wrapMode = TextureWrapMode.Clamp;

            pixels = new Color32[textureResolution * textureResolution];
            
            // Initialize completely unexplored (R = 0, G = 0)
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 0, 0, 255);
            }
            fogTexture.SetPixels32(pixels);
            fogTexture.Apply();
        }

        private void CreateOverlayPlane()
        {
            overlayPlane = new GameObject("FogOfWarOverlayPlane");
            overlayPlane.transform.SetParent(transform, false);

            var filter = overlayPlane.AddComponent<MeshFilter>();
            overlayRenderer = overlayPlane.AddComponent<MeshRenderer>();

            // Generate a flat quad spanning the map bounds
            Mesh mesh = new Mesh();
            
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(mapMin.x, 0.15f, mapMin.y),
                new Vector3(mapMax.x, 0.15f, mapMin.y),
                new Vector3(mapMin.x, 0.15f, mapMax.y),
                new Vector3(mapMax.x, 0.15f, mapMax.y)
            };

            int[] triangles = new int[] { 0, 2, 1, 1, 2, 3 };

            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            filter.mesh = mesh;

            // Assign custom translucent overlay material
            Shader fogShader = Shader.Find("RTSFramework/FogOfWarShader");
            if (fogShader != null)
            {
                Material mat = new Material(fogShader);
                mat.SetTexture("_MainTex", fogTexture);
                overlayRenderer.material = mat;
            }
            else
            {
                Debug.LogError("FogOfWarManager: Failed to locate 'RTSFramework/FogOfWarShader' shader asset!");
            }
        }

        public void RegisterRevealer(FogRevealer revealer)
        {
            if (!activeRevealers.Contains(revealer))
            {
                activeRevealers.Add(revealer);
            }
        }

        public void UnregisterRevealer(FogRevealer revealer)
        {
            activeRevealers.Remove(revealer);
        }

        public float GetVisibility(Vector3 worldPos)
        {
            if (isCheatRevealed) return 1.0f;

            // Convert world coords to texture coordinates
            float tx = (worldPos.x - mapMin.x) / (mapMax.x - mapMin.x);
            float ty = (worldPos.z - mapMin.y) / (mapMax.y - mapMin.y);

            if (tx < 0f || tx > 1f || ty < 0f || ty > 1f)
            {
                return 0f; // Return out of bounds as dark
            }

            int u = Mathf.Clamp(Mathf.FloorToInt(tx * textureResolution), 0, textureResolution - 1);
            int v = Mathf.Clamp(Mathf.FloorToInt(ty * textureResolution), 0, textureResolution - 1);

            int index = v * textureResolution + u;
            if (pixels[index].r > 0)
            {
                return 1.0f; // Current vision
            }
            else if (pixels[index].g > 0)
            {
                return 0.5f; // Explored shroud
            }
            
            return 0.0f; // Unexplored
        }

        public void ToggleFogCheat()
        {
            isCheatRevealed = !isCheatRevealed;
            if (overlayPlane != null)
            {
                overlayPlane.SetActive(!isCheatRevealed);
            }
            Debug.Log($"Cheat Console: Fog of War reveal state updated to {isCheatRevealed}");
        }

        private void Update()
        {
            if (isCheatRevealed) return;

            // 1. Clear current visibility (R = 0), preserve explored memory (G)
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i].r = 0;
            }

            // 2. Draw vision circles for each registered sensor
            float mapWidth = mapMax.x - mapMin.x;
            float mapHeight = mapMax.y - mapMin.y;

            if (mapWidth > 0f && mapHeight > 0f)
            {
                for (int i = 0; i < activeRevealers.Count; i++)
                {
                    var rev = activeRevealers[i];
                    if (rev == null) continue;

                    Vector3 pos = rev.transform.position;
                    float range = rev.SightRange;

                    float tx = (pos.x - mapMin.x) / mapWidth;
                    float ty = (pos.z - mapMin.y) / mapHeight;

                    int u = Mathf.RoundToInt(tx * textureResolution);
                    int v = Mathf.RoundToInt(ty * textureResolution);
                    int radius = Mathf.RoundToInt((range / mapWidth) * textureResolution);

                    DrawVisionCircle(u, v, radius);
                }
            }

            // 3. Upload texture modifications to GPU
            fogTexture.SetPixels32(pixels);
            fogTexture.Apply();
        }

        private void DrawVisionCircle(int cx, int cy, int radius)
        {
            int x0 = Mathf.Clamp(cx - radius, 0, textureResolution - 1);
            int x1 = Mathf.Clamp(cx + radius, 0, textureResolution - 1);
            int y0 = Mathf.Clamp(cy - radius, 0, textureResolution - 1);
            int y1 = Mathf.Clamp(cy + radius, 0, textureResolution - 1);

            int r2 = radius * radius;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    if (dx * dx + dy * dy <= r2)
                    {
                        int index = y * textureResolution + x;
                        pixels[index].r = 255;
                        pixels[index].g = 255;
                    }
                }
            }
        }
    }
}
