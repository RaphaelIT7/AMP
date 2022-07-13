﻿using AMP.Network.Data.Sync;
using System.Collections.Generic;

namespace AMP.Network.Data {
    public class SyncData {
        public int currentClientItemId = 1;
        public int currentClientCreatureId = 1;

        public Dictionary<int, ItemSync> items = new Dictionary<int, ItemSync>();

        public Dictionary<int, PlayerSync> players = new Dictionary<int, PlayerSync>();

        public Dictionary<int, CreatureSync> creatures = new Dictionary<int, CreatureSync>();

        public PlayerSync myPlayerData = new PlayerSync();
        
        public string serverlevel;
        public string servermode;
    }
}
