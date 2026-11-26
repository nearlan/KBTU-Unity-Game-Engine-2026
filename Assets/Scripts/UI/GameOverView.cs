using RUSH.Core;
using UnityEngine;

namespace RUSH.UI
{
    public class GameOverView : MonoBehaviour
    {
        private static readonly int ActiveBool = Animator.StringToHash("Active");

        [SerializeField] private Entity target;
        [SerializeField] private Animator hudAnimator;
        [SerializeField] private Animator fadeAnimator;
        [SerializeField] private CanvasGroup labelGroup;
        [SerializeField, Min(0f)] private float labelDelay = 0.6f;
        [SerializeField, Min(0.1f)] private float labelFadeSpeed = 3f;

        private bool isGameOver;
        private float labelTimer;

        private void Awake()
        {
            if (labelGroup == null)
                return;

            labelGroup.alpha = 0f;
            labelGroup.interactable = false;
            labelGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            if (target != null)
                target.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (target != null)
                target.Died -= HandleDied;
        }

        private void HandleDied(Entity entity)
        {
            isGameOver = true;
            labelTimer = labelDelay;

            if (hudAnimator != null)
                hudAnimator.SetBool(ActiveBool, false);

            if (fadeAnimator != null)
                fadeAnimator.SetBool(ActiveBool, true);
        }

        private void Update()
        {
            if (!isGameOver || labelGroup == null)
                return;

            if (labelTimer > 0f)
            {
                labelTimer -= Time.deltaTime;
                return;
            }

            labelGroup.alpha = Mathf.MoveTowards(labelGroup.alpha, 1f, labelFadeSpeed * Time.deltaTime);
        }
    }
}
