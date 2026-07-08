using System.Collections.Generic;
using UnityEngine;
using RTSFramework.Selection;

namespace RTSFramework.Fog
{
    public class FogReceiver : MonoBehaviour
    {
        private struct RendererState
        {
            public Renderer renderer;
            public bool originalEnabled;
        }

        private struct ColliderState
        {
            public Collider collider;
            public bool originalEnabled;
        }

        private List<RendererState> cachedRenderers = new List<RendererState>();
        private List<ColliderState> cachedColliders = new List<ColliderState>();

        private ISelectable selectable;
        private bool isCurrentlyVisible = true;
        private float checkInterval = 0.2f;
        private float nextCheckTime;
        private bool isInitialized;

        private void Start()
        {
            selectable = GetComponent<ISelectable>();

            // Cache all renderers and their original states
            var renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                
                // Skip canvas renderers (like billboard health bars)
                if (r.gameObject.GetComponent<CanvasRenderer>() != null) continue;
                
                cachedRenderers.Add(new RendererState { renderer = r, originalEnabled = r.enabled });
            }

            // Cache all colliders and their original states
            var colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                var c = colliders[i];
                cachedColliders.Add(new ColliderState { collider = c, originalEnabled = c.enabled });
            }

            isInitialized = true;
            
            // Run initial visibility setup
            UpdateVisibility(true);
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Throttled checking (5 times a second) to avoid CPU bottlenecks
            if (Time.time >= nextCheckTime)
            {
                nextCheckTime = Time.time + checkInterval;
                UpdateVisibility(false);
            }
        }

        private void UpdateVisibility(bool forceUpdate)
        {
            if (FogOfWarManager.Instance == null) return;

            // Check visibility value (1 = visible, else shrouded/dark)
            float vis = FogOfWarManager.Instance.GetVisibility(transform.position);
            bool targetVisible = vis > 0.9f;

            if (targetVisible != isCurrentlyVisible || forceUpdate)
            {
                isCurrentlyVisible = targetVisible;
                SetVisibility(isCurrentlyVisible);
            }
        }

        private void SetVisibility(bool visible)
        {
            // Toggle Renderers
            for (int i = 0; i < cachedRenderers.Count; i++)
            {
                var rState = cachedRenderers[i];
                if (rState.renderer != null)
                {
                    rState.renderer.enabled = visible ? rState.originalEnabled : false;
                }
            }

            // Toggle Colliders
            for (int i = 0; i < cachedColliders.Count; i++)
            {
                var cState = cachedColliders[i];
                if (cState.collider != null)
                {
                    cState.collider.enabled = visible ? cState.originalEnabled : false;
                }
            }

            // If hidden, force deselect
            if (!visible && SelectionManager.Instance != null && selectable != null)
            {
                SelectionManager.Instance.Deselect(selectable);
            }
        }
    }
}
