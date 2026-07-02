using UnityEngine;

namespace RTSFramework.Factions
{
    [CreateAssetMenu(fileName = "New Faction", menuName = "RTS/Faction")]
    public class Faction : ScriptableObject
    {
        [SerializeField] private string factionName = "New Faction";
        [SerializeField] private Color factionColor = Color.white;
        [SerializeField] private bool isPlayerFaction;

        public string FactionName => factionName;
        public Color FactionColor => factionColor;
        public bool IsPlayerFaction => isPlayerFaction;
    }
}
