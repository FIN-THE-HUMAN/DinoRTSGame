using UnityEngine;
using RTSFramework.Resources;
using RTSFramework.Buildings;
using RTSFramework.Units;

namespace RTSFramework.UI
{
    public class MinimapMarker : MonoBehaviour
    {
        private static Sprite circleSprite;

        private void Start()
        {
            // 1. Generate circle sprite if not already cached
            if (circleSprite == null)
            {
                circleSprite = CreateCircleSprite();
            }

            // 2. Create child icon GameObject
            GameObject iconObj = new GameObject("MinimapIcon");
            iconObj.transform.SetParent(transform, false);
            
            // Lift it high in the sky to float above the unit mesh
            iconObj.transform.localPosition = new Vector3(0f, 15f, 0f);
            iconObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Set size based on type
            float size = 4f;
            Color markerColor = new Color(0.9f, 0.22f, 0.18f); // Red default

            var resource = GetComponent<ResourceSource>();
            var building = GetComponent<Building>();
            var unit = GetComponent<UnitController>();

            if (resource != null)
            {
                size = 4f;
                markerColor = new Color(1.0f, 0.84f, 0.0f); // Yellow for resource nodes
            }
            else if (building != null)
            {
                size = 10f; // Buildings are larger
                markerColor = building.Faction != null && building.Faction.IsPlayerFaction ? 
                    new Color(0.2f, 0.85f, 0.3f) : new Color(0.9f, 0.22f, 0.18f);
            }
            else if (unit != null)
            {
                size = 5f; // Units are small dots
                markerColor = unit.Faction != null && unit.Faction.IsPlayerFaction ? 
                    new Color(0.2f, 0.85f, 0.3f) : new Color(0.9f, 0.22f, 0.18f);
            }

            iconObj.transform.localScale = new Vector3(size, size, 1f);

            // 3. Setup SpriteRenderer
            var spriteRenderer = iconObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = circleSprite;
            spriteRenderer.color = markerColor;
            spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;

            // Set layer to Minimap layer
            int layer = LayerMask.NameToLayer("Minimap");
            if (layer == -1) layer = 8; // fallback to 8 if not registered
            iconObj.layer = layer;
        }

        private Sprite CreateCircleSprite()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] cols = new Color[size * size];
            float center = size / 2f;
            float radius = size / 2f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        cols[y * size + x] = Color.white;
                    }
                    else
                    {
                        cols[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
