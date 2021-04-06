using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using UnityEngine;
using UnityEngine.Networking;
using BepInEx;

namespace HenryMod.Modules
{
    internal static class Projectiles
    {
        internal static GameObject timewinderPrefab;

        internal static GameObject bombPrefab;
        internal static GameObject bazookaRocketPrefab;

        internal static GameObject voidBlastPrefab;

        internal static void RegisterProjectiles()
        {
            // only separating into separate methods for my sanity
            CreateTimewinder();

            CreateBomb();
            CreateBazookaRocket();

            CreateVoidBlast();

            AddProjectile(timewinderPrefab);

            AddProjectile(bombPrefab);
            AddProjectile(bazookaRocketPrefab);
            AddProjectile(voidBlastPrefab);
        }

        internal static void AddProjectile(GameObject projectileToAdd)
        {
            Modules.Prefabs.projectilePrefabs.Add(projectileToAdd);
        }

        private static void CreateVoidBlast()
        {
            voidBlastPrefab = CloneProjectilePrefab("CommandoGrenadeProjectile", "NemryVoidBlastProjectile");

            ProjectileImpactExplosion bombImpactExplosion = voidBlastPrefab.GetComponent<ProjectileImpactExplosion>();
            InitializeImpactExplosion(bombImpactExplosion);

            bombImpactExplosion.blastRadius = 8f;
            bombImpactExplosion.destroyOnEnemy = true;
            bombImpactExplosion.lifetime = 12f;
            bombImpactExplosion.impactEffect = Resources.Load<GameObject>("Prefabs/Effects/NullifierExplosion");
            //bombImpactExplosion.lifetimeExpiredSound = Modules.Assets.CreateNetworkSoundEventDef("HenryBombExplosion");
            bombImpactExplosion.timerAfterImpact = true;
            bombImpactExplosion.lifetimeAfterImpact = 0.1f;

            ProjectileDamage bombDamage = voidBlastPrefab.GetComponent<ProjectileDamage>();
            bombDamage.damageType = DamageType.Nullify;

            ProjectileController bombController = voidBlastPrefab.GetComponent<ProjectileController>();
            bombController.ghostPrefab = Resources.Load<GameObject>("Prefabs/ProjectileGhosts/NullifierPreBombGhost");
            bombController.startSound = "";

            voidBlastPrefab.GetComponent<Rigidbody>().useGravity = false;
        }

        private static void CreateBomb()
        {
            bombPrefab = CloneProjectilePrefab("CommandoGrenadeProjectile", "HenryBombProjectile");

            ProjectileImpactExplosion bombImpactExplosion = bombPrefab.GetComponent<ProjectileImpactExplosion>();
            InitializeImpactExplosion(bombImpactExplosion);

            bombImpactExplosion.blastRadius = 16f;
            bombImpactExplosion.destroyOnEnemy = true;
            bombImpactExplosion.lifetime = 12f;
            bombImpactExplosion.impactEffect = Modules.Assets.bombExplosionEffect;
            //bombImpactExplosion.lifetimeExpiredSound = Modules.Assets.CreateNetworkSoundEventDef("HenryBombExplosion");
            bombImpactExplosion.timerAfterImpact = true;
            bombImpactExplosion.lifetimeAfterImpact = 0.1f;

            ProjectileController bombController = bombPrefab.GetComponent<ProjectileController>();
            bombController.ghostPrefab = CreateGhostPrefab("HenryBombGhost");
            bombController.startSound = "";
        }

        private static void CreateTimewinder()
        {
            timewinderPrefab = CloneProjectilePrefab("Sawmerang", "EkkoSawmerang");
            CustomProjectiles.TimewinderProjectile timewinderProjectile = timewinderPrefab.AddComponent<CustomProjectiles.TimewinderProjectile>();

            BoomerangProjectile boomerangProjectile = timewinderPrefab.GetComponent<BoomerangProjectile>();
            timewinderProjectile.impactSpark = boomerangProjectile.impactSpark;
            timewinderProjectile.crosshairPrefab = boomerangProjectile.crosshairPrefab;
            BaseUnityPlugin.DestroyImmediate(boomerangProjectile);

            timewinderProjectile.travelSpeed = 30f;
            timewinderProjectile.slowDownMultiplier = 4f;
            timewinderProjectile.returnSpeed = 60f;
            timewinderProjectile.charge = 1f;
            timewinderProjectile.transitionDuration = 1;
            timewinderProjectile.canHitCharacters = false;
            timewinderProjectile.canHitWorld = false;
            timewinderProjectile.distanceMultiplier = 0.6f;

            ProjectileDamage projectileDamage = timewinderPrefab.GetComponent<ProjectileDamage>();
            projectileDamage.damageType = DamageType.BlightOnHit | DamageType.SlowOnHit;
            // projectileDamage.damage = 1f;

            ProjectileOverlapAttack projectileOverlapAttack = timewinderPrefab.GetComponent<ProjectileOverlapAttack>();
            projectileOverlapAttack.SetDamageCoefficient(1f);

            ProjectileDotZone projectileDotZone = timewinderPrefab.GetComponent<ProjectileDotZone>();
            BaseUnityPlugin.DestroyImmediate(projectileDotZone);
        }

        private static void CreateBazookaRocket()
        {
            bazookaRocketPrefab = CloneProjectilePrefab("CommandoGrenadeProjectile", "HenryBazookaRocketProjectile");
            bazookaRocketPrefab.AddComponent<Modules.Components.BazookaRotation>();
            bazookaRocketPrefab.transform.localScale *= 2f;

            ProjectileImpactExplosion bazookaImpactExplosion = bazookaRocketPrefab.GetComponent<ProjectileImpactExplosion>();
            InitializeImpactExplosion(bazookaImpactExplosion);

            bazookaImpactExplosion.blastRadius = 8f;
            bazookaImpactExplosion.destroyOnEnemy = true;
            bazookaImpactExplosion.lifetime = 12f;
            bazookaImpactExplosion.impactEffect = Modules.Assets.bazookaExplosionEffect;
            //bazookaImpactExplosion.lifetimeExpiredSound = Modules.Assets.CreateNetworkSoundEventDef("HenryBazookaExplosion");
            bazookaImpactExplosion.timerAfterImpact = true;
            bazookaImpactExplosion.lifetimeAfterImpact = 0f;

            ProjectileController bazookaController = bazookaRocketPrefab.GetComponent<ProjectileController>();

            GameObject bazookaGhost = CreateGhostPrefab("HenryBazookaRocketGhost");
            bazookaGhost.GetComponentInChildren<ParticleSystem>().gameObject.AddComponent<Modules.Components.DetachOnDestroy>();

            bazookaController.ghostPrefab = bazookaGhost;
            bazookaController.startSound = "";
        }

        private static void InitializeImpactExplosion(ProjectileImpactExplosion projectileImpactExplosion)
        {
            projectileImpactExplosion.blastDamageCoefficient = 1f;
            projectileImpactExplosion.blastProcCoefficient = 1f;
            projectileImpactExplosion.blastRadius = 1f;
            projectileImpactExplosion.bonusBlastForce = Vector3.zero;
            projectileImpactExplosion.childrenCount = 0;
            projectileImpactExplosion.childrenDamageCoefficient = 0f;
            projectileImpactExplosion.childrenProjectilePrefab = null;
            projectileImpactExplosion.destroyOnEnemy = false;
            projectileImpactExplosion.destroyOnWorld = false;
            projectileImpactExplosion.explosionSoundString = "";
            projectileImpactExplosion.falloffModel = RoR2.BlastAttack.FalloffModel.None;
            projectileImpactExplosion.fireChildren = false;
            projectileImpactExplosion.impactEffect = null;
            projectileImpactExplosion.lifetime = 0f;
            projectileImpactExplosion.lifetimeAfterImpact = 0f;
            projectileImpactExplosion.lifetimeExpiredSoundString = "";
            projectileImpactExplosion.lifetimeRandomOffset = 0f;
            projectileImpactExplosion.offsetForLifetimeExpiredSound = 0f;
            projectileImpactExplosion.timerAfterImpact = false;

            projectileImpactExplosion.GetComponent<ProjectileDamage>().damageType = DamageType.Generic;
        }

        private static GameObject CreateGhostPrefab(string ghostName)
        {
            GameObject ghostPrefab = Modules.Assets.mainAssetBundle.LoadAsset<GameObject>(ghostName);
            if (!ghostPrefab.GetComponent<NetworkIdentity>()) ghostPrefab.AddComponent<NetworkIdentity>();
            if (!ghostPrefab.GetComponent<ProjectileGhostController>()) ghostPrefab.AddComponent<ProjectileGhostController>();

            Modules.Assets.ConvertAllRenderersToHopooShader(ghostPrefab);

            return ghostPrefab;
        }

        private static GameObject CloneProjectilePrefab(string prefabName, string newPrefabName)
        {
            GameObject newPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/Projectiles/" + prefabName), newPrefabName);
            return newPrefab;
        }
    }
}