using RUSH.Players;
using UnityEngine;

namespace RUSH.Animations
{
    public class ClipSoundState : StateMachineBehaviour
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.2f;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animator.TryGetComponent(out PlayerArms arms))
                arms.PlayClip(clip, volume);
        }
    }
}
