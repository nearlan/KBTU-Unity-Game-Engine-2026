using System;
using UnityEngine;

namespace RUSH.Weapons
{
    [Serializable]
    public class WeaponSounds
    {
        [SerializeField] private AudioClip holster;
        [SerializeField] private AudioClip unholster;
        [SerializeField] private AudioClip reload;
        [SerializeField] private AudioClip reloadEmpty;
        [SerializeField] private AudioClip fire;
        [SerializeField] private AudioClip fireEmpty;

        public AudioClip Get(WeaponSoundType type) => type switch
        {
            WeaponSoundType.Holster => holster,
            WeaponSoundType.Unholster => unholster,
            WeaponSoundType.Reload => reload,
            WeaponSoundType.ReloadEmpty => reloadEmpty,
            WeaponSoundType.Fire => fire,
            WeaponSoundType.FireEmpty => fireEmpty,
            _ => null
        };
    }
}
