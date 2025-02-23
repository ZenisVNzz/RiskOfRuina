﻿using RoR2;
using RoR2.Projectile;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RiskOfRuinaMod.Modules.Misc
{
    [RequireComponent(typeof(TeamFilter))]
    public class ArbiterPillarController : NetworkBehaviour
    {
        [SyncVar]
        [Tooltip("The area of effect.")]
        public float radius;
        [Tooltip("How long between buff pulses in the area of effect.")]
        public float interval = 1f;
        [Tooltip("The child range indicator object. Will be scaled to the radius.")]
        public Transform rangeIndicator;
        [Tooltip("The buff type to grant")]
        public BuffDef buffDef;
        [Tooltip("The buff duration")]
        public float buffDuration;
        [Tooltip("Does the ward disappear over time?")]
        public bool expires;
        [Tooltip("If set, stops all projectiles in the vicinity.")]
        public bool freezeProjectiles;
        public float expireDuration;
        public bool animateRadius;
        public AnimationCurve radiusCoefficientCurve;
        [Tooltip("If set, the ward will give you this amount of time to play removal effects.")]
        public float removalTime;
        private bool needsRemovalTime;
        public string removalSoundString = "";
        public UnityEvent onRemoval;
        private float buffTimer;
        private float rangeIndicatorScaleVelocity;
        private float stopwatch;
        private float calculatedRadius;
        private TeamFilter teamFilter;

        private void Awake()
        {
            this.teamFilter = base.GetComponent<TeamFilter>();
        }

        private void OnEnable()
        {
            if (this.rangeIndicator)
            {
                this.rangeIndicator.gameObject.SetActive(true);
            }
        }

        private void OnDisable()
        {
            if (this.rangeIndicator)
            {
                this.rangeIndicator.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            Util.PlaySound("Play_Binah_Stone_Fire", this.gameObject);

            if (this.removalTime > 0f)
            {
                this.needsRemovalTime = true;
            }

            if (this.rangeIndicator && this.expires)
            {
                ScaleParticleSystemDuration component = this.rangeIndicator.GetComponent<ScaleParticleSystemDuration>();
                if (component)
                {
                    component.newDuration = this.expireDuration;
                }
            }

            if (NetworkServer.active)
            {
                float radiusSqr = this.calculatedRadius * this.calculatedRadius;

                Vector3 position = base.transform.position;
                for (TeamIndex teamIndex = TeamIndex.Neutral; teamIndex < TeamIndex.Count; teamIndex += 1)
                {
                    this.BuffTeam(TeamComponent.GetTeamMembers(teamIndex), radiusSqr, position);
                }
            }
        }

        private void Update()
        {
            this.calculatedRadius = (this.animateRadius ? (this.radius * this.radiusCoefficientCurve.Evaluate(this.stopwatch / this.expireDuration)) : this.radius);
            this.stopwatch += Time.deltaTime;

            if (this.expires && NetworkServer.active)
            {
                if (this.needsRemovalTime)
                {
                    if (this.stopwatch >= this.expireDuration - this.removalTime)
                    {
                        this.needsRemovalTime = false;
                        Util.PlaySound(this.removalSoundString, base.gameObject);
                        this.onRemoval.Invoke();
                    }
                }
                else if (this.expireDuration <= this.stopwatch)
                {
                    UnityEngine.Object.Destroy(base.gameObject);
                }
            }

            if (this.rangeIndicator)
            {
                float num = Mathf.SmoothDamp(this.rangeIndicator.localScale.x, this.calculatedRadius, ref this.rangeIndicatorScaleVelocity, 0.2f);
                this.rangeIndicator.localScale = new Vector3(num, num, num);
            }
        }

        private void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                this.buffTimer -= Time.fixedDeltaTime;
                if (this.buffTimer <= 0f)
                {
                    this.buffTimer = this.interval;
                    float radiusSqr = this.calculatedRadius * this.calculatedRadius;

                    Vector3 position = base.transform.position;

                    for (TeamIndex teamIndex = TeamIndex.Neutral; teamIndex < TeamIndex.Count; teamIndex += 1)
                    {
                        this.BuffTeam(TeamComponent.GetTeamMembers(teamIndex), radiusSqr, position);
                    }
                }
            }

            if (this.freezeProjectiles)
            {
                this.FreezeProjectiles(this.calculatedRadius, base.transform.position);
            }
        }

        private void BuffTeam(IEnumerable<TeamComponent> recipients, float radiusSqr, Vector3 currentPosition)
        {
            if (!NetworkServer.active)
            {
                return;
            }

            foreach (TeamComponent teamComponent in recipients)
            {
                if ((teamComponent.transform.position - currentPosition).sqrMagnitude <= radiusSqr)
                {
                    CharacterBody charBody = teamComponent.body;
                    if (charBody)
                    {
                        if (this.buffDef) charBody.AddTimedBuff(this.buffDef, this.buffDuration);
                    }
                }
            }
        }

        private void FreezeProjectiles(float radius, Vector3 currentPosition)
        {
            Collider[] projectiles = Physics.OverlapSphere(currentPosition, radius, LayerIndex.projectile.mask);

            for (int i = 0; i < projectiles.Length; i++)
            {
                ProjectileController projectile = projectiles[i].GetComponent<ProjectileController>();
                if (projectile)
                {
                    TeamComponent projectileTeam = projectile.owner.GetComponent<TeamComponent>();
                    if (projectileTeam)
                    {
                        if (projectileTeam.teamIndex != this.teamFilter.teamIndex)
                        {
                            EffectData effectData = new EffectData();
                            effectData.origin = projectile.transform.position;
                            effectData.scale = 4;

                            EffectManager.SpawnEffect(Modules.Assets.fairyDeleteEffect, effectData, false);

                            Destroy(projectile.gameObject);
                        }
                    }
                }
            }
        }

        public override bool OnSerialize(NetworkWriter writer, bool forceAll)
        {
            if (forceAll)
            {
                writer.Write(this.radius);
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

                writer.Write(this.radius);
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
                this.radius = reader.ReadSingle();
                return;
            }

            int num = (int)reader.ReadPackedUInt32();
            if ((num & 1) != 0)
            {
                this.radius = reader.ReadSingle();
            }
        }
    }
}
