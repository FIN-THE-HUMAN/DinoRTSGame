using UnityEngine;
using RTSFramework.Selection;

namespace RTSFramework.Resources
{
    public class ResourceSource : MonoBehaviour, ISelectable
    {
        [SerializeField] private ResourceType resourceType;
        [SerializeField] private int maxAmount = 500;
        [SerializeField] private int currentAmount;

        private GameObject selectionVisual;

        public ResourceType ResourceType => resourceType;
        public int CurrentAmount => currentAmount;
        public int MaxAmount => maxAmount;
        public bool IsDepleted => currentAmount <= 0;

        // ISelectable implementation
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;
        public bool IsPlayerOwned => false; // resource nodes are neutral

        public void Select()
        {
            if (selectionVisual != null)
            {
                selectionVisual.SetActive(true);
            }
        }

        public void Deselect()
        {
            if (selectionVisual != null)
            {
                selectionVisual.SetActive(false);
            }
        }

        private void Awake()
        {
            currentAmount = maxAmount;
            SetupSelectionVisual();
        }

        private void Start()
        {
            SelectionManager.RegisterSelectable(this);
            Deselect();
        }

        private void OnDestroy()
        {
            SelectionManager.UnregisterSelectable(this);
        }

        public int Gather(int amountToGather)
        {
            if (IsDepleted) return 0;

            int gathered = Mathf.Min(amountToGather, currentAmount);
            currentAmount -= gathered;

            if (currentAmount <= 0)
            {
                Deplete();
            }

            return gathered;
        }

        private void Deplete()
        {
            // Trigger visual depletion or destroy the resource node
            Destroy(gameObject);
        }

        private void SetupSelectionVisual()
        {
            if (selectionVisual == null)
            {
                Transform existing = transform.Find("SelectionVisual");
                if (existing != null)
                {
                    selectionVisual = existing.gameObject;
                }
                else
                {
                    GameObject selVisualObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    selVisualObj.name = "SelectionVisual";
                    selVisualObj.transform.SetParent(transform, false);

                    var col = GetComponent<Collider>();
                    float bottomY = 0.02f;
                    float sizeVal = 2.0f;
                    if (col != null)
                    {
                        bottomY = (col.bounds.center.y - col.bounds.extents.y) - transform.position.y + 0.02f;
                        sizeVal = Mathf.Max(col.bounds.size.x, col.bounds.size.z) * 1.3f;
                    }
                    selVisualObj.transform.localPosition = new Vector3(0f, bottomY, 0f);
                    selVisualObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    selVisualObj.transform.localScale = new Vector3(sizeVal, sizeVal, 1f);

                    Destroy(selVisualObj.GetComponent<Collider>());

                    selectionVisual = selVisualObj;
                    ApplySelectionColor();
                }
            }
        }

        private void ApplySelectionColor()
        {
            if (selectionVisual == null) return;
            var renderer = selectionVisual.GetComponent<Renderer>();
            if (renderer == null) return;

            Shader spritesShader = Shader.Find("Sprites/Default");
            if (spritesShader == null) spritesShader = Shader.Find("Unlit/Color");
            if (spritesShader == null) spritesShader = Shader.Find("Standard");

            if (spritesShader != null)
            {
                Material mat = new Material(spritesShader);
                mat.color = new Color(1.0f, 0.82f, 0.0f); // Gold/Yellow selection ring
                renderer.sharedMaterial = mat;
            }
        }
    }
}
