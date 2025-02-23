﻿using EntityStates.Mage;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfRuinaMod.SkillStates
{
    class BasicAttack : BaseStates.BaseDirectionalSkill
    {
        public override void OnEnter()
        {
            if (this.attackIndex > 3)
            {
                this.attackIndex = 1;
            }

            this.hitboxName = "Basic";
            if (this.attackIndex == 3) this.hitboxName = "BasicThird";


            this.damageCoefficient = Modules.StaticValues.basicAttackDamageCoefficient;
            if (this.attackIndex == 3) this.damageCoefficient = Modules.StaticValues.basicAttackThirdHitDamageCoefficient;
            this.baseDuration = 1.3f;
            this.attackStartTime = 0.2f;
            this.attackEndTime = 0.4f;
            this.baseEarlyExitTime = 0.8f;
            if (this.attackIndex == 3) this.baseEarlyExitTime = 0.6f;
            this.hitStopDuration = 0.05f;
            if (this.attackIndex == 3) this.pushForce = 600f;

            this.swingSoundString = "Ruina_Swipe";
            this.impactSound = Modules.Assets.swordHitSoundVert.index;
            if (this.attackIndex == 3) this.impactSound = Modules.Assets.swordHitSoundHori.index;
            switch (attackIndex)
            {
                case (1):
                    this.muzzleString = "BasicSwing1";
                    break;
                case (2):
                    this.muzzleString = "BasicSwing2";
                    break;
                case (3):
                    this.muzzleString = "BasicSwing3";
                    break;
            }
            this.hitEffectPrefab = Modules.Assets.swordHitEffect;

            base.OnEnter();

            this.swingEffectPrefab = statTracker.slashPrefab;
        }

        protected override void PlayAttackAnimation()
        {
            base.PlayCrossfade("FullBody, Override", "BasicSlash" + attackIndex, "BaseAttack.playbackRate", this.duration, 0.1f);
        }

        protected override void PlaySwingEffect()
        {
            base.PlaySwingEffect();
        }

        protected override void OnHitEnemyAuthority()
        {
            base.OnHitEnemyAuthority();
        }

        protected override void FireAttack()
        {
            base.FireAttack();

            float num = Mathf.Clamp(0f, 0.5f, 0.5f * trueMoveSpeed);
            base.characterMotor.rootMotion += base.characterDirection.forward * (num * FlyUpState.speedCoefficientCurve.Evaluate(base.fixedAge / this.duration) * Time.fixedDeltaTime);
        }

        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
