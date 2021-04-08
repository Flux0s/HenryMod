using RoR2;
using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace HenryMod.Modules.Components
{
    public class DamageHistory : MonoBehaviour
    {

        internal struct EkkoDamageTick
        {
            public float damageTaken;
            public DateTime damageTime;
        }

        internal LinkedList<EkkoDamageTick> damageHistory;

        private void Awake()
        {
            this.damageHistory = new LinkedList<EkkoDamageTick>();
        }

        private void Start()
        {
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        private void OnDestroy()
        {
        }

        private void FixedUpdate()
        {
        }

        public void addDamage(float damageAmount, DateTime damageTime)
        {
            this.damageHistory.AddLast(new EkkoDamageTick() {damageTaken = damageAmount, damageTime = damageTime});
        }

        public void addDamage(float damageAmount)
        {
            this.damageHistory.AddLast(new EkkoDamageTick() {damageTaken = damageAmount, damageTime = DateTime.Now});
        }

        public void PruneDamageList()
        {
            while ((DateTime.Now - this.damageHistory.First.Value.damageTime).Seconds > SkillStates.Ekko.ChronoBreak.rewindLength)
            {
                this.damageHistory.RemoveFirst();
            }
        }

        public void ClearDamageList()
        {
            this.damageHistory.Clear();
        }

        public float GetTotalDamage()
        {
            float totalDamage = 0f;
            if (this.damageHistory.Count == 0) return 0f;
            LinkedListNode<EkkoDamageTick> currentNode = this.damageHistory.Last;
            while ((DateTime.Now - currentNode.Value.damageTime).Seconds <= SkillStates.Ekko.ChronoBreak.rewindLength)
            {
                Debug.LogWarning("Current node information id " + currentNode.Value.damageTaken + ", " + currentNode.Value.damageTime);
                totalDamage += currentNode.Value.damageTaken;
                if (!currentNode.Equals(this.damageHistory.First))
                {
                    currentNode = currentNode.Previous;
                }
                else
                {
                    return totalDamage;
                }
            }
            return totalDamage;
        }
    }
}