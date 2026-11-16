using KBTU.Players;
using KBTU.Weapons;
using UnityEngine;

namespace KBTU.Animations
{
    public class WeaponSoundState : StateMachineBehaviour
    {
        [SerializeField] private WeaponSoundType soundType;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animator.TryGetComponent(out PlayerArms arms))
                arms.PlayWeaponSound(soundType);
        }
    }
}
