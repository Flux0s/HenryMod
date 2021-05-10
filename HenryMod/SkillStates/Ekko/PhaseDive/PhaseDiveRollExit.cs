using EntityStates;
using HenryMod.Modules.Components;
using HenryMod.SkillStates.BaseStates;
using RoR2;
using RoR2.Skills;
using System;
using UnityEngine;

namespace HenryMod.SkillStates.Ekko.PhaseDive
{
    public class PhaseDiveRollExit : BaseSkillState
    {
        public static float duration = 3f;
        private HenryTracker tracker;
        public static SkillDef stingerDef = Modules.Survivors.Ekko.phaseDiveLungeSkillDef;


        public override void OnEnter()
        {
            base.OnEnter();
            this.tracker = base.GetComponent<HenryTracker>();
            this.tracker.enabled = true;
            base.skillLocator.primary.SetSkillOverride(base.skillLocator.primary, PhaseDiveRollExit.stingerDef, GenericSkill.SkillOverridePriority.Replacement);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.isAuthority && base.fixedAge >= PhaseDiveRollExit.duration)
            {
                this.tracker.enabled = false;
                this.outer.SetNextStateToMain();
                return;
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            base.skillLocator.primary.UnsetSkillOverride(base.skillLocator.primary, PhaseDiveRollExit.stingerDef, GenericSkill.SkillOverridePriority.Replacement);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Any;
        }
    }
}