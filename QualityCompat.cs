using System.Runtime.CompilerServices;
using System.Collections;
using System.Collections.Generic;
using R2API;
using RoR2;
using ItemQualities;
using ItemQualities.ContentManagement;
using ItemQualities.Utilities.Extensions;
using Nautilus;
using Nautilus.Items;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using UnityEngine.Networking;

namespace NautilusQuality
{
    internal static class QualityCompat
    {

        public static Dictionary<string, ItemQualityGroup> Groups = [];

        public static void Init()
        {
            QualityContentManager.LoadContentAsync += OnLoadContent;
        }

        public static ItemQualityGroup GetGroup(string itemName)
        {
            if (!Groups.ContainsKey(itemName))
            {
                return null;
            }

            return Groups[itemName];
        }

        static IEnumerator OnLoadContent(QualityContentLoadArgs args)
        {
            for (var i = 0; i < ItemInit.ItemList.Count; i++)
            {
                var item = ItemInit.ItemList[i];

                //skip empty helper items n such
                if (item.ItemDef == null || item.Hidden || item.IsConsumed)
                {
                    continue;
                }

                var group = args.CreateItemQualityGroup(item.ItemDef);
                Groups[item.Name] = group;
                AddPlaceholderTokens(item.Name);

                NautilusQuality.Log.LogInfo("created quality group for " + item.Name);
            }

            yield break;
        }

        static void AddPlaceholderTokens(string baseName)
        {
            var upper = baseName.ToUpper();
            var tiers = new[] { "UNCOMMON", "RARE", "EPIC", "LEGENDARY" };

            for (var t = 0; t < tiers.Length; t++)
            {
                var prefix = "ITEM_" + upper + "_" + tiers[t].ToUpper();
                LanguageAPI.Add(prefix + "_NAME", baseName + " (" + tiers[t] + ")");
            }
        }

    }
}