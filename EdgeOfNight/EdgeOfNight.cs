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
using System.IO;

namespace EdgeOfNightMod
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(PluginGUID, ModName, ModVer)]
    public class EdgeOfNight : BaseUnityPlugin
    {
        public const string ModVer = "0.1.3";
        public const string ModAuthor = "George";
        public const string ModName = "EdgeofNightMod";
        public const string PluginGUID = $"{ModAuthor}.{ModName}";
        public static BepInEx.Logging.ManualLogSource Log;

        // used vars
        public static BuffDef activeBuff;
        public static BuffDef cooldownBuff;
        public static float buffDuration = 1f;
        public static float buffStackBonus = 2f;
        public static float cooldownDuration = 8f;
        public static uint procSoundEventID = 4094061087;
        public static uint offCooldownSoundEventID = 3231506196;

        public void Awake()
        {
            Log = Logger;
            Assets.Init(Log);
            CreateBuffs(); // Creating Edge of Night buff
            HooksContainer(); // Preps event handlers
        }

        // creates both the active buff and cooldown buff
        private static void CreateBuffs()
        {
            activeBuff = ScriptableObject.CreateInstance<BuffDef>();
            activeBuff.iconSprite = Assets.EdgeOfNightIcon;
            activeBuff.name = "EDGEOFNIGHT_BUFF";
            activeBuff.canStack = false;
            activeBuff.isDebuff = false;
            activeBuff.isCooldown = false;
            activeBuff.isHidden = false;
            activeBuff.buffColor = Color.white;
            ContentAddition.AddBuffDef(activeBuff);

            cooldownBuff = ScriptableObject.CreateInstance<BuffDef>();
            cooldownBuff.iconSprite = Assets.EdgeOfNightIcon;
            cooldownBuff.name = "EDGEOFNIGHT_COOLDOWN";
            cooldownBuff.canStack = true;
            cooldownBuff.isDebuff = false;
            cooldownBuff.isCooldown = true;
            cooldownBuff.isHidden = false;
            cooldownBuff.buffColor = Color.gray;
            ContentAddition.AddBuffDef(cooldownBuff);
        }

        // holds all event handlers for the item
        private static void HooksContainer()
        {
            On.RoR2.CharacterBody.OnTakeDamageServer += (orig, self, damageReport) =>
            {
                VerifyBody(self, damageReport);
                orig(self, damageReport);
            };

            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                UpdateBuff(self);
                orig(self);
            };

            On.RoR2.UI.ItemIcon.SetItemIndex += (orig, self, newIndex, newCount) =>
            {
                orig(self, newIndex, newCount);
                if (self.tooltipProvider != null && newIndex == Assets.EdgeOfNightItemDef.itemIndex)
                {
                    self.tooltipProvider.overrideBodyText = GetDisplayInformation();
                }
            };
        }

        // makes sure the player themself is the one who was damaged
        public static void VerifyBody(CharacterBody self, DamageReport damageReport)
        {
            if (damageReport.attackerBody == null) // if attackerBody doesn't exist, the damage is environmental/health cost
                return;
            if (self && self.isPlayerControlled)
            {
                int edgeOfNightCount = self.inventory.GetItemCount(Assets.EdgeOfNightItemDef);
                if (edgeOfNightCount > 0)
                {
                    ActivateEffect(edgeOfNightCount, self, damageReport);
                }
            }
        }

        // gets times for the item effect and item description (possibly temporary)
        public static int GetTotalBuffTime(int count)
        {
            return Convert.ToInt32(buffDuration + buffStackBonus * count);
        }

        // gives elite effect, removes active buff, runs cooldown stacking function to begin a "cooldown"
        private static void ActivateEffect(int edgeOfNight_count, CharacterBody self, DamageReport damageReport)
        {
            if (!self.HasBuff(activeBuff))
                return;
            for (int i = 0; i < BuffCatalog.eliteBuffIndices.Length; i++)
            {
                BuffIndex buffIndex = BuffCatalog.eliteBuffIndices[i];
                if (damageReport.attackerBody.HasBuff(buffIndex))
                {
                    self.AddTimedBuff(buffIndex, GetTotalBuffTime(edgeOfNight_count));
                    self.RemoveBuff(activeBuff);
                    AddCooldownStacks(self, cooldownBuff, cooldownDuration);
                    AkSoundEngine.PostEvent(procSoundEventID, self.gameObject); // plays triggered sound effect
                }
            }
        }

        // adds a "cooldown" in the form of stacks
        public static void AddCooldownStacks(CharacterBody self, BuffDef cooldown, float duration)
        {
            float myTimer = 1;
            while (myTimer <= duration)
            {
                self.AddTimedBuff(cooldown, myTimer);
                myTimer++;
            }
        }

        // gives active buff on-pickup, gives active buff whenever cooldownBuff is gone
        private static void UpdateBuff(CharacterBody self)
        {
            if (!self || !self.inventory)
                return;
            if (self.inventory.GetItemCount(Assets.EdgeOfNightItemDef.itemIndex) <= 0)
                return;
            if (self.HasBuff(activeBuff))
                return;
            if (!self.HasBuff(cooldownBuff))
            {
                self.AddBuff(activeBuff);
                AkSoundEngine.PostEvent(offCooldownSoundEventID, self.gameObject); // adds off-cooldown sound effect
            }

        }

        // retrieves item description for scoreboard (possibly temporary)
        private static string GetDisplayInformation()
        {
            return Language.GetString(Assets.EdgeOfNightItemDef.descriptionToken);
        }

        // runs on every frame (temporary)
        //private void Update()
        //{
        //    // Checking if player presses F2
        //    if (Input.GetKeyDown(KeyCode.F2))
        //    {
        //        // Gets player body
        //        GameObject playerGameObject = PlayerCharacterMasterController.instances[0].master.GetBodyObject();
        //        CharacterBody body = playerGameObject.GetComponent<CharacterBody>();
        //       if (!body)
        //       {
        //           return;
        //       }
        //       if (Util.CheckRoll(50, body.master)) // 50/50 chance
        //       {
        //           // if positive roll:
        //           Transform playerTransform = playerGameObject.transform;
        //           PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(Assets.EdgeOfNightItemDef.itemIndex), playerTransform.position, playerTransform.forward * 20f);
        //       }
        //       else
        //       {
        //           // if negative roll:
        //           DamageInfo info = new DamageInfo();
        //           info.damage = (body.maxHealth / 10);
        //           info.crit = false;
        //           info.procCoefficient = 0f;
        //           body.healthComponent.TakeDamage(info);
        //       }
        //    }
        //}
    }
}
