using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerNS
{
    public class Player : NetworkBehaviour
    {

        public NetworkVariable<int> choosedColor = new NetworkVariable<int>();
        public NetworkVariable<int> team = new NetworkVariable<int>();

        public List<Material> materials = new List<Material>();
                
        private float movingDistance = 0.5f;
        private bool canMove = true;

        private int[,] teamsMaterialsIndex;

        private PlayerManager playerManager;

        // ======================================================================================================================= client methods

        void Awake() {
            playerManager = GameObject.Find("PlayerManager").GetComponent<PlayerManager>();
            teamsMaterialsIndex = new int[,] {{0, 0}, {1, 3}, {4, 6}};

        }

        void Start() {
            if (IsOwner) {
                SubmitInitialPositionRequestServerRpc();
            }
        }

        void Update()
        {
            if (IsOwner) {
                if (canMove && (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)))   SubmitPositionServerRpc(- movingDistance, 0);
                if (canMove && (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)))  SubmitPositionServerRpc(movingDistance, 0);
                if (canMove && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)))     SubmitPositionServerRpc(0, movingDistance);
                if (canMove && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)))   SubmitPositionServerRpc(0, - movingDistance);

                if (Input.GetKeyDown(KeyCode.M)) MoveToOrigin();
            }

            GetComponent<MeshRenderer>().material = materials[choosedColor.Value];
        }

        public override void OnNetworkDespawn() {
            //TODO
            // Notificar ao server para eliminar o xogador do equipo, se o ten.
        }

        public void MoveToOrigin() {
            if (canMove) {
                SubmitInitialPositionRequestServerRpc();
            } else {
                Debug.Log("Non se pode mover");
            }
        }

        // ======================================================================================================================= ClientRPC

        [ClientRpc]
        public void CanMoveClientRpc(bool move, ClientRpcParams clientRpcParams = default) {
            canMove = move;
        }

        // ======================================================================================================================= ServerRPC

        [ServerRpc]
        void SubmitSetDefaultValuesServerRpc() {
            team.Value = 0;
            SubmitChangeColorServerRpc(team.Value);
        }

        [ServerRpc]
        void SubmitInitialPositionRequestServerRpc() {
            transform.position = new Vector3(Random.Range(playerManager.noTeamLimitLeft, playerManager.noTeamLimitRight), 1f, Random.Range(playerManager.GameBoardLimitLeft, playerManager.GameBoardLimitRight));
        }

        [ServerRpc]
        void SubmitPositionServerRpc(float moveLeftRight, float moveBackForward, ServerRpcParams serverRpcParams = default){

            Vector3 newPosition = new Vector3(transform.position.x + moveLeftRight, transform.position.y, transform.position.z + moveBackForward);

            if (newPosition.x < playerManager.GameBoardLimitRight && newPosition.x > playerManager.GameBoardLimitLeft
              && newPosition.z < playerManager.GameBoardLimitForward && newPosition.z > playerManager.GameBoardLimitBackward ){

            var clientId = serverRpcParams.Receive.SenderClientId;

                if (newPosition.x <= playerManager.noTeamLimitLeft) {

                    if (team.Value == 1)  {

                        transform.position = newPosition;
                    } else if (playerManager.membersTeam1.Count < playerManager.maxPlayerPerTeam.Value)  {
                        transform.position = newPosition;
                        team.Value = 1;
                        playerManager.AddMember(team.Value, clientId);
                        SubmitChangeColorServerRpc(team.Value);
                    }
                } else if (newPosition.x >= playerManager.noTeamLimitRight) {

                    if (team.Value == 2)  {
                        transform.position = newPosition;
                    } else if (playerManager.membersTeam2.Count < playerManager.maxPlayerPerTeam.Value)  {
                        
                        transform.position = newPosition;
                        team.Value = 2;
                        playerManager.AddMember(team.Value, clientId);
                        SubmitChangeColorServerRpc(team.Value);
                    }
                } else if (newPosition.x >= playerManager.noTeamLimitLeft && newPosition.x <= playerManager.noTeamLimitRight) {
                    
                    transform.position = newPosition;
                    if (team.Value > 0) {
                        
                        playerManager.RemoveMember(team.Value, clientId);
                        team.Value = 0;
                        SubmitChangeColorServerRpc(team.Value);
                    }
                }
              }
        } 

        
        [ServerRpc]
        void SubmitChangeColorServerRpc(int team){

            choosedColor.Value = Random.Range(teamsMaterialsIndex[team,0], teamsMaterialsIndex[team,1]);

        }
        
        
    }
}