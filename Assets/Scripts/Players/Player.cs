using System.Collections;
using KBTU.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KBTU.Players
{
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerLook), typeof(PlayerWeaponHandler))]
    public class Player : Entity
    {
        [SerializeField] private PlayerArms arms;
        [SerializeField, Min(0f)] private float restartDelay = 3f;

        private PlayerMovement movement;
        private PlayerLook look;
        private PlayerWeaponHandler weapons;

        protected override void Awake()
        {
            base.Awake();
            movement = GetComponent<PlayerMovement>();
            look = GetComponent<PlayerLook>();
            weapons = GetComponent<PlayerWeaponHandler>();
        }

        private void Update()
        {
            bool isAiming = weapons.IsAiming;

            movement.IsAiming = isAiming;
            movement.SprintBlocked = isAiming || weapons.IsBusy || weapons.IsFiring;
            look.SetAiming(isAiming);
            arms.UpdateLocomotion(movement.MoveAmount, movement.IsSprinting, isAiming);
        }

        protected override void Die(DamageInfo killingBlow)
        {
            movement.enabled = false;
            look.enabled = false;
            weapons.enabled = false;
            enabled = false;
            StartCoroutine(RestartAfterDelay());
        }

        private IEnumerator RestartAfterDelay()
        {
            yield return new WaitForSeconds(restartDelay);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
