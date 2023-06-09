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
        public NetworkVariable<bool> canMove = new NetworkVariable<bool>();
        public NetworkVariable<ulong> clientId = new NetworkVariable<ulong>();

        public List<Material> materials = new List<Material>();
                
        private float movingDistance = 0.5f;

        private int[,] teamsMaterialsIndex;

        private PlayerManager playerManager;

        // ======================================================================================================================= client methods

        void Awake() {
            playerManager = GameObject.Find("PlayerManager").GetComponent<PlayerManager>();
            teamsMaterialsIndex = new int[,] {{0, 0}, {1, 3}, {4, 6}};

        }

        public override void OnNetworkSpawn() {
            
            if (IsOwner) {
                base.OnNetworkSpawn();
                team.OnValueChanged += ChangedTeam;
            }
        }
        
        void Start() {
            if (IsOwner) {
                SubmitSetInitialDataServerRpc();
            }
        }

        void Update()
        {
            if (IsOwner) {
            
                if (canMove.Value && (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)))   SubmitPositionServerRpc(- movingDistance, 0);
                if (canMove.Value && (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)))  SubmitPositionServerRpc(movingDistance, 0);
                if (canMove.Value && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)))     SubmitPositionServerRpc(0, movingDistance);
                if (canMove.Value && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)))   SubmitPositionServerRpc(0, - movingDistance);

                if (Input.GetKeyDown(KeyCode.M)) MoveToOrigin();
            }

            GetComponent<MeshRenderer>().material = materials[choosedColor.Value];
        }

        public override void OnNetworkDespawn() {
            playerManager.RemoveMember(team.Value, clientId.Value);
        }


        private void ChangedTeam(int oldTeam, int newTeam) {
            ChangedTeamServerRpc(oldTeam, newTeam);
        }

        public void MoveToOrigin() {

            if (canMove.Value) {
                SubmitSetInitialDataServerRpc();
            }

        }

        // ======================================================================================================================= ClientRPC


        [ClientRpc]
        public void CanMoveClientRpc(bool move, ClientRpcParams clientRpcParams = default) {

            SubmitChangeMoveLockServerRpc(move);

        }

        // ======================================================================================================================= ServerRPC

        [ServerRpc]
        void SubmitSetInitialDataServerRpc(ServerRpcParams serverRpcParams = default) {
            
            transform.position = new Vector3(Random.Range(playerManager.noTeamLimitLeft, playerManager.noTeamLimitRight), 1f, Random.Range(playerManager.GameBoardLimitLeft, playerManager.GameBoardLimitRight));            
            clientId.Value = serverRpcParams.Receive.SenderClientId;
            team.Value = 0;

            canMove.Value = (playerManager.membersTeam1.Count < playerManager.maxPlayerPerTeam.Value && playerManager.membersTeam2.Count < playerManager.maxPlayerPerTeam.Value);

            Rigidbody rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeAll;

        }

        [ServerRpc]
        void SubmitPositionServerRpc(float moveLeftRight, float moveBackForward, ServerRpcParams serverRpcParams = default){

            // Permitimos o movemento en horizontal (eixo X e Y)
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

            // Calculamos a nova posicion
            Vector3 newPosition = new Vector3(transform.position.x + moveLeftRight, transform.position.y, transform.position.z + moveBackForward);

            // Comprobamos se esta dentro dos limites do taboleiro
            if (newPosition.x < playerManager.GameBoardLimitRight && newPosition.x > playerManager.GameBoardLimitLeft
              && newPosition.z < playerManager.GameBoardLimitForward && newPosition.z > playerManager.GameBoardLimitBackward ){

                if (newPosition.x <= playerManager.noTeamLimitLeft) {

                    // Quere moverse ao equipo 1.
                    // Comprobamos se xa esta no equipo ou se pode incorporarse

                    if (team.Value == 1)  {

                        transform.position = newPosition;
                    } else if (playerManager.membersTeam1.Count < playerManager.maxPlayerPerTeam.Value)  {

                        transform.position = newPosition;
                        team.Value = 1;
                    }
                } else if (newPosition.x >= playerManager.noTeamLimitRight) {

                    // Quere moverse ao equipo 2.
                    // Comprobamos se xa esta no equipo ou se pode incorporarse

                    if (team.Value == 2)  {
                        transform.position = newPosition;
                    } else if (playerManager.membersTeam2.Count < playerManager.maxPlayerPerTeam.Value)  {
                        
                        transform.position = newPosition;
                        team.Value = 2;
                    }
                } else if (newPosition.x >= playerManager.noTeamLimitLeft && newPosition.x <= playerManager.noTeamLimitRight) {

                    // Comprobamos se a nova posicion esta na zona centrao (sen equipo)
                    
                    transform.position = newPosition;
                    if (team.Value > 0) {
                        team.Value = 0;
                    }
                }
            }                

            // bloqueamos todos os movementos e rotacions
            rb.constraints = RigidbodyConstraints.FreezeAll;
        } 

        
        [ServerRpc]
        void SubmitChangeColorServerRpc(int team){
            // Random.Range entre enteiros non ten en conta o limite superior;
            // por iso sumamos 1 a teamsMaterialsIndex[team,1]
            choosedColor.Value = Random.Range(teamsMaterialsIndex[team,0], teamsMaterialsIndex[team,1] + 1);

        }

        [ServerRpc]
        void ChangedTeamServerRpc(int oldTeam, int newTeam) {

            if (oldTeam > 0) playerManager.RemoveMember(oldTeam, clientId.Value);

            if (newTeam > 0) playerManager.AddMember(newTeam, clientId.Value);

            SubmitChangeColorServerRpc(newTeam);

        }

        [ServerRpc]
        void SubmitChangeMoveLockServerRpc(bool move) {
            canMove.Value = move;
        }
        
    }
}