﻿using AMP.Data;
using AMP.DiscordNetworking;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Handler;
using AMP.Network.Packets;
using AMP.Network.Packets.Implementation;
using AMP.Overlay;
using AMP.SupportFunctions;
using Discord;
using Steamworks;
using System;
using System.Collections.Generic;

namespace AMP.Steam {
    internal class SteamNetworking : NetworkHandler {

        internal enum Mode {
            NONE,
            CLIENT,
            SERVER
        }

        public Mode mode = Mode.NONE;
        internal CSteamID? currentLobby;

        private Callback<LobbyCreated_t> _LobbyCreatedCallback;
        private Callback<LobbyEnter_t> _LobbyEnterCallback;
        private Callback<LobbyDataUpdate_t> _LobbyDataUpdateCallback;
        private Callback<LobbyChatUpdate_t> _LobbyChatUpdateCallback;
        private Callback<P2PSessionRequest_t> _p2PSessionRequestCallback;

        public SteamNetworking() {
            try {
                if(SteamManager.Initialized) {
                    _LobbyCreatedCallback      = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
                    _LobbyEnterCallback        = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
                    _LobbyDataUpdateCallback   = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
                    _LobbyChatUpdateCallback   = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
                    _p2PSessionRequestCallback = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);

                } else {
                    Log.Err("AMP", $"Couldn't initialize Steam, so no Steam Networking is available.");
                }
            } catch(Exception e) {
                Log.Err("AMP", $"Couldn't initialize Steam, so no Steam Networking is available:\n{e}");
            }
        }

        private void OnLobbyChatUpdate(LobbyChatUpdate_t data) {
            switch(data.m_rgfChatMemberStateChange) {
                case (uint) EChatMemberStateChange.k_EChatMemberStateChangeEntered:
                    Log.Debug("Join " + data.m_ulSteamIDUserChanged);
                    break;
                case (uint) EChatMemberStateChange.k_EChatMemberStateChangeLeft:
                case (uint) EChatMemberStateChange.k_EChatMemberStateChangeDisconnected:
                    Log.Debug("Leave " + data.m_ulSteamIDUserChanged);
                    break;
                default:
                    Log.Debug(data.m_rgfChatMemberStateChange);
                    break;
            }
        }


        internal Dictionary<ulong, string> userList = new Dictionary<ulong, string>();
        private void OnLobbyDataUpdate(LobbyDataUpdate_t data) {
            if(data.m_ulSteamIDMember != data.m_ulSteamIDLobby) {
                if(!userList.ContainsKey(data.m_ulSteamIDMember)) {
                    userList.Add(data.m_ulSteamIDMember, SteamFriends.GetFriendPersonaName((CSteamID) data.m_ulSteamIDMember));

                    Log.Debug("Join " + userList[data.m_ulSteamIDMember]);
                }
            }
        }

        internal void CreateLobby(int maxPlayers) {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);
        }

        internal void JoinLobby(ulong lobbyId) {
            SteamMatchmaking.JoinLobby((CSteamID) lobbyId);
        }

        private void OnLobbyCreated(LobbyCreated_t data) {
            if(data.m_eResult == EResult.k_EResultOK) {
                Log.Debug(Defines.SERVER, $"Lobby \"{data.m_ulSteamIDLobby}\" created.");

                string personalName = SteamFriends.GetPersonaName();
                SteamMatchmaking.SetLobbyData((CSteamID) data.m_ulSteamIDLobby, "name", personalName + "'s game");

                mode = Mode.SERVER;
            } else {
                Log.Err("AMP", $"Couldn't create lobby: " + data.m_eResult);
            }
        }

        private void OnLobbyEnter(LobbyEnter_t data) {
            Log.Debug(Defines.SERVER, $"Lobby \"{data.m_ulSteamIDLobby}\" entered.");

            currentLobby = (CSteamID) data.m_ulSteamIDLobby;
            isConnected = true;

            if(mode == Mode.SERVER) {
                ModManager.HostServer(4, 0);
            }
            ModManager.JoinServer(this);
        }

        private void OnP2PSessionRequest(P2PSessionRequest_t data) {
            CSteamID clientId = data.m_steamIDRemote;
            if(ExpectingClient(clientId)) {
                Steamworks.SteamNetworking.AcceptP2PSessionWithUser(clientId);
            } else {
                Log.Debug("Unexpected session request from " + clientId);
            }
        }

        private bool ExpectingClient(CSteamID clientId) {
            if(mode == Mode.SERVER) {

            } else if(mode == Mode.CLIENT) {
                
            }
            return false;
        }

        internal override void Connect() {
            //if(mode == Mode.SERVER) {
            //
            //} else {
            //    new EstablishConnectionPacket(UserData.GetUserName(), Defines.MOD_VERSION).SendToServerReliable();
            //}
            Log.Debug("EstablishConnectionPacket");
            new EstablishConnectionPacket(UserData.GetUserName(), Defines.MOD_VERSION).SendToServerReliable();
        }

        internal override void Disconnect() {
            _LobbyCreatedCallback.Unregister();
            _LobbyEnterCallback.Unregister();
            _LobbyDataUpdateCallback.Unregister();
            _p2PSessionRequestCallback.Unregister();
            _LobbyChatUpdateCallback.Unregister();

            mode = Mode.NONE;
            currentLobby = null;
            isConnected = false;
            userList.Clear();
        }

        internal override void RunCallbacks() {
            
        }

        internal override void SendReliable(NetPacket packet) {
            base.SendReliable(packet);
            byte[] data = packet.GetData();
            Steamworks.SteamNetworking.SendP2PPacket(SteamUser.GetSteamID(), data, (uint) data.Length, EP2PSend.k_EP2PSendReliable);
        }

        internal override void SendUnreliable(NetPacket packet) {
            base.SendUnreliable(packet);
            byte[] data = packet.GetData();
            Steamworks.SteamNetworking.SendP2PPacket(SteamUser.GetSteamID(), data, (uint)data.Length, EP2PSend.k_EP2PSendUnreliableNoDelay);
        }
    }
}
