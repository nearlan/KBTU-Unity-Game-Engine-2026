using KBTU.Players;
using UnityEngine;

namespace KBTU.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class Crosshair : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponHandler weaponHandler;
        [SerializeField] private bool hideWhileAiming = true;
        [SerializeField, Min(0f)] private float fadeSpeed = 12f;

        private CanvasGroup canvasGroup;

        private bool ShouldBeVisible =>
            weaponHandler != null
            && weaponHandler.isActiveAndEnabled
            && weaponHandler.CurrentWeapon != null
            && !(hideWhileAiming && weaponHandler.IsAiming);

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void Update()
        {
            float target = ShouldBeVisible ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, fadeSpeed * Time.deltaTime);
        }
    }
}
