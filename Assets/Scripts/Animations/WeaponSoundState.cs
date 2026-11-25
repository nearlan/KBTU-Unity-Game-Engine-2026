using RUSH.Players;
using RUSH.Weapons;
using UnityEngine;

namespace RUSH.Animations
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
