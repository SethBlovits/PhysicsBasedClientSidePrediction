using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Movement;

public class ClientPrediction : NetworkBehaviour
{
    // Start is called before the first frame update
    [SerializeField] float moveSpeed;
    Dictionary<ulong,NetworkObjectReference> clientDict;
    Rigidbody m_rigidBody;
    float serverTickRate;
    float timer = 0;
    float minTimeBetweenTicks;
    int clientTick;
    int lastSentTick;
    int lastReconciledTick;
    Vector3 velocity;
    Vector3 angularVelocity;
    const int CacheSize = 1024;
    SimulationState[] simulationStateCache = new SimulationState[CacheSize];
    InputPacket[] inputPacketCache = new InputPacket[CacheSize];
    SimulationState currentServerState = new SimulationState();
    SimulationState tempSimulationState = new SimulationState();
    [SerializeField]SettingsConfig settingsConfig;
    
    public struct InputPacket : INetworkSerializable{
        public sbyte horizontal;
        public sbyte vertical;
        public bool jump;
        public ushort currentTick;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref horizontal);
            serializer.SerializeValue(ref vertical);
            serializer.SerializeValue(ref jump);
            serializer.SerializeValue(ref currentTick);
        }
    }
    public struct MessagePacket : INetworkSerializable{
        public InputPacket[] inputs;
        public byte numInputs;         
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref inputs);
            serializer.SerializeValue(ref numInputs);
        }
    }
    public struct SimulationState : INetworkSerializable {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public ushort currentTick;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter{
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref velocity);
            serializer.SerializeValue(ref angularVelocity);
            serializer.SerializeValue(ref currentTick);
        }
    }
    void Start()
    {
        
        m_rigidBody = GetComponent<Rigidbody>();
        m_rigidBody.isKinematic = true;
        m_rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        serverTickRate = NetworkManager.Singleton.NetworkTickSystem.TickRate;
       
        minTimeBetweenTicks = 1.0f/serverTickRate;
        //Debug.Log(minTimeBetweenTicks);

    }

    // Update is called once per frame
    void Update()
    {
        if(IsOwner){
            //Debug.Log("IsClient + IsOwner" + m_rigidBody.name);
            //Debug.Log("Running the script for: "+ NetworkManager.Singleton.LocalClientId);
            timer += Time.deltaTime;
            while(timer>=minTimeBetweenTicks){
                timer -= minTimeBetweenTicks;
                int cacheIndex = clientTick%CacheSize;
                inputPacketCache[cacheIndex] = GetCurrentInput();
                
                //Debug.Log("Simulating Movement: "+ clientTick);
                
                simulateMovementTick(inputPacketCache[cacheIndex]);
                simulationStateCache[cacheIndex] = GetSimulationState();
                if(!IsServer){
                    sendClientInputsToServer();
                }
                
                clientTick++;
            }
            Reconciliation();
        }
        else if(!IsServer){
            //Debug.Log("Else " + m_rigidBody.name);
            if(tempSimulationState.currentTick == 0){
                syncObjectsRequestRpc();
                //Debug.Log("The current tick is 0");
                return;
            }
            syncObjectsRequestRpc();
            //Debug.Log("Trying to move " + m_rigidBody.name + " to "+tempSimulationState.position);
            if(m_rigidBody.isKinematic){
                m_rigidBody.isKinematic = false;
            }
            m_rigidBody.position = tempSimulationState.position;
            m_rigidBody.rotation = tempSimulationState.rotation;
            m_rigidBody.velocity = tempSimulationState.velocity;
            m_rigidBody.angularVelocity = tempSimulationState.angularVelocity;
            //m_rigidBody.isKinematic = true;
        }
        
        

    }
    InputPacket GetCurrentInput(){
        InputPacket clientInputPacket = new InputPacket();
        clientInputPacket.horizontal = (sbyte)Input.GetAxisRaw("Horizontal");
        clientInputPacket.vertical = (sbyte)Input.GetAxisRaw("Vertical");
        clientInputPacket.jump = Input.GetKey(settingsConfig.jump);
        clientInputPacket.currentTick = (ushort)clientTick;
        return clientInputPacket;
    }
    SimulationState GetSimulationState(){
        SimulationState simulationState = new SimulationState();
        simulationState.position = m_rigidBody.position;
        simulationState.velocity = m_rigidBody.velocity;
        simulationState.rotation = m_rigidBody.rotation;
        simulationState.angularVelocity = m_rigidBody.angularVelocity;
        simulationState.currentTick = (ushort)clientTick;
        return simulationState;
    }
    void simulateMovementTick(InputPacket inputPacket){
        float horizontal = inputPacket.horizontal;
        float vertical = inputPacket.vertical;
        bool jump = inputPacket.jump;
        Physics.simulationMode = SimulationMode.Script;
        m_rigidBody.isKinematic = false;
        m_rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        //Debug.Log(velocity);
        m_rigidBody.velocity = velocity;
        m_rigidBody.angularVelocity = angularVelocity;
        
        MovementFunctions.addMovementForce(m_rigidBody,horizontal,vertical,moveSpeed);
        MovementFunctions.addJumpForce(m_rigidBody,jump);    
        MovementFunctions.checkHorizontalSpeed(m_rigidBody,moveSpeed);
        MovementFunctions.noInputBehaviour(m_rigidBody,horizontal,vertical);

        Physics.Simulate(minTimeBetweenTicks);
        velocity = m_rigidBody.velocity;
        angularVelocity = m_rigidBody.angularVelocity;
        m_rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        m_rigidBody.isKinematic = true;
        Physics.simulationMode = SimulationMode.FixedUpdate;
    }
    void sendClientInputsToServer(){
        //we need to collect all of the input packets from now and the last sent message to server
        //MessagePacket messagePacket = new MessagePacket();//construct a new message packet. Populate it with the number of ticks passed and all the data
        //Debug.Log("Current Server Tick When Sending client Inputs: " + currentServerState.currentTick);
        //Debug.Log("Client Tick when sending Client Inputs: " + clientTick);
        int numTicks = clientTick - currentServerState.currentTick;
        //Debug.Log(currentServerState.currentTick +" "+ (IsClient && !IsServer));
        //Debug.Log("Number of Ticks" + numTicks);
        //messagePacket.numInputs = (byte)numTicks;
        //messagePacket.inputs = new InputPacket[numTicks];
        int messageIndex = 0;
        InputPacket[] inputPackets = new InputPacket[numTicks];
        for(int i=currentServerState.currentTick;i<clientTick;i++){

            inputPackets[messageIndex] = inputPacketCache[i%CacheSize];
            //messagePacket.inputs[messageIndex] = inputPacketCache[i];
            messageIndex++;  
        }
        //Debug.Log(inputPackets.Length);
        if(inputPackets.Length>0){
            sendClientInputsServerRpc(inputPackets);
        }
        
    }
    [Rpc(SendTo.Server)]
    void sendClientInputsServerRpc(InputPacket[] inputPackets,RpcParams rpcParams = default){
        //byte numInputPackets = messagePacket.numInputs;
        int numInputPackets = inputPackets.Length;
        //Debug.Log(numInputPackets + "Server: " + rpcParams.Receive.SenderClientId);
        //InputPacket[] inputPackets = messagePacket.inputs;
        //simulateClientOnServer(inputPackets);
        //Debug.Log(m_rigidBody.name);
        
        for(int i = 0;i<numInputPackets;i++){
            if(inputPackets[i].currentTick>lastSentTick){
                float horizontal = inputPackets[i].horizontal;
                float vertical = inputPackets[i].vertical;
                int tick = inputPackets[i].currentTick;
                Debug.Log(tick);
                simulateMovementTick(inputPackets[i]);
                SimulationState tempState; //= GetSimulationState();
                tempState.position = m_rigidBody.position;
                tempState.angularVelocity = m_rigidBody.angularVelocity;
                tempState.rotation = m_rigidBody.rotation;
                tempState.velocity = m_rigidBody.velocity;
                tempState.currentTick = (ushort)tick;
                lastSentTick = tick;
                //Debug.Log(tick);
                //Debug.Log(tick + " Server " + rpcParams.Receive.SenderClientId); 
                updateServerSimulationStateClientRpc(tempState,RpcTarget.Single(rpcParams.Receive.SenderClientId,RpcTargetUse.Temp));
                //Rigidbody clientRigidBody = NetworkManager.Singleton.ConnectedClients[rpcParams.Receive.SenderClientId].PlayerObject.GetComponent<Rigidbody>();
                //clientRigidBody.position = tempState.position;
                currentServerState = GetSimulationState();
                //Debug.Log("Temp State: " + tempState.currentTick + "Current Simulation State: " + currentServerState.currentTick);
                ulong objectId = NetworkManager.Singleton.ConnectedClients[rpcParams.Receive.SenderClientId].PlayerObject.NetworkObjectId;
            }
            //updateRigidBodiesOnOtherClientsRpc(tempState,objectId,RpcTarget.Not(rpcParams.Receive.SenderClientId,RpcTargetUse.Temp));
        }
    }
    [Rpc(SendTo.Server)]
    void syncObjectsRequestRpc(RpcParams rpcParams = default){
        SimulationState simulationState = GetSimulationState();
        syncResponseClientRpc(simulationState,RpcTarget.Single(rpcParams.Receive.SenderClientId,RpcTargetUse.Temp));
    }
    [Rpc(SendTo.SpecifiedInParams)]
    void syncResponseClientRpc(SimulationState simulationState,RpcParams rpcParams){
        tempSimulationState = simulationState;
    }
    /*
    [Rpc(SendTo.SpecifiedInParams)]
    void updateRigidBodiesOnOtherClientsRpc(SimulationState simulationState,ulong objectId,RpcParams rpcParams){
        if(IsServer)return;
        NetworkObject networkObject = GetNetworkObject(objectId);
        
        //Debug.Log("Trying to move" + networkObject.name);
        //Debug.Log("Trying to move " + networkObject.name + " to: " + simulationState.position + " " + networkObject.IsOwner);
        //updatePositionByOwnerRpc(simulationState,objectId,RpcTarget.Single(networkObject.OwnerClientId,RpcTargetUse.Temp));
        Rigidbody updateBody = networkObject.GetComponent<Rigidbody>();
        //Debug.Log(updateBody.name);
        updateBody.isKinematic = false;
        updateBody.position = simulationState.position;
        updateBody.rotation = simulationState.rotation;
        updateBody.velocity = simulationState.velocity;
        updateBody.angularVelocity = simulationState.angularVelocity;
        updateBody.isKinematic = true;
    }*/
    [Rpc(SendTo.SpecifiedInParams)]
    void updateServerSimulationStateClientRpc(SimulationState simulationState,RpcParams rpcParams){
        currentServerState = simulationState;
        //Debug.Log(simulationState.currentTick + "Client");
        
    }
    void Reconciliation(){
        //get the current tick of the
        if(lastReconciledTick >= currentServerState.currentTick){
            return;
        } 
        int cacheIndex = currentServerState.currentTick % CacheSize;
        //int cacheIndex = tick % CacheSize;
        //Debug.Log(cacheIndex);
        //Debug.Log("Reconciling Tick "+currentServerState.currentTick);
        InputPacket tickInputPacket = inputPacketCache[cacheIndex];
        SimulationState tickSimulationState = simulationStateCache[cacheIndex];
        
        float positionError = Vector3.Distance(currentServerState.position,tickSimulationState.position);
        float velocityError = Vector3.Distance(currentServerState.velocity,tickSimulationState.velocity);
        float rotationError = 1.0f - Mathf.Abs(Quaternion.Dot(currentServerState.rotation,tickSimulationState.rotation));
        Debug.Log("Velocity Error: "  + velocityError);
        if(positionError > 0.00001f){ //|| rotationError > 0.0001f
            Debug.Log("Position Error: " + positionError + "on tick " + currentServerState.currentTick);
            Debug.Log("ServerPosition: " + currentServerState.position + "tickSimulationState: " + tickSimulationState.position);

            //Debug.Log("Server and Client Position are different");
            m_rigidBody.isKinematic = false;
            //m_rigidBody.position = Vector3.Lerp(currentServerState.position,m_rigidBody.position,0.99f);//gonna need to lerp quaternions as well soon
            m_rigidBody.position = currentServerState.position;//gonna need to lerp quaternions as well soon
            m_rigidBody.rotation = currentServerState.rotation;
            m_rigidBody.velocity = currentServerState.velocity;
            m_rigidBody.angularVelocity = currentServerState.angularVelocity;
            m_rigidBody.isKinematic = true;

            int correctionTick = currentServerState.currentTick;
            lastReconciledTick = correctionTick;
            for(int i = correctionTick;i<clientTick;i++){
                //get the cached inputs
                int resimIndex = i%CacheSize;;
         
                InputPacket resimulatedPacket = inputPacketCache[resimIndex];
                simulationStateCache[resimIndex] = GetSimulationState();
                simulationStateCache[resimIndex].currentTick = (ushort)i;
                simulateMovementTick(resimulatedPacket);
            }
            

        }
        
    }
    /*
    void OnConnectedClientCallback(ulong clientID){//need to finish to allow server to send the list of rigidbodies to the client
        Debug.Log("Connected Client");
        if(IsServer){
            NetworkObjectReference networkObject = NetworkManager.Singleton.ConnectedClients[clientID].PlayerObject;
            clientDict.Add(clientID,networkObject);
            clientGameObjectRpc(clientDict);
        }
    }
    [Rpc(SendTo.NotServer)]
    void clientGameObjectRpc(Dictionary<ulong,NetworkObjectReference> newClientDict){
        clientDict = newClientDict;
    }*/
    
     
}
