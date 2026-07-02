using System.Collections.Generic;
using UnityEngine;

namespace RTSFramework.Buildings
{
    [CreateAssetMenu(fileName = "New Building Data", menuName = "RTS/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [SerializeField] private string buildingName;
        [SerializeField] private float maxHealth = 500f;
        [SerializeField] private float constructionTime = 10f; // seconds
        [SerializeField] private List<BuildingCost> cost = new List<BuildingCost>();
        [SerializeField] private GameObject buildingPrefab;
        [SerializeField] private GameObject ghostPrefab;
        [SerializeField] private float gridSize = 1f;

        public string BuildingName => buildingName;
        public float MaxHealth => maxHealth;
        public float ConstructionTime => constructionTime;
        public List<BuildingCost> Cost => cost;
        public GameObject BuildingPrefab => buildingPrefab;
        public GameObject GhostPrefab => ghostPrefab;
        public float GridSize => gridSize;
    }
}
