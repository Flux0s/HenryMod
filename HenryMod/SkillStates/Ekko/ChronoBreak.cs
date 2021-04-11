using EntityStates;
using RoR2;
using R2API;
using UnityEngine;
using System.Collections.Generic;

namespace HenryMod.SkillStates.Ekko
{
    public class ChronoBreak : BaseSkillState
    {
        public static float baseDashSpeed = 30f;
        public static float stopThreshold = 3f;
        public static float rewindLength = 3f;
        public static float blastRadius = 30f;
        public static float blastDamageCoefficient = 25f;
        public static float blastProcCoefficient = 1f;
        public static float blastForce = 100f;
        public static DamageType blastDamageType = DamageType.Generic;

        private Animator animator;
        private List<DamageTrail.TrailPoint> storedPoints;
        private GameObject explosionEffectPrefab;
        private int currentPoint;
        private float baseHealModifier = .2f;
        private float additiveHealModifier = 3f;
        private float localTimeStore;

        public override void OnEnter()
        {
            base.OnEnter();

            //if (NetworkServer.active)
            //{
            //    base.characterBody.AddTimedBuff(Modules.Buffs.armorBuff, 3f * ChronoBreak.duration);
            //    base.characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 0.5f * ChronoBreak.duration);
            //}
            DamageTrail ChronoDamageTrail = base.GetComponent<DamageTrail>();
            if (ChronoDamageTrail.pointsList.Count > 0) storedPoints = ChronoDamageTrail.pointsList;
            else storedPoints = new List<DamageTrail.TrailPoint>();
            currentPoint = storedPoints.Count - 2;

            ChronoDamageTrail.active = false;
            base.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
            base.characterBody.AddBuff(RoR2Content.Buffs.Cloak);

            localTimeStore = ChronoDamageTrail.localTime;
            ChronoDamageTrail.localTime = 0f;
            if (base.characterBody) base.characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
        }



        private bool Reel(Vector3 position1, Vector3 position2)
        {
            Vector3 vector = position1 - position2;
            return vector.magnitude <= ChronoBreak.stopThreshold;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.localTimeStore += Time.fixedDeltaTime;
            if (storedPoints.Count == 1)
            {
                Modules.Components.DamageHistory damageHistory = base.GetComponent<Modules.Components.DamageHistory>();
                float recentDamage = damageHistory.GetTotalDamage();
                base.healthComponent.Heal(CalculateChronoHeal(recentDamage), new ProcChainMask());
                Debug.LogWarning("Ekko healed this: " + CalculateChronoHeal(recentDamage));
                damageHistory.ClearDamageList();
                this.outer.SetNextStateToMain();
                return;
            }

            if (base.isAuthority)
            {
                float velocityModifier = (base.transform.position - storedPoints[storedPoints.Count - 2].position).magnitude;
                Vector3 velocity = (storedPoints[storedPoints.Count - 2].position - base.transform.position).normalized * ChronoBreak.baseDashSpeed * velocityModifier;
                base.characterMotor.velocity = velocity;
                base.characterDirection.forward = base.characterMotor.velocity.normalized;

                // don't get locked in a stinger forever (idk what would cause this but it is entirely possible)
                if (base.fixedAge >= 10f)
                {
                    this.outer.SetNextStateToMain();
                    return;
                }

                //this.attack.forceVector = base.characterMotor.velocity.normalized * PhaseDiveLunge.pushForce;

                if (Reel(base.transform.position, storedPoints[storedPoints.Count - 2].position))
                {

                    if (storedPoints.Count == 1)
                    {
                        return;
                    }
                    Debug.LogWarning("Points left in the line: " + storedPoints.Count);
                    storedPoints.RemoveAt(storedPoints.Count - 2);
                    return;
                }
            }
            else
            {
                base.skillLocator.special.AddOneStock();
                this.outer.SetNextStateToMain();
                return;
            }

        }

        public override void OnExit()
        {
            base.OnExit();

            base.GetComponent<DamageTrail>().localTime = localTimeStore;
            this.explosionEffectPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("prefabs/effects/ShopkeeperKickFromShopExplosion"), "ChronoBreakPrefab");
            ExplodeServer();

            base.characterMotor.disableAirControlUntilCollision = false;
            base.characterMotor.velocity = new Vector3(0f, 0f, 0f);
            base.characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;

            base.GetComponent<DamageTrail>().active = true;
            base.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
            base.characterBody.RemoveBuff(RoR2Content.Buffs.Cloak);

            base.characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
        }

        //public override void OnSerialize(NetworkWriter writer)
        //{
        //    base.OnSerialize(writer);
        //    writer.Write(this.forwardDirection);
        //}

        //public override void OnDeserialize(NetworkReader reader)
        //{
        //    base.OnDeserialize(reader);
        //    this.forwardDirection = reader.ReadVector3();
        //}

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Frozen;
        }

        private float CalculateChronoHeal(float damageTaken)
        {
            float baseHeal = damageTaken * baseHealModifier;
            CharacterBody charBody = base.GetComponent<CharacterBody>();
            float damagePercentOfMax = (damageTaken / charBody.maxHealth) * additiveHealModifier;
            float additiveHeal = damagePercentOfMax * baseHeal;
            return baseHeal + additiveHeal;
        }

        private void ExplodeServer()
        {
            EffectData effectData = new EffectData
            {
                origin = base.transform.position,
                scale = blastRadius
            };
            EffectManager.SpawnEffect(this.explosionEffectPrefab, effectData, true);
            new BlastAttack
            {
                attacker = base.characterBody.gameObject,
                baseDamage = blastDamageCoefficient * base.characterBody.damage,
                baseForce = blastForce,
                // bonusForce = blastBonusForce,
                attackerFiltering = AttackerFiltering.NeverHit,
                crit = base.characterBody.RollCrit(),
                damageColorIndex = DamageColorIndex.Item,
                damageType = blastDamageType,
                falloffModel = BlastAttack.FalloffModel.Linear,
                inflictor = base.gameObject,
                position = base.transform.position,
                procChainMask = default(ProcChainMask),
                procCoefficient = blastProcCoefficient,
                radius = blastRadius,
                teamIndex = base.characterBody.teamComponent.teamIndex
            }.Fire();
            // Util.PlaySound(this.explosionSoundString, base.gameObject);
        }
    }
}