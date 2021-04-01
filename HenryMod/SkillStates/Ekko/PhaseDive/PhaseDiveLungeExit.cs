using EntityStates;
using HenryMod.Modules.Components;
using HenryMod.SkillStates.BaseStates;
using RoR2;
using UnityEngine;

namespace HenryMod.SkillStates.Ekko.PhaseDive
{
    public class PhaseDiveLungeExit : BaseSkillState
    {
        public static float duration = 0.4f;

        public override void OnEnter()
        {
            base.OnEnter();
			base.SmallHop(base.characterMotor, PhaseDiveLunge.hopForce);
			base.PlayAnimation("FullBody, Override", "LungeSlash", "LungeSlash.playbackRate", 2f * PhaseDiveLungeExit.duration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.isAuthority && base.fixedAge >= PhaseDiveLungeExit.duration)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Frozen;
        }
    }
}