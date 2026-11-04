using System.Collections.Generic;
using RoR2;
using ItemQualities;
using BepInEx;
using BepInEx.Logging;
using System.IO;
using R2API.Utils;

namespace NautilusQuality
{
    [BepInPlugin("paris.NautilusQuality", "NautilusQuality", "0.1.0")]
    [BepInDependency("com.Gorakh.ItemQualities")]
    [BepInDependency("com.Hex3.Nautilus")]

    public class NautilusQuality : BaseUnityPlugin
    {

        // nautilus item name (e.g. "Crabsinthe") -> its quality group
        public static Dictionary<string, ItemQualityGroup> Groups = [];

        internal static ManualLogSource Log;

        void Awake()
        {
            Log = Logger;
            QualityCompat.Init();
            LoadLanguageFiles();

            CrabsintheQuality.Init();
        }

    private void LoadLanguageFiles()
    {
        string languageFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "lang");
        if (Directory.Exists(languageFolder))
        {
            Language.collectLanguageRootFolders += (folders) => folders.Add(languageFolder);
        }
    }

    }

}

