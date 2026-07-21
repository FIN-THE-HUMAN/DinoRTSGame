using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using RTSFramework.Commands;
using RTSFramework.Selection;

namespace RTSFramework.Units
{
    public class UnitController : MonoBehaviour, ISelectable
    {
        [SerializeField] private GameObject selectionVisual;
        [SerializeField] private Factions.Faction faction;
        [SerializeField] private UnitData unitData;

        private readonly Queue<Command> commandQueue = new Queue<Command>();
        private Command currentCommand;

        public Transform Transform => transform;
        public GameObject GameObject => gameObject;
        public Factions.Faction Faction => faction;
        public UnitData UnitData => unitData;
        public bool IsPlayerOwned => faction != null && faction.IsPlayerFaction;
        public bool HasActiveCommand => currentCommand != null || commandQueue.Count > 0;
        public Command CurrentCommand => currentCommand;

        private void OnEnable()
        {
            SelectionManager.RegisterSelectable(this);
        }

        private void OnDisable()
        {
            SelectionManager.UnregisterSelectable(this);
        }

        private float baseMoveSpeed;

        private void Start()
        {
            Deselect();
            ApplyFactionColor();

            var agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                baseMoveSpeed = agent.speed;
            }

            // Dynamically attach Fog of War components based on ownership
            if (IsPlayerOwned)
            {
                if (GetComponent<RTSFramework.Fog.FogRevealer>() == null)
                {
                    var rev = gameObject.AddComponent<RTSFramework.Fog.FogRevealer>();
                    rev.SightRange = 10f;
                }
            }
            else
            {
                if (GetComponent<RTSFramework.Fog.FogReceiver>() == null)
                {
                    gameObject.AddComponent<RTSFramework.Fog.FogReceiver>();
                }
            }

            if (Upgrades.UpgradeManager.Instance != null)
            {
                Upgrades.UpgradeManager.Instance.OnUpgradeCompleted += HandleUpgradeCompleted;
                RecalculateMovementSpeed();
            }
        }

        private void OnDestroy()
        {
            if (Upgrades.UpgradeManager.HasInstance)
            {
                Upgrades.UpgradeManager.Instance.OnUpgradeCompleted -= HandleUpgradeCompleted;
            }
        }

        private void HandleUpgradeCompleted(RTSFramework.Factions.Faction upgradeFaction, Upgrades.UpgradeData upgrade)
        {
            if (upgradeFaction == faction)
            {
                RecalculateMovementSpeed();
            }
        }

        private void RecalculateMovementSpeed()
        {
            var agent = GetComponent<NavMeshAgent>();
            if (agent == null || faction == null) return;

            string targetTag = unitData != null ? unitData.UnitName : "";
            agent.speed = Upgrades.UpgradeManager.Instance.GetModifiedValue(faction, targetTag, Upgrades.UpgradeEffectType.MoveSpeed, baseMoveSpeed);
        }

        private void ApplyFactionColor()
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
                mat.color = IsPlayerOwned ? Color.green : Color.red;
                renderer.sharedMaterial = mat;
            }
        }

        private void Update()
        {
            ProcessCommands();
        }

        public void GiveCommand(Command command, bool queue = false)
        {
            if (!queue)
            {
                CancelAllCommands();
            }
            commandQueue.Enqueue(command);
        }

        private void ProcessCommands()
        {
            if (currentCommand == null && commandQueue.Count > 0)
            {
                currentCommand = commandQueue.Dequeue();
                currentCommand.Execute(gameObject);
            }

            if (currentCommand != null)
            {
                currentCommand.Update(gameObject);
                if (currentCommand.IsFinished)
                {
                    currentCommand = null;
                }
            }
        }

        public void CancelAllCommands()
        {
            currentCommand?.Cancel(gameObject);
            currentCommand = null;
            foreach (var cmd in commandQueue)
            {
                cmd.Cancel(gameObject);
            }
            commandQueue.Clear();
        }

        public void EvadeFrom(Vector3 dangerPoint, float pushDistance)
        {
            // Only evade if currently idle (no active commands)
            if (HasActiveCommand) return;

            Vector3 diff = transform.position - dangerPoint;
            diff.y = 0f;
            
            Vector3 escapeDir;
            if (diff.sqrMagnitude > 0.01f)
            {
                escapeDir = diff.normalized;
            }
            else
            {
                // If exactly at the danger point, pick a random angle
                float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                escapeDir = new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle));
            }

            Vector3 targetEscape = transform.position + escapeDir * pushDistance;

            // Project onto NavMesh to ensure it is walkable
            if (NavMesh.SamplePosition(targetEscape, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
            {
                targetEscape = hit.position;
            }
            else
            {
                return; // Can't evade here
            }

            // Give a temporary move command to step aside
            GiveCommand(new MoveCommand(targetEscape), false);
        }

        // ISelectable implementation
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

        public void SetFaction(Factions.Faction newFaction)
        {
            this.faction = newFaction;
            ApplyFactionColor();
        }

        private Dictionary<Renderer, Color[]> originalColors = new Dictionary<Renderer, Color[]>();

        public void SetHighlight(bool highlight)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (selectionVisual != null && r.gameObject == selectionVisual) continue;

                if (highlight)
                {
                    if (!originalColors.ContainsKey(r))
                    {
                        var mats = r.materials;
                        Color[] colors = new Color[mats.Length];
                        for (int i = 0; i < mats.Length; i++)
                        {
                            if (mats[i].HasProperty("_BaseColor")) colors[i] = mats[i].GetColor("_BaseColor");
                            else if (mats[i].HasProperty("_Color")) colors[i] = mats[i].GetColor("_Color");
                            else colors[i] = Color.white;
                        }
                        originalColors[r] = colors;
                    }

                    var materials = r.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        Color orig = originalColors[r][i];
                        Color highlightColor = Color.Lerp(orig, Color.white, 0.12f);
                        if (materials[i].HasProperty("_BaseColor")) materials[i].SetColor("_BaseColor", highlightColor);
                        else if (materials[i].HasProperty("_Color")) materials[i].SetColor("_Color", highlightColor);
                    }
                }
                else
                {
                    if (originalColors.TryGetValue(r, out Color[] colors))
                    {
                        var materials = r.materials;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            if (materials[i].HasProperty("_BaseColor")) materials[i].SetColor("_BaseColor", colors[i]);
                            else if (materials[i].HasProperty("_Color")) materials[i].SetColor("_Color", colors[i]);
                        }
                    }
                }
            }
        }
    }
}
