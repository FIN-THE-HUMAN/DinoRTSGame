using System.Collections.Generic;
using UnityEngine;
using RTSFramework.Buildings;

namespace RTSFramework.Units
{
    [CreateAssetMenu(fileName = "New Unit Data", menuName = "RTS/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [SerializeField] private string unitName;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float trainingTime = 5f; // seconds
        [SerializeField] private List<BuildingCost> cost = new List<BuildingCost>();
        [SerializeField] private GameObject unitPrefab;
        
        [Header("UI Config")]
        [SerializeField] private Sprite unitIcon;
        [SerializeField] private int selectionPriority = 10;

        [Header("Audio Config")]
        [SerializeField] private List<AudioClip> selectVoices = new List<AudioClip>();
        [SerializeField] private List<AudioClip> commandVoices = new List<AudioClip>();

        public string UnitName => unitName;
        public float MaxHealth => maxHealth;
        public float TrainingTime => trainingTime;
        public List<BuildingCost> Cost => cost;
        public GameObject UnitPrefab => unitPrefab;
        public Sprite UnitIcon => unitIcon;
        public int SelectionPriority => selectionPriority;
        public IReadOnlyList<AudioClip> SelectVoices => selectVoices;
        public IReadOnlyList<AudioClip> CommandVoices => commandVoices;
    }
}
