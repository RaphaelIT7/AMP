﻿using AMP.Data;
using AMP.Logging;
using AMP.Network.Client;
using AMP.Network.Handler;
using AMP.Network.Packets;
using AMP.Network.Packets.Implementation;
using AMP.SupportFunctions;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using static ThunderRoad.Trigger;

namespace AMP.SteamNet {
    internal class SteamNetHandler : NetworkHandler {

        public struct LobbyMetaData {
            public string key;
            public string value;
        }

        public struct LobbyMembers {
            public CSteamID steamId;
            public LobbyMetaData[] data;
        }

        public struct Lobby {
            public CSteamID lobbySteamId;
            public CSteamID ownerSteamId;
            public LobbyMembers[] members;
            public int memberLimit;
            public LobbyMetaData[] data;
        }

        public Lobby currentLobby;

        public bool IsHost {
            get { 
                return currentLobby.ownerSteamId == SteamIntegration.Instance.mySteamId;
            }
        }

        // Various callback functions that Steam will call to let us know about whether we should
        // allow clients to play or we should kick/deny them.
        //
        // Tells us a client has been authenticated and approved to play by Steam (passes auth, license check, VAC status, etc...)
        protected Callback<ValidateAuthTicketResponse_t> callbackGSAuthTicketResponse;

        // client connection state
        protected Callback<P2PSessionRequest_t> callbackP2PSessionRequest;
        protected Callback<P2PSessionConnectFail_t> callbackP2PSessionConnectFail;

        protected Callback<LobbyCreated_t> callbackLobbyCreated;
        protected Callback<LobbyEnter_t> callbackLobbyEnter;
        protected Callback<LobbyChatUpdate_t> callbackLobbyChatUpdate;

        private SteamSocket reliableSocket;
        private SteamSocket unreliableSocket;

        public SteamNetHandler() {
            SteamNetworking.AllowP2PPacketRelay(true);

            RegisterCallbacks();
        }

        internal override void Connect(string password = "") {
            Thread connectionThread = new Thread(() => {
                int cnt = 5;
                while(ModManager.clientInstance.myPlayerId == 0 && cnt >= 0) {
                    SendReliable(new EstablishConnectionPacket(UserData.GetUserName(), Defines.MOD_VERSION, password));
                    cnt--;
                    Thread.Sleep(500);
                }
                if(ModManager.clientInstance.myPlayerId == 0) {
                    Log.Err(Defines.CLIENT, $"Couldn't establish a connection, handshake with server failed after multiple retries.");
                }
            });
            connectionThread.Name = "Establish Connection Thread";
            connectionThread.Start();
        }

        public void CreateLobby(uint maxClients) {
            Log.Debug(Defines.STEAM_API, "Creating Lobby...");
            isConnected = false;
            currentLobby = default(Lobby);
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, (int) maxClients);
        }

        public void RegisterCallbacks() {
            callbackGSAuthTicketResponse  = Callback<ValidateAuthTicketResponse_t>.Create(OnValidateAuthTicketResponse);
            callbackP2PSessionRequest     = Callback<P2PSessionRequest_t>.         Create(OnP2PSessionRequest);
            callbackP2PSessionConnectFail = Callback<P2PSessionConnectFail_t>.     Create(OnP2PSessionConnectFail);
            callbackLobbyCreated          = Callback<LobbyCreated_t>.              Create(OnLobbyCreated);
            callbackLobbyEnter            = Callback<LobbyEnter_t>.                Create(OnLobbyEnter);
            callbackLobbyChatUpdate       = Callback<LobbyChatUpdate_t>.           Create(OnLobbyChatUpdate);
        }

        private void OnLobbyChatUpdate(LobbyChatUpdate_t pCallback) {
            Log.Debug(Defines.STEAM_API, $"Lobby changed: {pCallback.m_ulSteamIDLobby}");
            UpdateLobbyInfo((CSteamID) pCallback.m_ulSteamIDLobby, ref currentLobby);
        }

        private void OnLobbyEnter(LobbyEnter_t pCallback) {
            if(pCallback.m_ulSteamIDLobby > 0) {
                Log.Debug(Defines.STEAM_API, $"Lobby joined: {pCallback.m_ulSteamIDLobby}");
                UpdateLobbyInfo((CSteamID) pCallback.m_ulSteamIDLobby, ref currentLobby);
                isConnected = true;
            }
        }

        void OnLobbyCreated(LobbyCreated_t pCallback) {
            if(pCallback.m_eResult != EResult.k_EResultOK) {
                Log.Err(Defines.STEAM_API, "OnLobbyCreated encountered an Failure");
                return;
            }

            Log.Debug(Defines.STEAM_API, $"Lobby created: {pCallback.m_ulSteamIDLobby}");

            UpdateLobbyInfo((CSteamID) pCallback.m_ulSteamIDLobby, ref currentLobby);
        }

        void UpdateLobbyInfo(CSteamID steamIDLobby, ref Lobby outLobby) {
            outLobby.lobbySteamId = steamIDLobby;
            outLobby.ownerSteamId = SteamMatchmaking.GetLobbyOwner(steamIDLobby);
            outLobby.members      = new LobbyMembers[SteamMatchmaking.GetNumLobbyMembers(steamIDLobby)];
            outLobby.memberLimit  = SteamMatchmaking.GetLobbyMemberLimit(steamIDLobby);

            for(int i = 0; i < outLobby.members.Length; i++) {
                outLobby.members[i].steamId = SteamMatchmaking.GetLobbyMemberByIndex(steamIDLobby, i);
                if(IsHost) {
                    long playerId = (long)(ulong)outLobby.members[i].steamId;
                    if(!ModManager.serverInstance.clients.ContainsKey(playerId)) {
                        ModManager.serverInstance.EstablishConnection(playerId);
                    }
                }
            }

            int nDataCount = SteamMatchmaking.GetLobbyDataCount(steamIDLobby);
            outLobby.data = new LobbyMetaData[nDataCount];
            for(int i = 0; i < nDataCount; ++i) {
                bool lobbyDataRet = SteamMatchmaking.GetLobbyDataByIndex(steamIDLobby, i, out outLobby.data[i].key, Constants.k_nMaxLobbyKeyLength, out outLobby.data[i].value, Constants.k_cubChatMetadataMax);
                if(!lobbyDataRet) {
                    Log.Err(Defines.STEAM_API, "SteamMatchmaking.GetLobbyDataByIndex returned false.");
                    continue;
                }
            }

            if(reliableSocket   == null) reliableSocket   = new SteamSocket(outLobby.ownerSteamId, EP2PSend.k_EP2PSendReliable,          Defines.STEAM_RELIABLE_CHANNEL  );
            if(unreliableSocket == null) unreliableSocket = new SteamSocket(outLobby.ownerSteamId, EP2PSend.k_EP2PSendUnreliableNoDelay, Defines.STEAM_UNRELIABLE_CHANNEL);
        }

        void OnValidateAuthTicketResponse(ValidateAuthTicketResponse_t pResponse) {
            Log.Debug(Defines.STEAM_API, "OnValidateAuthTicketResponse Called steamID: " + pResponse.m_SteamID);

            if(pResponse.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseOK) {
                
            } else {
                
            }
        }

        void OnP2PSessionRequest(P2PSessionRequest_t pCallback) {
            Log.Debug(Defines.STEAM_API, "OnP2PSesssionRequest Called steamIDRemote: " + pCallback.m_steamIDRemote);

            // Check if the user trying to connect is part of the lobby (when user is host)
            // or the host (when user is client)
            bool allow = false;
            if(IsHost) { // User is the host
                foreach(LobbyMembers member in currentLobby.members) { // Check if the person trying to connect is a member of the lobby
                    if(pCallback.m_steamIDRemote == member.steamId) {
                        allow = true;
                        break;
                    }
                }
            } else { // User is a client,
                if(pCallback.m_steamIDRemote == currentLobby.ownerSteamId) { // Check if the person trying to connect is the host
                    allow = true;
                }
            }

            if(allow) {
                Log.Debug(Defines.STEAM_API, "Connection allowed from SteamId " + pCallback.m_steamIDRemote);
                SteamGameServerNetworking.AcceptP2PSessionWithUser(pCallback.m_steamIDRemote);
            } else {
                Log.Warn(Defines.STEAM_API, "Connection denied from unknown SteamId " + pCallback.m_steamIDRemote);
            }
        }

        void OnP2PSessionConnectFail(P2PSessionConnectFail_t pCallback) {
            Log.Debug(Defines.STEAM_API, "OnP2PSessionConnectFail Called steamIDRemote: " + pCallback.m_steamIDRemote);
        }

        internal override void SendReliable(NetPacket packet) {
            if(IsHost) {
                if(ModManager.serverInstance.clients.ContainsKey(ModManager.clientInstance.myPlayerId))
                    ModManager.serverInstance.clients[ModManager.clientInstance.myPlayerId].reliable.onPacket(packet);
            } else {
                reliableSocket?.SendPacket(packet);
            }
        }

        internal override void SendUnreliable(NetPacket packet) {
            if(IsHost) {
                if(ModManager.serverInstance.clients.ContainsKey(ModManager.clientInstance.myPlayerId))
                    ModManager.serverInstance.clients[ModManager.clientInstance.myPlayerId].unreliable.onPacket(packet);
            } else {
                unreliableSocket?.SendPacket(packet);
            }
        }
    }
}
