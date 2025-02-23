﻿using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace RiskOfRuinaMod.SkillStates.BaseStates
{
    public abstract class BaseThrowSpellState : BaseSkillState
    {
        public GameObject projectilePrefab;
        public GameObject muzzleflashEffectPrefab;
        public float baseDuration;
        public float minDamageCoefficient;
        public float maxDamageCoefficient;
        public float force;
        public float selfForce;
        private float duration;
        public float charge;
        public string throwSound;

        private ChildLocator childLocator { get; set; }

        public override void OnEnter()
        {
            base.OnEnter();

            this.childLocator = base.GetModelChildLocator();

            this.duration = this.baseDuration / this.attackSpeedStat;

            base.PlayAnimation("Gesture, Override", "CastSpell", "Spell.playbackRate", this.duration);

            if (this.muzzleflashEffectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(this.muzzleflashEffectPrefab, base.gameObject, "HandR", false);
            }

            Util.PlaySound(throwSound, base.gameObject);

            this.Fire();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.isAuthority && base.fixedAge >= this.duration)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        private void Fire()
        {
            if (base.isAuthority)
            {
                Ray aimRay = base.GetAimRay();

                if (this.projectilePrefab != null)
                {
                    float num = Util.Remap(this.charge, 0f, 1f, this.minDamageCoefficient, this.maxDamageCoefficient);
                    float num2 = this.charge * this.force;

                    FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                    {
                        projectilePrefab = this.projectilePrefab,
                        position = childLocator.FindChild("SpearSummon").position,
                        rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                        owner = base.gameObject,
                        damage = this.damageStat * num,
                        force = num2,
                        crit = base.RollCrit(),
                        speedOverride = 160f
                    };

                    ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                }

                if (base.characterMotor)
                {
                    base.characterMotor.ApplyForce(aimRay.direction * (-this.selfForce * this.charge), false, false);
                }
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
