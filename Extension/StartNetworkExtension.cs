﻿using AMP.Logging;
using AMP.Network.Client;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Data.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace AMP.Extension {
    public static class StartNetworkExtension {

        public static NetworkItem StartNetworking(this ItemNetworkData itemNetworkData) {
            NetworkItem networkItem = itemNetworkData.clientsideItem.gameObject.GetElseAddComponent<NetworkItem>();
            networkItem.Init(itemNetworkData);

            return networkItem;
        }

        public static NetworkCreature StartNetworking(this CreatureNetworkData creatureNetworkData) {
            NetworkCreature networkCreature = creatureNetworkData.clientsideCreature.gameObject.GetElseAddComponent<NetworkCreature>();
            networkCreature.Init(creatureNetworkData);

            return networkCreature;
        }

        public static NetworkPlayerCreature StartNetworking(this PlayerNetworkData playerNetworkData) {
            NetworkPlayerCreature networkPlayerCreature = playerNetworkData.creature.gameObject.GetElseAddComponent<NetworkPlayerCreature>();
            networkPlayerCreature.Init(playerNetworkData);

            return networkPlayerCreature;
        }

        public static NetworkLocalPlayer StartNetworking(this Player player) {
            NetworkLocalPlayer networkLocalPlayer = player.creature.gameObject.GetElseAddComponent<NetworkLocalPlayer>();

            return networkLocalPlayer;
        }

    }
}