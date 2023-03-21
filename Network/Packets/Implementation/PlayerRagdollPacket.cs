﻿using AMP.Network.Data.Sync;
using AMP.Network.Packets.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.PLAYER_RAGDOLL)]
    public class PlayerRagdollPacket : NetPacket {
        [SyncedVar]       public long         playerId;
        [SyncedVar]       public Vector3      position;
        [SyncedVar(true)] public float        rotationY;
        [SyncedVar(true)] public Vector3[]    ragdollPositions;
        [SyncedVar(true)] public Quaternion[] ragdollRotations;
        [SyncedVar(true)] public Vector3[]    velocities;
        [SyncedVar(true)] public Vector3[]    angularVelocities;

        public PlayerRagdollPacket() { }

        public PlayerRagdollPacket(long playerId, Vector3 position, float rotationY, Vector3[] ragdollPositions, Quaternion[] ragdollRotations, Vector3[] velocities, Vector3[] angularVelocities) {
            this.playerId     = playerId;
            this.position     = position;
            this.rotationY    = rotationY;
            this.ragdollPositions  = ragdollPositions;
            this.ragdollRotations  = ragdollRotations;
            this.velocities        = velocities;
            this.angularVelocities = angularVelocities;
        }

        public PlayerRagdollPacket(PlayerNetworkData pnd)
            : this( playerId: pnd.clientId
                  , position: pnd.position
                  , rotationY: pnd.rotationY
                  , ragdollPositions:  pnd.ragdollPositions
                  , ragdollRotations:  pnd.ragdollRotations
                  , velocities:        pnd.ragdollVelocity
                  , angularVelocities: pnd.ragdollAngularVelocity
                  ) {

        }
    }
}
