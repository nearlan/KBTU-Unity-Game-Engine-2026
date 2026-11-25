using System;
using UnityEngine;

namespace RUSH.Players
{
    public class ArmsAnimationEvents : MonoBehaviour
    {
        public event Action CasingEjected;
        public event Action AmmunitionFilled;
        public event Action ReloadEnded;
        public event Action InspectEnded;

        private void OnEjectCasing() => CasingEjected?.Invoke();

        private void OnAmmunitionFill() => AmmunitionFilled?.Invoke();

        private void OnAnimationEndedReload() => ReloadEnded?.Invoke();

        private void OnAnimationEndedInspect() => InspectEnded?.Invoke();

        private void OnAnimationEndedHolster()
        {
        }

        private void OnSlideBack()
        {
        }
    }
}
