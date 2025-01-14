﻿using AMP.Data;
using AMP.Extension;
using AMP.GameInteraction;
using AMP.Logging;
using AMP.Network.Data.Sync;
using AMP.Network.Packets.Implementation;
using AMP.SupportFunctions;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Client.NetworkComponents {
    internal class NetworkPlayerCreature : NetworkCreature {

        internal Transform handLeftTarget;
        internal Transform handRightTarget;
        internal Transform headTarget;

        private Vector3 handLeftPos;
        private Quaternion handLeftRot;
        internal Quaternion handLeftTargetRot;
        internal Vector3 handLeftRotVelocity;
        internal Vector3 handLeftTargetPos;
        private Vector3 handLeftTargetVel;

        private Vector3 handRightPos;
        private Quaternion handRightRot;
        internal Quaternion handRightTargetRot;
        internal Vector3 handRightRotVelocity;
        internal Vector3 handRightTargetPos;
        private Vector3 handRightTargetVel;

        private Vector3 headPos;
        private Quaternion headRot;
        internal Quaternion headTargetRot;
        internal Vector3 headRotVelocity;
        internal Vector3 headTargetPos;
        private Vector3 headTargetVel;
        
        internal float currentRotation = 0f;
        internal float targetRotation = 0f;
        private float rotationVelocity = 0f;

        protected PlayerNetworkData playerNetworkData;

        private float health = 1f;
        public HealthbarObject healthBar;

        internal void Init(PlayerNetworkData playerNetworkData) {
            if(this.playerNetworkData != playerNetworkData) registeredEvents = false;
            this.playerNetworkData = playerNetworkData;

            targetPos = this.playerNetworkData.position;
            //Log.Warn("INIT Player");

            UpdateCreature();
            RegisterEvents();
        }

        internal override bool IsSending() {
            return playerNetworkData.clientId == ModManager.clientInstance.netclient.ClientId;
        }

        protected new void OnAwake() {
            base.OnAwake();
        }

        void FixedUpdate() {
            
        }

        protected override void ManagedUpdate() {
            if(IsSending()) return;

            slowmo = playerNetworkData.health == 0;

            base.ManagedUpdate();

            //Log.Info("NetworkPlayerCreature");

            if(health != playerNetworkData.health) {
                if(healthBar != null) {
                    healthBar.SetHealth(playerNetworkData.health);
                }
                health = playerNetworkData.health;
            }

            currentRotation = Mathf.SmoothDampAngle(currentRotation, targetRotation, ref rotationVelocity, Config.MOVEMENT_DELTA_TIME);
            if(ragdollPositions == null) {
                transform.eulerAngles = new Vector3(0, currentRotation, 0);

                if(handLeftTarget == null) return;

                // Rotations
                handLeftRot = handLeftRot.SmoothDamp(handLeftTargetRot, ref handLeftRotVelocity, Config.MOVEMENT_DELTA_TIME);
                handRightRot = handRightRot.SmoothDamp(handRightTargetRot, ref handRightRotVelocity, Config.MOVEMENT_DELTA_TIME);
                headRot = headRot.SmoothDamp(headTargetRot, ref headRotVelocity, Config.MOVEMENT_DELTA_TIME);

                handLeftTarget.rotation = handLeftRot;
                handRightTarget.rotation = handRightRot;
                headTarget.rotation = headRot;

                // Positions
                handLeftPos = Vector3.SmoothDamp(handLeftPos, handLeftTargetPos, ref handLeftTargetVel, Config.MOVEMENT_DELTA_TIME);
                handRightPos = Vector3.SmoothDamp(handRightPos, handRightTargetPos, ref handRightTargetVel, Config.MOVEMENT_DELTA_TIME);
                headPos = Vector3.SmoothDamp(headPos, headTargetPos, ref headTargetVel, Config.MOVEMENT_DELTA_TIME);
                
                handLeftTarget.position = transform.position + handLeftPos;
                handRightTarget.position = transform.position + handRightPos;
                headTarget.position = headPos;
                headTarget.Translate(Vector3.forward);
            } else {
                if(healthBar != null) healthBar.transform.localEulerAngles = new Vector3(0, 180 + currentRotation, 0);
            }
        }


        internal override void UpdateCreature(bool reset_pos = false) {
            if(creature == null) return;

            base.UpdateCreature(reset_pos);

            creature.animator.enabled = false;
            creature.StopAnimation();
            creature.animator.speed = 0f;
            creature.locomotion.enabled = false;

            creature.ragdoll.standingUp = true;

            //creature.ragdoll.SetState(Ragdoll.State.Standing);

            // Freeze some components of the ragdoll so we dont have issues with gravity
            
            foreach(RagdollPart ragdollPart in creature.ragdoll.parts.Where(part => Config.playerRagdollTypesToFreeze.Contains(part.type))) {
                ragdollPart.transform.SetParent(creature.ragdoll.transform, true);
                ragdollPart.meshBone.SetParentOrigin(ragdollPart.transform);

                ragdollPart.physicBody.constraints = RigidbodyConstraints.FreezeAll;
                //ragdollPart.physicBody.ForceFreeze();
                ragdollPart.physicBody.isKinematic = false;
                ragdollPart.physicBody.useGravity = false;
            }

            foreach(RagdollPart ragdollPart in creature.ragdoll.parts.Where(part => !Config.playerRagdollTypesToFreeze.Contains(part.type))) {
                ragdollPart.physicBody.isKinematic = false;
                ragdollPart.physicBody.useGravity = false;
            }

            creature.locomotion.enabled = false;
            creature.SetAnimatorHeightRatio(0f);

            if(playerNetworkData.clientId != ModManager.clientInstance.netclient.ClientId) // Only because of DEBUG_SELF
                ClientSync.EquipItemsForCreature(playerNetworkData.clientId, Datatypes.ItemHolderType.PLAYER);
        }

        private bool registeredEvents = false;
        internal new void RegisterEvents() {
            if(registeredEvents) return;
            if(creature == null) return;

            creature.OnDamageEvent += Creature_OnDamageEvent;
            creature.OnHealEvent += Creature_OnHealEvent;
            creature.OnHeightChanged += Creature_OnHeightChanged;

            if(playerNetworkData.clientId != ModManager.clientInstance.netclient.ClientId) // Only because of DEBUG_SELF
                ClientSync.EquipItemsForCreature(playerNetworkData.clientId, Datatypes.ItemHolderType.PLAYER);

            registeredEvents = true;
        }

        private void Creature_OnDamageEvent(CollisionInstance collisionInstance, EventTime eventTime) {
            if(eventTime == EventTime.OnStart) return;
            //if(!collisionInstance.IsDoneByPlayer()) return; // Damage is not caused by the local player, so no need to mess with the other clients health
            if(collisionInstance.IsDoneByCreature(creature)) return; // If the damage is done by the creature itself, ignore it

            float damage = creature.currentHealth - creature.maxHealth; // Should be negative
            if(damage >= 0) return;
            creature.currentHealth = creature.maxHealth;

            new PlayerHealthChangePacket(playerNetworkData.clientId, damage).SendToServerReliable();

            Log.Debug(Defines.CLIENT, $"Damaged player {playerNetworkData.name} with {damage} damage.");
        }

        private void Creature_OnHealEvent(float heal, Creature healer, EventTime eventTime) {
            if(eventTime == EventTime.OnStart) return;
            if(healer == null) return;
            if(!healer.isPlayer) return;

            new PlayerHealthChangePacket(playerNetworkData.clientId, heal).SendToServerReliable();
            Log.Debug(Defines.CLIENT, $"Healed {playerNetworkData.name} with {heal} heal.");
        }

        private void Creature_OnHeightChanged() {
            if(creature.GetHeight() != playerNetworkData.height) {
                new SizeChangePacket(playerNetworkData);
            }
        }
    }
}
