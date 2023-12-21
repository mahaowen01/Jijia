// (c) Copyright Cleverous 2015. All rights reserved.

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Audio;

namespace Deftly
{
    public static class StaticUtil
    {
        public static string PrefPath = "Assets/Deftly/Core/Resources/Preferences.asset";
        public static Preferences Preferences;

#if UNITY_EDITOR
        public static Preferences GetPrefsInEditor(bool createIfNeeded)
        {
            Preferences prefs = (Preferences)AssetDatabase.LoadAssetAtPath(PrefPath, typeof(Preferences));
            if (prefs) return prefs;

            if (!createIfNeeded)
            {
                Debug.LogError("Preferences Asset doesn't exist and creation was not allowed. Coulnd't return proper data.");
                return null;
            }

            ResetDamageTypes(prefs);
            prefs = ScriptableObject.CreateInstance<Preferences>();
            AssetDatabase.CreateAsset(prefs, "Assets/Deftly/Core/Resources/Preferences.asset");
            AssetDatabase.SaveAssets();
            return prefs;
        }
        public static void ResetDamageTypes()
        {
            ResetDamageTypes(GetPrefsInEditor(false));
        }
        public static void ResetDamageTypes(Preferences preferences)
        {
            preferences.DamageTypes = new[]
            {
                "Kinetic (Default)",    // 0
                "Fire",                 // 1
                "Ice",                  // 2
                "Electric",             // 3
                "Poison",               // 4
                "Other",                // 5
                "Unused",               // 6
                "Unused",               // 7
                "Unused",               // 8
                "Unused",               // 9
            };
        }
#endif

        public static void Init()
        {
            Preferences = (Preferences)Resources.Load("Preferences");
        }
        public static float[] ResetDamageMultipliers()
        {
            return new float[] {1,1,1,1,1,1,1,1,1,1};
        }
        public static void SpawnFloatingText(Object origin, Vector3 position, string value)
        {
            if (Preferences.UseFloatingText != true) return;
            if (Preferences.TextPrefab == null)
            {
                Debug.LogError("Floating Damage Prefab not found! Check the Deftly Global Options window.");
                return;
            }

            GameObject dmg = Object.Instantiate(Preferences.TextPrefab);
            FloatingText txt = dmg.GetComponent<FloatingText>();
            if (txt == null)
            {
                Debug.LogError("No Floating Text component found on the Floating Text Prefab! Add one to the prefab.");
                return;
            }
            txt.StartPosition = position;
            txt.Value = value;
        }

        /// <summary> Checks to see if a GameObject is on a layer in a LayerMask. </summary>
        /// <returns> True if the provided GameObject's Layer matches one of the Layers in the provided LayerMask. </returns>
        public static bool LayerMatchTest(LayerMask approvedLayers, GameObject objInQuestion)
        {
            return ((1 << objInQuestion.layer) & approvedLayers) != 0;
        }

        public static void SpawnLoot(GameObject prefab, Vector3 location, bool bounceAndSpin)
        {
            GameObject newLoot = (GameObject)Object.Instantiate(prefab, location + Vector3.up, Random.rotation);
            if (bounceAndSpin)
            {
                Rigidbody rb = newLoot.GetComponent<Rigidbody>();
                if (rb != null) rb.AddExplosionForce(25, location, 2);
            }
        }

        /// <summary>Wrapper for your pooling solution integration.</summary>
        public static Object Spawn(Object original, Vector3 position, Quaternion rotation)
        {
            // Sweet pooling code here
            return Object.Instantiate(original, position, rotation) as GameObject;
        }
        /// <summary>Wrapper for your pooling solution integration.</summary>
        public static void DeSpawn(Object target)
        {
            // Sweet pooling code here
            Object.Destroy(target);
        }

        public static void GiveXp(int amount, Subject target)
        {
            int val = (int)target.Stats.Experience.Actual + amount;
            if (val >= target.Stats.Experience.Max)
            {
                target.Stats.Experience.Actual += amount;
                target.LevelUp();
            }
            else target.Stats.Experience.Actual += amount;
        }
        public static float StatShouldBe(Stat stat, int level)
        {
            return (stat.Base + stat.IncreasePerLevel) * level;
        }
        public static bool SameTeam(Subject guy, Subject otherGuy)
        {
            return guy.Stats.TeamId == otherGuy.Stats.TeamId;
        }

        /// <summary>Replacement for Unity's built in PlayClipAtPoint since it offers no control of the AudioSource and defaults to 2d spatial blend.</summary>
        public static void PlaySoundAtPoint(AudioClip sound, Vector3 position, float volume = 1, float maxDistance = 100, AudioMixerGroup mixer = null, bool is3D = true, float pitch = 1)
        {
            // Spawn host
            GameObject host = new GameObject {name = ".Sound: " + sound.name};
            host.transform.position = position;
            
            // Setup source
            AudioSource player = host.AddComponent<AudioSource>();
            player.outputAudioMixerGroup = mixer;
            player.spatialBlend = is3D?1:0;
            player.pitch = pitch;
            player.volume = volume;
            player.maxDistance = maxDistance;

            // do teh noiz
            player.clip = sound;
            player.Play();

            // Get rid of this GameObject after a time
            Lifetimer.AddTimer(host, sound.length, false);
        }
    }
}