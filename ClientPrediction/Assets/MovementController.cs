using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MovementController : NetworkBehaviour
{
    // Start is called before the first frame update
    Rigidbody m_Rigidbody;
    public float ServerTickRate = 64f;
    [SerializeField] float moveSpeed;
    Vector3 moveVector;
    float minTimeBetweenTicks;
    float timer;
    ushort clientStepTick = 0;
    const int StateCacheSize = 1024;
    float horizontalInput;
    float verticalInput;
    SimulationState[] simulationStateCache = new SimulationState[StateCacheSize];
    ClientInputState[] inputStateCache = new ClientInputState[StateCacheSize];
    SimulationState serverSimulationState = new SimulationState();
    ClientInputState lastReceivedInputs = new ClientInputState();
    public struct SimulationState{
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public ushort currentTick;
    }
    public struct ClientInputState{
        public sbyte horizontal;
        public sbyte vertical;
        public ushort currentTick;
    }
    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        moveVector = Vector3.zero;
        minTimeBetweenTicks = 1f/ServerTickRate;
        timer = 0;
    }

    // Update is called once per frame
    void Update() 
    {
        if(!IsOwner){return;}

        timer += Time.deltaTime;
        while(timer>= minTimeBetweenTicks){
            timer -= minTimeBetweenTicks;
            int currentCacheIndex = clientStepTick % StateCacheSize;
            inputStateCache[currentCacheIndex] = getClientInputs();
            SendMessageToServer();
            clientStepTick++;//should be the last line
            
        }
        
        /*if(!IsOwner){
            return;
        }
        moveVector = checkMovement();*/
    }

    struct InputPacket : INetworkSerializable{
        public sbyte horizontal;
        public sbyte vertical;
        public ushort currentTick;
                
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref horizontal);
            serializer.SerializeValue(ref vertical);
            serializer.SerializeValue(ref currentTick);
        }

    }
    struct MessagePacket : INetworkSerializable{
        
        public InputPacket[] inputs;
        public byte numInputs;         
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref inputs);
            serializer.SerializeValue(ref numInputs);
        }

    } 
    void SendMessageToServer(){
        
        //In the github, they packed all the redudndant values into one message using riptide networking.
        //In our case we could do the same thing by packing all the C# primitives into one array and sending it 
        //over
        MessagePacket message;
        message.numInputs = (byte)(clientStepTick - serverSimulationState.currentTick);//at this line we need the current time that the server simulation is at
        message.inputs = new InputPacket[message.numInputs];
        int currentMessageIndex=0;
        for(int i = serverSimulationState.currentTick;i<clientStepTick;i++){
            InputPacket inputPacket;
            inputPacket.horizontal = inputStateCache[i].horizontal;
            inputPacket.vertical = inputStateCache[i].vertical;
            inputPacket.currentTick = inputStateCache[i].currentTick;
            message.inputs[currentMessageIndex] = inputPacket;
            currentMessageIndex++;
        }
        sendClientInputRpc(message); 
    }
    ClientInputState getClientInputs(){
        ClientInputState inputs;
        inputs.horizontal = (sbyte)Input.GetAxisRaw("Horizontal");
        inputs.vertical = (sbyte)Input.GetAxisRaw("Vertical");
        inputs.currentTick = clientStepTick;
        return inputs;
    }
    [Rpc(SendTo.Server)]
    void sendClientInputRpc(MessagePacket messagePacket){
        byte numInputs = messagePacket.numInputs;
        Debug.Log(numInputs);
        ClientInputState[] inputs = new ClientInputState[numInputs];
        for(int i = 0;i<numInputs;i++){
            ClientInputState serverClientInputState;
            serverClientInputState.horizontal = messagePacket.inputs[i].horizontal;
            serverClientInputState.vertical = messagePacket.inputs[i].vertical;
            serverClientInputState.currentTick = messagePacket.inputs[i].currentTick;
        }
        //after this the server has to process the movement inputs, simulate them and then send it back to the client
    }
    void currentInputs(float horizontal,float vertical){
        horizontalInput = horizontal;
        verticalInput = vertical;
        TickSimulation();
    }
    void TickSimulation(){

    }
    void handleClientInputs(ClientInputState[] inputs){
        if(!IsServer && inputs.Length==0) return;
        int currentTickIndex = inputs.Length-1;
        if(inputs[currentTickIndex].currentTick>=lastReceivedInputs.currentTick){
            int startIndex=0;
            if(lastReceivedInputs.currentTick>inputs[0].currentTick){
                startIndex = lastReceivedInputs.currentTick-inputs[0].currentTick;
            }
            for(int i=0;i<currentTickIndex;i++){
                currentInputs(inputs[i].vertical,inputs[i].horizontal);


            }
            lastReceivedInputs = inputs[currentTickIndex];
        }
        //we parse the values from the inputs. simulate the physics with the inputs
        //then send the simulated movement back to the client
    }
}
