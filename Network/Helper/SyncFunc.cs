﻿using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Data.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Helper {
    internal class SyncFunc {

        internal static long DoesItemAlreadyExist(ItemNetworkData new_item, List<ItemNetworkData> items) {
            float dist = getCloneDistance(new_item.dataId);

            foreach(ItemNetworkData item in items) {
                if(item.position.Approximately(new_item.position, dist)) {
                    if(item.dataId.Equals(new_item.dataId)) {
                        return item.networkedId;
                    }
                }
            }

            return 0;
        }

        private static float getCloneDistance(string itemId) {
            float dist = Config.MEDIUM_ITEM_CLONE_MAX_DISTANCE;

            switch(itemId.ToLower()) {
                case "arrow":
                    dist = Config.SMALL_ITEM_CLONE_MAX_DISTANCE;
                    break;

                case "barrel1":
                case "wheelbarrowassembly_01":
                case "bench2m":
                case "shoebench":
                case "table2":
                    dist = Config.BIG_ITEM_CLONE_MAX_DISTANCE;
                    break;

                case "cranecrate":
                case "chandelier":
                    dist = 100 * 100; //100m should be enough
                    break;

                default: break;
            }

            return dist;
        }

        internal static Item DoesItemAlreadyExist(ItemNetworkData new_item, List<Item> items) {
            float dist = getCloneDistance(new_item.dataId);

            foreach(Item item in items) {
                if(item.transform.position.Approximately(new_item.position, dist)) {
                    if(item.itemId.Equals(new_item.dataId)) {
                        return item;
                    }
                }
            }

            return null;
        }

        internal static bool hasItemMoved(ItemNetworkData item) {
            if(item.clientsideItem == null) return false;
            if(!item.clientsideItem.isPhysicsOn) return false;
            if(item.clientsideItem.holder != null) return false;
            if(item.clientsideItem.mainHandler != null) return false;

            if(!item.position.Approximately(item.clientsideItem.transform.position, Config.REQUIRED_MOVE_DISTANCE)) {
                return true;
            } else if(!item.rotation.Approximately(item.clientsideItem.transform.eulerAngles, Config.REQUIRED_ROTATION_DISTANCE)) {
                return true;
            } else if(!item.velocity.Approximately(item.clientsideItem.rb.velocity, Config.REQUIRED_MOVE_DISTANCE)) {
                return true;
            } else if(!item.angularVelocity.Approximately(item.clientsideItem.rb.angularVelocity, Config.REQUIRED_MOVE_DISTANCE)) {
                return true;
            }

            return false;
        }

        internal static bool hasCreatureMoved(CreatureNetworkData creature) {
            if(creature.clientsideCreature == null) return false;

            if(creature.clientsideCreature.IsRagdolled()) {
                Vector3[] ragdollParts = creature.clientsideCreature.ReadRagdoll();

                if(creature.ragdollParts == null) return true;

                float distance = 0f;
                for(int i = 0; i < ragdollParts.Length; i += 2) {
                    distance += ragdollParts[i].SQ_DIST(creature.ragdollParts[i]);
                }
                return distance > Config.REQUIRED_RAGDOLL_MOVE_DISTANCE;
            } else {
                if(!creature.position.Approximately(creature.clientsideCreature.transform.position, Config.REQUIRED_MOVE_DISTANCE)) {
                    return true;
                } else if(!creature.rotation.Approximately(creature.clientsideCreature.transform.eulerAngles, Config.REQUIRED_ROTATION_DISTANCE)) {
                    return true;
                }/* else if(!creature.velocity.Approximately(creature.clientsideCreature.locomotion.rb.velocity, Config.REQUIRED_MOVE_DISTANCE)) {
                    return true;
                }*/
            }

            return false;
        }

        internal static bool hasPlayerMoved() {
            if(Player.currentCreature == null) return false;

            PlayerNetworkData playerSync = ModManager.clientSync.syncData.myPlayerData;

            if(!Player.currentCreature.transform.position.Approximately(playerSync.playerPos, Config.REQUIRED_PLAYER_MOVE_DISTANCE)) { return true; }
            //if(Mathf.Abs(Player.local.transform.eulerAngles.y - playerSync.playerRot) > REQUIRED_ROTATION_DISTANCE) return true;
            if(!Player.currentCreature.ragdoll.ik.handLeftTarget.position.Approximately(playerSync.handLeftPos, Config.REQUIRED_PLAYER_MOVE_DISTANCE)) { return true; }
            if(!Player.currentCreature.ragdoll.ik.handRightTarget.position.Approximately(playerSync.handRightPos, Config.REQUIRED_PLAYER_MOVE_DISTANCE)) { return true; }
            //if(Mathf.Abs(Player.currentCreature.ragdoll.headPart.transform.eulerAngles.y - playerSync.playerRot) > Config.REQUIRED_ROTATION_DISTANCE) { return true; }

            return false;
        }

        internal static bool GetCreature(Creature creature, out bool isPlayer, out long networkId) {
            isPlayer = false;
            networkId = -1;
            if(creature == null) return false;

            if(creature == Player.currentCreature) {
                networkId = ModManager.clientInstance.myClientId;
                isPlayer = true;
                return true;
            } else {
                try {
                    KeyValuePair<long, CreatureNetworkData> entry = ModManager.clientSync.syncData.creatures.First(value => creature.Equals(value.Value.clientsideCreature));
                    if(entry.Value.networkedId > 0) {
                        networkId = entry.Value.networkedId;
                        return true;
                    }
                } catch(InvalidOperationException) { }
            }

            return false;
        }

    }
}
