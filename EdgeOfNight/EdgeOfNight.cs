using System;
using System.Linq;
using System.Collections.Generic;

using BepInEx;
using BepInEx.Logging;
using R2API;
using R2API.Utils;

using RoR2;
using UnityEngine;
using Mono.Security.Authenticode;


namespace EdgeOfNightMod
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI))]
    [BepInPlugin(PluginGUID, ModName, ModVer)]
    public class CustomItem : BaseUnityPlugin
    {
        private const string ModVer = "1.1";
        public const string ModAuthor = "George";
        public const string ModName = "EdgeofNightMod";
        public const string PluginGUID = $"{ModAuthor}.{ModName}";

        public static BepInEx.Logging.ManualLogSource Log;

        private static float buffDuration = 3f;

        public void Awake()
        {
            Log = Logger;
            Assets.Init(Log);
            On.RoR2.CharacterBody.OnTakeDamageServer += (orig, self, damageReport) => { VerifyBody(orig, self, damageReport); };
        }
        public static void VerifyBody(On.RoR2.CharacterBody.orig_OnTakeDamageServer orig, CharacterBody self, DamageReport damageReport)
        {
            if (!self.Equals(null) && self.isPlayerControlled)
            {
                int edgeOfNight_count = self.inventory.GetItemCount(Assets.EdgeOfNightItemDef);
                if (edgeOfNight_count > 0)
                {
                    DoEdgeOfNight(edgeOfNight_count, self, damageReport);
                }
            }

            orig(self, damageReport);
        }

        public static void DoEdgeOfNight(int edgeOfNight_count, CharacterBody self, DamageReport damageReport)
        {
            /*
             * Wake of Vultures code for reference:
             * 
             *  int itemCount9 = inventory.GetItemCount(RoR2Content.Items.HeadHunter);
				int itemCount10 = inventory.GetItemCount(RoR2Content.Items.KillEliteFrenzy);
				if (itemCount9 > 0)
				{
					float duration = 3f + 5f * (float)itemCount9;
					for (int k = 0; k < BuffCatalog.eliteBuffIndices.Length; k++)
					{
						BuffIndex buffIndex = BuffCatalog.eliteBuffIndices[k];
						if (victimBody.HasBuff(buffIndex))
						{
							attackerBody.AddTimedBuff(buffIndex, duration);
						}
					}
				}
            */

            Log.LogInfo($"Damage report: {damageReport}");
            for (int i = 0; i < BuffCatalog.eliteBuffIndices.Length; i++)
            {
                BuffIndex buffIndex = BuffCatalog.eliteBuffIndices[i];
                if (damageReport.attackerBody.HasBuff(buffIndex))
                {
                    damageReport.victimBody.AddTimedBuff(buffIndex, buffDuration);
                }
            }
        }

        // Runs on every frame
        private void Update()
        {
            // Checking if player presses F2
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Gets player body
                GameObject playerGameObject = PlayerCharacterMasterController.instances[0].master.GetBodyObject();
                CharacterBody body = playerGameObject.GetComponent<CharacterBody>();
               Log.LogInfo("Player pressed F2");
               if (!body)
               {
                   Log.LogInfo("Component body doesn't exist");
                   return;
               }
               if (Util.CheckRoll(50, body.master)) // 50/50 chance
               {
                   // if positive roll:
                   Transform playerTransform = playerGameObject.transform;
                   Log.LogInfo($"Spawning our custom item at coordinates {playerTransform.position}");
                   PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(Assets.EdgeOfNightItemDef.itemIndex), playerTransform.position, playerTransform.forward * 20f);
               }
               else
               {
                   // if negative roll:
                   DamageInfo info = new DamageInfo();
                   info.damage = (body.maxHealth / 10);
                   info.crit = false;
                   info.procCoefficient = 0f;
                   body.healthComponent.TakeDamage(info);
                   Log.LogInfo($"Player rolled negatively - took {info.damage} damage");
               }
            }
        }
    }
}
