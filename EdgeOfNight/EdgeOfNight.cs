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

        private static System.Random random = new System.Random();
        private static bool debounce = false;
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
                    DoEdgeOfNight(edgeOfNight_count, self);
                }
            }

            orig(self, damageReport);
        }

        public static void DoEdgeOfNight(int edgeOfNight_count, CharacterBody self)
        {
            if (self.healthComponent.isHealthLow)
            {
                // we've already executed - skip...
                if (debounce)
                {
                    return;
                }

                debounce = true;
                for (int i = 0; i <= edgeOfNight_count; i++)
                {
                    var monsters = TeamComponent.GetTeamMembers(TeamIndex.Monster);
                    if (monsters.Count == 0) // nothing to charm
                    {
                        return;
                    }

                    // copy our monsters list into a randomly sorted order
                    // it's O(n), but it's the best we can really do here...
                    var monsters_copy = new List<TeamComponent>(monsters).OrderBy(x => random.Next()).ToList();

                    // grab our random monster that's not a boss
                    TeamComponent monster = null;
                    foreach (var m in monsters_copy)
                    {
                        if (!m.body.master.isBoss && BossGroup.FindBossGroup(m.body) is null)
                        {
                            Log.Log(LogLevel.Debug, "Selected monster: " + m.body.master.name);
                            monster = m;
                            break;
                        }
                    }

                    // We were unable to find non-boss a monster to charm, lets just exit
                    // and try again later.
                    if (monster is null)
                    {
                        Log.Log(LogLevel.Warning, "Unable to find a suitable mob to Edge of Night (change this log lol).");
                        return;
                    }

                    // Assign to player team
                    monster.body.master.teamIndex = TeamIndex.Player;
                    monster.body.teamComponent.teamIndex = TeamIndex.Player;

                    // Reset aggro
                    var baseAi = monster.body.master.GetComponent<RoR2.CharacterAI.BaseAI>();
                    baseAi.currentEnemy.Reset();
                    baseAi.ForceAcquireNearestEnemyIfNoCurrentEnemy();
                }

                // reset drone aggro if needed
                var players = TeamComponent.GetTeamMembers(TeamIndex.Player);
                foreach (var player in players)
                {
                    if (!player.body.isPlayerControlled)
                    {
                        var ai = player.body.masterObject.GetComponent<RoR2.CharacterAI.BaseAI>();
                        if (ai.currentEnemy.characterBody.teamComponent.teamIndex == TeamIndex.Player)
                        {
                            ai.currentEnemy.Reset();
                            ai.ForceAcquireNearestEnemyIfNoCurrentEnemy();
                        }
                    }
                }
            }
            else
            {
                debounce = false;
            }
        }
        private void Update()
        {
        //    This if statement checks if the player has currently pressed F2.
           if (Input.GetKeyDown(KeyCode.F2))
           {
               Log.LogInfo("Player pressed F2");
            //    Get the player body
               GameObject playerGameObject = PlayerCharacterMasterController.instances[0].master.GetBodyObject();
               CharacterBody body = playerGameObject.GetComponent<CharacterBody>();
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
