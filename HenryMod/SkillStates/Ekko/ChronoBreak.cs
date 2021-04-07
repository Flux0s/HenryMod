using EntityStates;
using RoR2;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace HenryMod.SkillStates.Ekko
{
    public class ChronoBreak : BaseSkillState
    {
        public static float dashSpeed = 50f;
        public static float stopThreshold = 1f;

        public static string dodgeSoundString = "HenryRoll";
        public static float dodgeFOV = EntityStates.Commando.DodgeState.dodgeFOV;

        private Animator animator;
        private  List<DamageTrail.TrailPoint> storedPoints;

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
        }
       
        

        private bool Reel(Vector3 position1, Vector3 position2)
        {
            Vector3 vector = position1 - position2;
            return vector.magnitude <= ChronoBreak.stopThreshold;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();


            if (storedPoints.Count == 0)
            {
                this.outer.SetNextStateToMain();
                return;
            }

            if (base.isAuthority)
            {
                Vector3 velocity = (storedPoints[storedPoints.Count - 1].position - base.transform.position).normalized * ChronoBreak.dashSpeed;
                base.characterMotor.velocity = velocity;
                base.characterDirection.forward = base.characterMotor.velocity.normalized;

                // don't get locked in a stinger forever (idk what would cause this but it is entirely possible)
                if (base.fixedAge >= 20f)
                {
                    this.outer.SetNextStateToMain();
                    return;
                }

                //this.attack.forceVector = base.characterMotor.velocity.normalized * PhaseDiveLunge.pushForce;

                if (Reel(base.transform.position, storedPoints[storedPoints.Count - 1].position))
                {
                    storedPoints.RemoveAt(storedPoints.Count - 1);
                    return;
                }
            }
            else
            {
                base.skillLocator.secondary.AddOneStock();
                this.outer.SetNextStateToMain();
                return;
            }

        }

        public override void OnExit()
        {
            base.OnExit();

            base.characterMotor.disableAirControlUntilCollision = false;

            base.characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            base.characterMotor.velocity = new Vector3(0f, 0f, 0f);
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
    }
}