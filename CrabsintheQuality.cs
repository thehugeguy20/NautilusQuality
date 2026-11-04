using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using RoR2;
using ItemQualities;
using ItemQualities.Utilities.Extensions;
using System.Collections.Generic;
using System.Threading;
using R2API;

namespace NautilusQuality
{
    internal static class CrabsintheQuality
    {
        private static readonly float[] RegenToBarrier = { 2.0f, 5.0f, 10.0f, 20.0f };

        private const float SpreadRadius = 20f;
        private const float BaseBarrierCapPercent = 0.40f; // 40% base cap
        private const float CapPerStack = 0.10f;  // +10% per stack

        public static void Init()
        {
            On.RoR2.HealthComponent.Heal += OnHeal;
            RecalculateStatsAPI.GetStatCoefficients += ReduceBarrierDecay;
            On.RoR2.CharacterBody.OnInventoryChanged += OnInventoryChanged;
        }

        private static void OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);

            if (self.inventory == null) return;

            var counts = self.inventory.GetItemCountsEffective(QualityCompat.GetGroup("Crabsinthe"));

            if (counts.TotalCount > 0 && counts.HighestQuality != QualityTier.None)
            {
                UpdatePlayerVisuals(self, counts.TotalCount, counts.HighestQuality);
            }
        }

        private static void ReduceBarrierDecay(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.inventory == null) return;

            var counts = sender.inventory.GetItemCountsEffective(QualityCompat.GetGroup("Crabsinthe"));

            if (counts.TotalCount > 0 && counts.HighestQuality != QualityTier.None)
            {
                args.barrierDecayMult *= 0.93f;
            }
        }

        private static float OnHeal(On.RoR2.HealthComponent.orig_Heal orig, HealthComponent self, float amount, ProcChainMask procChainMask, bool nonRegen)
        {
            var healed = orig(self, amount, procChainMask, nonRegen);

            // only for regen
            if (nonRegen || healed <= 0f || !NetworkServer.active)
            {
                return healed;
            }

            var body = self.body;
            if (body == null || body.inventory == null)
            {
                return healed;
            }

            var counts = body.inventory.GetItemCountsEffective(QualityCompat.GetGroup("Crabsinthe"));
            var highestQuality = counts.HighestQuality;

            if (highestQuality != QualityTier.None)
            {
                float barrierGained = healed * RegenToBarrier[(int)highestQuality];
                
                self.AddBarrier(barrierGained);

                SpreadBarrierToAllies(self, barrierGained, counts.TotalCount);
            }

            return healed;
        }

        private static void SpreadBarrierToAllies(HealthComponent selfHealth, float barrierAmount, int totalItemCount)
        {
            if (selfHealth.body == null || !selfHealth.body.transform) return;

            Vector3 position = selfHealth.body.transform.position;
            TeamIndex teamIndex = selfHealth.body.teamComponent ? selfHealth.body.teamComponent.teamIndex : TeamIndex.None;

            // looping through allies
            foreach (var ally in TeamComponent.GetTeamMembers(teamIndex))
            {
                if (!ally || !ally.body || ally.body.healthComponent == selfHealth)
                {
                    continue;
                }

                var allyBody = ally.body;
                var allyHealth = allyBody.healthComponent;
                if (!allyHealth) continue;

                // check if theyre within the 20m radius
                float sqrDist = (allyBody.transform.position - position).sqrMagnitude;
                if (sqrDist > SpreadRadius * SpreadRadius)
                {
                    continue;
                }

                // calculate ally's individual max barrier cap (40% + 10% per stack)
                float maxAllowedBarrier = allyBody.maxHealth * (BaseBarrierCapPercent + (CapPerStack * totalItemCount));
                
                // apply barrier increase if barrier isn't over maxAllowedBarrier
                if (allyHealth.barrier / allyHealth.fullHealth < maxAllowedBarrier)
                {
                    float barrierToAdd = barrierAmount;
                    allyHealth.AddBarrier(barrierToAdd);
                }
            }
        }

        private static void UpdatePlayerVisuals(CharacterBody body, int totalCount, QualityTier highestQuality)
        {
            var indicator = body.GetComponent<CrabsintheRadiusEffect>();

            if (totalCount > 0 && highestQuality != QualityTier.None)
            {
                if (!indicator)
                {
                    body.gameObject.AddComponent<CrabsintheRadiusEffect>();
                }
            }
            else
            {
                if (indicator)
                {
                    Object.Destroy(indicator);
                }
            }
        }

    }


    public class CrabsintheRadiusEffect : MonoBehaviour
    {
        private GameObject indicator;
        private const float Radius = 20f;

        private GameObject radiusPrefab;

        private void Start()
        {
            GameObject radiusPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Items/AttackSpeedPerNearbyAllyOrEnemy/BolsteringLanternBonusIndicator.prefab").WaitForCompletion();

            if (!radiusPrefab )
            {
                Debug.LogError("NautilusQuality: crabsinthe didnt load bolstlantern prefab");
                return;
            }

            indicator = Object.Instantiate(radiusPrefab, transform.position, Quaternion.identity, transform);

            foreach (var renderer in indicator.GetComponentsInChildren<Renderer>())
            {
                renderer.material.SetColor("_TintColor", new Color(0.6f, 0.1f, 0.9f, 0.8f));
                renderer.material.SetColor("_EmColor", new Color(0.6f, 0.1f, 0.9f, 0.8f));
            }

            foreach (Transform child in indicator.transform)
            {
                child.localScale = new Vector3(Radius * 2, Radius * 2, Radius * 2);
            }

            indicator.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(base.gameObject, null);
        }
    }

}