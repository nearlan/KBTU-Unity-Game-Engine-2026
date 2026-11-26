using RUSH.Players;
using RUSH.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace RUSH.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class AmmoView : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponHandler weaponHandler;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private TMP_Text ammunitionLabel;
        [FormerlySerializedAs("magazineLabel")]
        [SerializeField] private TMP_Text reserveLabel;
        [SerializeField] private string unlimitedReserveText = "--";
        [SerializeField, Min(0f)] private float fadeSpeed = 8f;

        private CanvasGroup canvasGroup;
        private Weapon shownWeapon;
        private int shownAmmunition = -1;
        private int shownReserve = -1;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void Update()
        {
            Weapon weapon = weaponHandler != null ? weaponHandler.CurrentWeapon : null;

            if (weapon != shownWeapon)
            {
                shownWeapon = weapon;
                if (weaponIcon != null && weapon != null && weapon.HudIcon != null)
                    weaponIcon.sprite = weapon.HudIcon;
            }

            if (weapon is Firearm firearm)
                SetCounts(firearm.Ammunition, weaponHandler.HasUnlimitedAmmunition ? int.MaxValue : weaponHandler.ReserveAmmunition);

            float target = weapon != null ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, fadeSpeed * Time.deltaTime);
        }

        private void SetCounts(int ammunition, int reserve)
        {
            if (ammunition != shownAmmunition && ammunitionLabel != null)
            {
                shownAmmunition = ammunition;
                ammunitionLabel.SetText("{0}", ammunition);
            }

            if (reserve != shownReserve && reserveLabel != null)
            {
                shownReserve = reserve;
                if (reserve == int.MaxValue)
                    reserveLabel.SetText(unlimitedReserveText);
                else
                    reserveLabel.SetText("{0}", reserve);
            }
        }
    }
}
