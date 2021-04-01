using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HenryMod.SkillStates.Ekko.PhaseDive
{
    public class PhaseDiveRoll : BaseSkillState
    {
        public static float duration = 0.4f;
        public static float initialSpeedCoefficient = 4f;
        public static float finalSpeedCoefficient = 1f;

        public static string dodgeSoundString = "HenryRoll";
        public static float dodgeFOV = EntityStates.Commando.DodgeState.dodgeFOV;

        private float rollSpeed;
        private Vector3 forwardDirection;
        private Animator animator;
        private Vector3 previousPosition;

        public override void OnEnter()
        {
            base.OnEnter();
            this.animator = base.GetModelAnimator();

            if (base.isAuthority && base.inputBank && base.characterDirection)
            {
                this.forwardDirection = ((base.inputBank.moveVector == Vector3.zero) ? base.characterDirection.forward : base.inputBank.moveVector).normalized;
            }

            Vector3 rhs = base.characterDirection ? base.characterDirection.forward : this.forwardDirection;
            Vector3 rhs2 = Vector3.Cross(Vector3.up, rhs);

            float num = Vector3.Dot(this.forwardDirection, rhs);
            float num2 = Vector3.Dot(this.forwardDirection, rhs2);

            this.RecalculateRollSpeed();

            if (base.characterMotor && base.characterDirection)
            {
                base.characterMotor.velocity.y = 0f;
                base.characterMotor.velocity = this.forwardDirection * this.rollSpeed;
            }

            Vector3 b = base.characterMotor ? base.characterMotor.velocity : Vector3.zero;
            this.previousPosition = base.transform.position - b;

            base.PlayAnimation("FullBody, Override", "Roll", "Roll.playbackRate", PhaseDiveRoll.duration);
            Util.PlaySound(PhaseDiveRoll.dodgeSoundString, base.gameObject);

            if (NetworkServer.active)
            {
                base.characterBody.AddTimedBuff(Modules.Buffs.armorBuff, 3f * PhaseDiveRoll.duration);
                base.characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 0.5f * PhaseDiveRoll.duration);
            }
        }

        private void RecalculateRollSpeed()
        {
            this.rollSpeed = this.moveSpeedStat * Mathf.Lerp(PhaseDiveRoll.initialSpeedCoefficient, PhaseDiveRoll.finalSpeedCoefficient, base.fixedAge / PhaseDiveRoll.duration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.RecalculateRollSpeed();

            if (base.characterDirection) base.characterDirection.forward = this.forwardDirection;
            if (base.cameraTargetParams) base.cameraTargetParams.fovOverride = Mathf.Lerp(PhaseDiveRoll.dodgeFOV, 60f, base.fixedAge / PhaseDiveRoll.duration);

            Vector3 normalized = (base.transform.position - this.previousPosition).normalized;
            if (base.characterMotor && base.characterDirection && normalized != Vector3.zero)
            {
                Vector3 vector = normalized * this.rollSpeed;
                float d = Mathf.Max(Vector3.Dot(vector, this.forwardDirection), 0f);
                vector = this.forwardDirection * d;
                vector.y = 0f;

                base.characterMotor.velocity = vector;
            }
            this.previousPosition = base.transform.position;

            if (base.isAuthority && base.fixedAge >= PhaseDiveRoll.duration)
            {
                // this.outer.SetNextState(new PhaseDiveRollExit());
                this.outer.SetNextState(new PhaseDiveRollExit());
                return;
            }
        }

        public override void OnExit()
        {
            if (base.cameraTargetParams) base.cameraTargetParams.fovOverride = -1f;
            base.OnExit();

            base.characterMotor.disableAirControlUntilCollision = false;
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(this.forwardDirection);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            this.forwardDirection = reader.ReadVector3();
        }

		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.Frozen;
		}
    }
}