using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RUSH.Weapons
{
    [Serializable]
    public class WeaponRecoil
    {
        [SerializeField, Min(0f)] private float vertical = 1.5f;
        [SerializeField, Min(0f)] private float horizontal = 0.4f;
        [SerializeField, Range(0f, 1f)] private float aimMultiplier = 0.6f;
        [SerializeField, Range(0f, 1f)] private float crouchMultiplier = 0.75f;

        public Vector2 EvaluateKick(bool aiming, bool crouching)
        {
            float scale = (aiming ? aimMultiplier : 1f) * (crouching ? crouchMultiplier : 1f);
            return new Vector2(vertical, Random.Range(-horizontal, horizontal)) * scale;
        }
    }
}
