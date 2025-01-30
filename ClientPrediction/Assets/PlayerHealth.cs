using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerHealth : NetworkBehaviour
{
    // Start is called before the first frame update
    public NetworkVariable<int> playerHealth = new NetworkVariable<int>(100,NetworkVariableReadPermission.Owner,NetworkVariableWritePermission.Server);

    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       
    }
}
