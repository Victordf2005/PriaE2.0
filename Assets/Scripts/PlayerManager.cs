using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PlayerNS
{
    public class PlayerManager : NetworkBehaviour
    {

        public NetworkList<ulong> membersTeam1;
        public NetworkList<ulong> membersTeam2;

        public NetworkVariable<int> maxPlayerPerTeam = new NetworkVariable<int>();
        
        private ClientRpcParams clientRpcParams;
        
        private Player p;

        void Awake() {
            membersTeam1 = new NetworkList<ulong>();
            membersTeam2 = new NetworkList<ulong>();
            maxPlayerPerTeam.Value = 2;
        }
        

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();

                if (IsHost || IsClient) SubmitMoveToOrigin();

            }
            GUILayout.EndArea();
        }

        static void StartButtons()
        {
            if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
            if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
        }

        static void StatusLabels()
        {
            var mode = NetworkManager.Singleton.IsHost ?
                "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

            GUILayout.Label("Transport: " +
                NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
            GUILayout.Label("Mode: " + mode);
        }

        static void SubmitMoveToOrigin()
        {
            if (GUILayout.Button("Move to origin"))
            {
                var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                var player = playerObject.GetComponent<Player>();
                player.MoveToOrigin();
            }
        }

        public void AddMember(int team, ulong clientId) {

            if (team == 1) {
                membersTeam1.Add(clientId);
                if (membersTeam1.Count >= maxPlayerPerTeam.Value) {
                    foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds) {
                        if ( ! membersTeam1.Contains(id)){
                            clientRpcParams = new ClientRpcParams {
                                    Send = new ClientRpcSendParams {
                                        TargetClientIds = new ulong[]{id}
                                    }
                                };
                                // chamada a clientRpc
                            p = NetworkManager.Singleton.ConnectedClientsList[(int) id].PlayerObject.GetComponent<Player>();
                            p.CanMoveClientRpc(false, clientRpcParams);
                        }
                    }
                }
            } else if (team == 2) {
                membersTeam2.Add(clientId);
                if (membersTeam2.Count >= maxPlayerPerTeam.Value) {
                    foreach(ulong id in NetworkManager.Singleton.ConnectedClientsIds) {
                        if ( ! membersTeam2.Contains(id)){
                            clientRpcParams = new ClientRpcParams {
                                    Send = new ClientRpcSendParams {
                                        TargetClientIds = new ulong[]{id}
                                    }
                                };
                                // chamada a clientRpc
                            p = NetworkManager.Singleton.ConnectedClientsList[(int) id].PlayerObject.GetComponent<Player>();
                            p.CanMoveClientRpc(false, clientRpcParams);
                        }
                    }
                }
            }



Debug.Log(">>>> Membros: " + membersTeam1.Count + " - " + membersTeam2.Count);
        }

        public void RemoveMember(int team, ulong clientId) {
            if (team == 1) {
                membersTeam1.Remove(clientId);                
                if (membersTeam1.Count < maxPlayerPerTeam.Value) { 
                    // Desbloqueamos todos
                    foreach(ulong id in NetworkManager.Singleton.ConnectedClientsIds) {
                            clientRpcParams = new ClientRpcParams {
                                    Send = new ClientRpcSendParams {
                                        TargetClientIds = new ulong[]{id}
                                    }
                                };
                                // chamada a clientRpc
                            p = NetworkManager.Singleton.ConnectedClientsList[(int) id].PlayerObject.GetComponent<Player>();
                            p.CanMoveClientRpc(true, clientRpcParams);
                    }
                }

            } else if (team == 2) {
                membersTeam2.Remove(clientId);
                if (membersTeam2.Count < maxPlayerPerTeam.Value) {  
                    // Desbloqueamos todos 
                    foreach(ulong id in NetworkManager.Singleton.ConnectedClientsIds) {
                            clientRpcParams = new ClientRpcParams {
                                    Send = new ClientRpcSendParams {
                                        TargetClientIds = new ulong[]{id}
                                    }
                                };
                                // chamada a clientRpc
                            p = NetworkManager.Singleton.ConnectedClientsList[(int) id].PlayerObject.GetComponent<Player>();
                            p.CanMoveClientRpc(true, clientRpcParams);
                    }
                }
            }
Debug.Log(">>>> Membros: " + membersTeam1.Count + " - " + membersTeam2.Count);
        }
    }
}