using RUSH.Players;
using UnityEngine;

namespace RUSH.UI
{
    public class StaminaDialView : MonoBehaviour
    {
        private static readonly int FillParameter = Animator.StringToHash("Health");

        [SerializeField] private PlayerStamina stamina;
        [SerializeField] private Animator dialAnimator;

        private void Update()
        {
            if (stamina == null || dialAnimator == null)
                return;

            dialAnimator.SetFloat(FillParameter, stamina.Normalized);
        }
    }
}
