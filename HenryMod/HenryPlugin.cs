using BepInEx;
using R2API.Utils;
using RoR2;
using System.Security;
using System.Security.Permissions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace HenryMod
{
    [BepInDependency("com.TeamMoonstorm.Starstorm2", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.DestroyedClone.AncientScepter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(MODUID, MODNAME, MODVERSION)]
    [R2APISubmoduleDependency(new string[]
    {
        "PrefabAPI",
        "LanguageAPI",
        "SoundAPI",
    })]

    public class HenryPlugin : BaseUnityPlugin
    {
        // if you don't change these you're giving permission to deprecate the mod-
        //  please change the names to your own stuff, thanks
        //   this shouldn't even have to be said
        public const string MODUID = "com.rob.HenryMod";
        public const string MODNAME = "HenryMod";
        public const string MODVERSION = "1.2.4";


        // a prefix for name tokens to prevent conflicts
        public const string developerPrefix = "ROB";

        // soft dependency stuff
        public static bool starstormInstalled = false;
        public static bool scepterInstalled = false;

        public static HenryPlugin instance;

        private void Awake()
        {
            instance = this;

            // check for soft dependencies
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.TeamMoonstorm.Starstorm2")) starstormInstalled = true;
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.DestroyedClone.AncientScepter")) scepterInstalled = true;

            // load assets and read config
            Modules.Assets.Initialize();
            Modules.Config.ReadConfig();
            Modules.States.RegisterStates(); // register states for networking
            Modules.Buffs.RegisterBuffs(); // add and register custom buffs/debuffs
            Modules.Projectiles.RegisterProjectiles(); // add and register custom projectiles
            Modules.Tokens.AddTokens(); // register name tokens
            Modules.ItemDisplays.PopulateDisplays(); // collect item display prefabs for use in our display rules

            new Modules.Survivors.Henry().Initialize();
            new Modules.Survivors.Ekko().Initialize();
            //new Modules.Survivors.SimpleCharacter().Initialize();
            //new Modules.Enemies.MrGreen().CreateCharacter();

            // nemry leak? if you're reading this keep quiet about it please.
            // use it as an example for your own nemesis if you want i suppose
            //new Modules.Enemies.Nemry().CreateCharacter();

            new Modules.ContentPacks().Initialize();

            RoR2.ContentManagement.ContentManager.onContentPacksAssigned += LateSetup;

            Hook();
        }

        private void LateSetup(HG.ReadOnlyArray<RoR2.ContentManagement.ReadOnlyContentPack> obj)
        {
            Modules.Survivors.Henry.instance.SetItemDisplays();
        }

        private void Hook()
        {
            // run hooks here, disabling one is as simple as commenting out the line
            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;

            On.RoR2.SurvivorPodController.OnPassengerEnter += (orig, self, passenger) =>
            {
                orig(self, passenger);
                if (passenger.GetComponent<CharacterBody>().name == Modules.Survivors.Ekko.EkkoName)
                {
                    LineRenderer lineRenderer = passenger.GetComponent<LineRenderer>();
                    lineRenderer.widthMultiplier = 0f;
                }
            };

            On.RoR2.SurvivorPodController.OnPassengerExit += (orig, self, passenger) =>
            {
                orig(self, passenger);
                if (passenger.GetComponent<CharacterBody>().name == Modules.Survivors.Ekko.EkkoName)
                {
                    LineRenderer lineRenderer = passenger.GetComponent<LineRenderer>();
                    lineRenderer.widthMultiplier = 1f;
                }
            };

            On.RoR2.CharacterBody.OnTakeDamageServer += (orig, self, damageReport) =>
            {
                if (damageReport.victim.name == Modules.Survivors.Ekko.EkkoName)
                {
                    Modules.Components.DamageHistory damageHistory = damageReport.victim.GetComponent<Modules.Components.DamageHistory>();
                    // Debug.LogWarning("Ekko took " + damageReport.damageDealt + " damage");
                    damageHistory.addDamage(damageReport.damageDealt);
                    damageHistory.PruneDamageList();
                    // Debug.LogWarning("The total damage is " + damageHistory.GetTotalDamage());
                }
                orig(self, damageReport);
            };

            //Does some crazy stuff to only apply debuff when ekko hits an enemy with HIS attacks (not proc attack i.e. Ukulele)
            On.RoR2.OverlapAttack.PerformDamage += (orig, attacker, inflictor, damage, isCrit,
                procChainMask, procCoefficient, damageColorIndex, damageType, forceVector, pushAwayForce, hitList) =>
            {
                bool resetDamageType = false;
                List<OverlapAttack.OverlapInfo> hitListCast = (List<OverlapAttack.OverlapInfo>)hitList;
                if (attacker.name == Modules.Survivors.Ekko.EkkoName)
                {
                    if ((damageType & DamageType.BlightOnHit) == DamageType.BlightOnHit)
                    {
                        resetDamageType = true;
                        hitListCast.RemoveAll(overlapInfo =>
                        {
                            HealthComponent healthComponent = overlapInfo.hurtBox.healthComponent;
                            if (healthComponent)
                            {
                                var victimCharacterBody = healthComponent.gameObject.GetComponent<CharacterBody>();
                                victimCharacterBody.AddTimedBuff(Modules.Buffs.zDriveDebuff, HenryMod.Modules.Buffs.ZDriveduration, Modules.Buffs.ZDriveMaxStacks);
                                if (victimCharacterBody.GetBuffCount(Modules.Buffs.zDriveDebuff) == Modules.Buffs.ZDriveMaxStacks)
                                {
                                    victimCharacterBody.ClearTimedBuffs(Modules.Buffs.zDriveDebuff);
                                    float zDriveDamage = (damage * Modules.Buffs.ZDriveDamageBonus);
                                    DamageInfo damageInfo = new DamageInfo();
                                    damageInfo.attacker = attacker;
                                    damageInfo.inflictor = inflictor;
                                    damageInfo.force = forceVector + pushAwayForce * overlapInfo.pushDirection;
                                    damageInfo.damage = zDriveDamage;
                                    damageInfo.crit = isCrit;
                                    damageInfo.position = overlapInfo.hitPosition;
                                    damageInfo.procChainMask = procChainMask;
                                    damageInfo.procCoefficient = procCoefficient;
                                    damageInfo.damageColorIndex = damageColorIndex;
                                    damageInfo.damageType = damageType & ~DamageType.BlightOnHit;
                                    damageInfo.ModifyDamageInfo(overlapInfo.hurtBox.damageModifier);
                                    healthComponent.TakeDamage(damageInfo);
                                    GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
                                    GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
                                    return true;
                                }
                            }
                            return false;
                        });
                    }
                }
                if (resetDamageType)
                {
                    damageType = damageType & ~DamageType.BlightOnHit;
                }
                orig(attacker, inflictor, damage, isCrit, procChainMask, procCoefficient, damageColorIndex,
                damageType, forceVector, pushAwayForce, hitList);
            };
        }

        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);

            // a simple stat hook, adds armor after stats are recalculated
            if (self)
            {
                if (self.HasBuff(Modules.Buffs.armorBuff))
                {
                    self.armor += 300f;
                }
            }
        }
    }
}