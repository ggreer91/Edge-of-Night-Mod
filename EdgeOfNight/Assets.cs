using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EdgeOfNightMod
{
    internal static class Assets
    {
        internal static GameObject EdgeOfNightPrefab;
        internal static Sprite EdgeOfNightIcon;
        internal static ManualLogSource Logger;
        private const string ModPrefix = "@EdgeOfNightMod:";

        internal static ItemDef EdgeOfNightItemDef;

        internal static void Init(ManualLogSource Logger)
        {
            Assets.Logger = Logger;

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EdgeOfNight.eonassetbundle"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);

                EdgeOfNightPrefab = bundle.LoadAsset<GameObject>("Assets/AssetsBundlesWanted/Edge_of_Night.prefab");
                EdgeOfNightIcon = bundle.LoadAsset<Sprite>("Assets/AssetsBundlesWanted/Edge_of_Night.png");
            }

            CreateEdgeOfNightItem();
            AddLanguageTokens();
        }

        private static void CreateEdgeOfNightItem()
        {
            EdgeOfNightItemDef = ScriptableObject.CreateInstance<ItemDef>();
            EdgeOfNightItemDef.name = "EdgeOfNight";
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            EdgeOfNightItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            EdgeOfNightItemDef.pickupModelPrefab = EdgeOfNightPrefab;
            EdgeOfNightItemDef.pickupIconSprite = EdgeOfNightIcon;
            EdgeOfNightItemDef.nameToken = "EDGEOFNIGHT_NAME";
            EdgeOfNightItemDef.pickupToken = "EDGEOFNIGHT_PICKUP";
            EdgeOfNightItemDef.descriptionToken = "EDGEOFNIGHT_DESC";
            EdgeOfNightItemDef.loreToken = "EDGEOFNIGHT_LORE";
            EdgeOfNightItemDef.tags = [ ItemTag.Utility, ItemTag.Damage ];
            EdgeOfNightItemDef.canRemove = true;
            EdgeOfNightItemDef.hidden = false;

            var itemDisplayRules = new ItemDisplayRuleDict(null); // make null if we don't want the item on the survivor model
            //var itemDisplayRules = new ItemDisplayRule[1]; // allows item to show on survivor
            //itemDisplayRules[0].followerPrefab = EdgeOfNightPrefab; // the prefab that shows up on the survivor
            //itemDisplayRules[0].childName = "Chest"; // defines starting point of the position of the model, you can see what names are available in the prefab model of the survivors
            //itemDisplayRules[0].localScale = new Vector3(0.15f, 0.15f, 0.15f); // scale the model
            //itemDisplayRules[0].localAngles = new Vector3(0f, 0f, 0f); // rotate the model
            //itemDisplayRules[0].localPos = new Vector3(-0.2f, 1f, -0f); // position offset relative to the childName

            var EdgeOfNight = new R2API.CustomItem(EdgeOfNightItemDef, itemDisplayRules);

            if (!ItemAPI.Add(EdgeOfNight))
            {
                EdgeOfNightItemDef = null;
                Logger.LogError("Unable to add Edge of Night item");
            }
        }

        private static void AddLanguageTokens()
        {
            // Styles
            // <style=cIsHealth>" + exampleValue + "</style>
            // <style=cIsDamage>" + exampleValue + "</style>
            // <style=cIsHealing>" + exampleValue + "</style>
            // <style=cIsUtility>" + exampleValue + "</style>
            // <style=cIsVoid>" + exampleValue + "</style>
            // <style=cHumanObjective>" + exampleValue + "</style>
            // <style=cLunarObjective>" + exampleValue + "</style>
            // <style=cStack>" + exampleValue + "</style>
            // <style=cWorldEvent>" + exampleValue + "</style>
            // <style=cArtifact>" + exampleValue + "</style>
            // <style=cUserSetting>" + exampleValue + "</style>
            // <style=cDeath>" + exampleValue + "</style>
            // <style=cSub>" + exampleValue + "</style>
            // <style=cMono>" + exampleValue + "</style>
            // <style=cShrine>" + exampleValue + "</style>
            // <style=cEvent>" + exampleValue + "</style>

            LanguageAPI.Add("EDGEOFNIGHT_NAME", "Edge of Night");
            LanguageAPI.Add("EDGEOFNIGHT_PICKUP", "When attacked by an Elite enemy, temporarily gain that Elite's power.");
            LanguageAPI.Add("EDGEOFNIGHT_DESC", $"Upon being attacked by an Elite enemy, gain that Elite's <style=cIsDamage>power</style> for <style=cIsUtility>{EdgeOfNight.GetTotalBuffTime(1)}s</style> <style=cStack>(+{EdgeOfNight.buffStackBonus}s per stack)</style>. Recharges every <style=cIsUtility>{EdgeOfNight.cooldownDuration}</style> seconds.");
            LanguageAPI.Add("EDGEOFNIGHT_LORE", "It's just a cool-looking cape..?");
        }
    }
}