using UnityEngine;

namespace RUSH.Items
{
    [CreateAssetMenu(menuName = "RUSH/Items/Ammo", fileName = "Ammo")]
    public class AmmoDefinition : ItemDefinition
    {
        [SerializeField] private string caliber;

        public string Caliber => caliber;
    }
}
