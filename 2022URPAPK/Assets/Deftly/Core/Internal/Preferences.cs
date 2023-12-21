// (c) Copyright Cleverous 2015. All rights reserved.

using UnityEngine;

namespace Deftly
{
    public class Preferences : ScriptableObject
    {
        // General
        public float Difficulty;
        public bool WeaponPickupAutoSwitch;
        public bool UseRpgElements;
        public bool FriendlyFire;
        public string AssetVersion = "0.7.5";
        public bool PeacefulMode;

        // Damage Types
        public string[] DamageTypes;

        // Floating Text
        public bool UseFloatingText;
        public GameObject TextPrefab;
    }
}