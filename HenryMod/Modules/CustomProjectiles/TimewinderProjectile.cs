using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using RoR2;
using RoR2.Projectile;

namespace HenryMod.Modules.CustomProjectiles
{
    [RequireComponent(typeof(ProjectileController))]
    public class TimewinderProjectile : BoomerangProjectile
    {
        private new void Awake()
        {
            this.rigidbody = base.GetComponent<Rigidbody>();
            this.projectileController = base.GetComponent<ProjectileController>();
            this.projectileDamage = base.GetComponent<ProjectileDamage>();
            if (this.projectileController && this.projectileController.owner)
            {
                this.ownerTransform = this.projectileController.owner.transform;
            }
            this.maxFlyStopwatch = this.charge * this.distanceMultiplier;
        }

        private new void Start()
        {
            float num = this.charge * 7f;
            if (num < 1f)
            {
                num = 1f;
            }
            Vector3 localScale = new Vector3(num * base.transform.localScale.x, num * base.transform.localScale.y, num * base.transform.localScale.z);
            // base.transform.localScale = localScale;
            // base.gameObject.GetComponent<ProjectileController>().ghost.transform.localScale = localScale;
            // base.GetComponent<ProjectileDotZone>().damageCoefficient *= num;
        }

        public new void OnProjectileImpact(ProjectileImpactInfo impactInfo)
        {
            if (!this.canHitWorld)
            {
                return;
            }
            this.NetworktimewinderState = TimewinderProjectile.TimewinderState.FlyBack;
            UnityEvent unityEvent = this.onFlyBack;
            if (unityEvent != null)
            {
                unityEvent.Invoke();
            }
            EffectManager.SimpleImpactEffect(this.impactSpark, impactInfo.estimatedPointOfImpact, -base.transform.forward, true);
        }

        private new bool Reel()
        {
            Vector3 vector = this.projectileController.owner.transform.position - base.transform.position;
            Vector3 normalized = vector.normalized;
            return vector.magnitude <= 2f;
        }

        public new void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                if (!this.setScale)
                {
                    this.setScale = true;
                }
                if (!this.projectileController.owner)
                {
                    UnityEngine.Object.Destroy(base.gameObject);
                    return;
                }
                switch (this.timewinderState)
                {
                    case TimewinderProjectile.TimewinderState.FlyOut:
                        if (NetworkServer.active)
                        {
                            this.rigidbody.velocity = this.travelSpeed * base.transform.forward;
                            this.stopwatch += Time.fixedDeltaTime;
                            if (this.stopwatch >= this.maxFlyStopwatch)
                            {
                                this.stopwatch = 0f;
                                this.NetworktimewinderState = TimewinderProjectile.TimewinderState.SlowDown;
                                return;
                            }
                        }
                        break;
                    case TimewinderProjectile.TimewinderState.SlowDown:
                        if (NetworkServer.active)
                        {
                            if (!this.slowDownFlag)
                            {
                                this.slowDownFlag = true;
                                float num = this.charge * 7f;
                                Vector3 localScale = new Vector3(num * base.transform.localScale.x, num * base.transform.localScale.y, num * base.transform.localScale.z);
                                base.transform.localScale = localScale;
                                base.gameObject.GetComponent<ProjectileController>().ghost.transform.localScale = localScale;
                                this.rigidbody.velocity /= this.slowDownMultiplier;
                            }
                            this.stopwatch += Time.fixedDeltaTime;
                            if (this.stopwatch >= this.maxSlowDownStopwatch)
                            {
                                this.stopwatch = 0f;
                                this.NetworktimewinderState = TimewinderProjectile.TimewinderState.FlyBack;
                                ProjectileOverlapAttack projectileOverlapAttack = base.GetComponent<ProjectileOverlapAttack>();
                                projectileOverlapAttack.ResetOverlapAttack();
                                return;
                            }
                        }
                        break;
                    case TimewinderProjectile.TimewinderState.Transition:
                        {
                            this.stopwatch += Time.fixedDeltaTime;
                            float num = this.stopwatch / this.transitionDuration;
                            Vector3 a = this.CalculatePullDirection();
                            this.rigidbody.velocity = Vector3.Lerp(this.travelSpeed * base.transform.forward, this.travelSpeed * a, num);
                            if (num >= 1f)
                            {
                                this.NetworktimewinderState = TimewinderProjectile.TimewinderState.FlyBack;
                                UnityEvent unityEvent = this.onFlyBack;
                                if (unityEvent == null)
                                {
                                    return;
                                }
                                unityEvent.Invoke();
                                return;
                            }
                            break;
                        }
                    case TimewinderProjectile.TimewinderState.FlyBack:
                        {
                            if (!this.returnFlag)
                            {
                                this.returnFlag = true;
                                float num = this.charge / 7f;
                                Vector3 localScale = new Vector3(num * base.transform.localScale.x, num * base.transform.localScale.y, num * base.transform.localScale.z);
                                base.transform.localScale = localScale;
                                base.gameObject.GetComponent<ProjectileController>().ghost.transform.localScale = localScale;
                            }
                            bool flag = this.Reel();
                            if (NetworkServer.active)
                            {
                                this.canHitWorld = false;
                                Vector3 a2 = this.CalculatePullDirection();
                                this.rigidbody.velocity = this.returnSpeed * a2;
                                if (flag)
                                {
                                    UnityEngine.Object.Destroy(base.gameObject);
                                }
                            }
                            break;
                        }
                    default:
                        return;
                }
            }
        }

        private Vector3 CalculatePullDirection()
        {
            if (this.projectileController.owner)
            {
                return (this.projectileController.owner.transform.position - base.transform.position).normalized;
            }
            return base.transform.forward;
        }

        public TimewinderProjectile.TimewinderState NetworktimewinderState
        {
            get
            {
                return this.timewinderState;
            }
            set
            {
                ulong newValueAsUlong = (ulong)((long)value);
                ulong fieldValueAsUlong = (ulong)((long)this.timewinderState);
                base.SetSyncVarEnum<TimewinderProjectile.TimewinderState>(value, newValueAsUlong, ref this.timewinderState, fieldValueAsUlong, 1U);
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool forceAll)
        {
            if (forceAll)
            {
                writer.Write((int)this.timewinderState);
                return true;
            }
            bool flag = false;
            if ((base.syncVarDirtyBits & 1U) != 0U)
            {
                if (!flag)
                {
                    writer.WritePackedUInt32(base.syncVarDirtyBits);
                    flag = true;
                }
                writer.Write((int)this.timewinderState);
            }
            if (!flag)
            {
                writer.WritePackedUInt32(base.syncVarDirtyBits);
            }
            return flag;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                this.timewinderState = (TimewinderProjectile.TimewinderState)reader.ReadInt32();
                return;
            }
            int num = (int)reader.ReadPackedUInt32();
            if ((num & 1) != 0)
            {
                this.timewinderState = (TimewinderProjectile.TimewinderState)reader.ReadInt32();
            }
        }

        public new float travelSpeed = 10f;
        public float returnSpeed = 60f;
        public float slowDownMultiplier = 2f;
        public float maxSlowDownStopwatch = 1.5f;
        private bool slowDownFlag = false;
        private bool returnFlag = false;
        public new float charge;
        public new float transitionDuration;
        private new float maxFlyStopwatch;
        public new GameObject impactSpark;
        public new GameObject crosshairPrefab;
        public new bool canHitCharacters;
        public new bool canHitWorld;
        private new ProjectileController projectileController;
        [SyncVar]
        private TimewinderProjectile.TimewinderState timewinderState;
        private new Transform ownerTransform;
        private new ProjectileDamage projectileDamage;
        private new Rigidbody rigidbody;
        private new float stopwatch;
        private new float fireAge;
        private new float fireFrequency;
        public new float distanceMultiplier = 2f;
        public new UnityEvent onFlyBack;
        private new bool setScale;
        public enum TimewinderState
        {
            FlyOut,
            SlowDown,
            Transition,
            FlyBack
        }
    }
}
