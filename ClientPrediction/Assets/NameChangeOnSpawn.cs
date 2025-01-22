using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class NameChangeOnSpawn : NetworkBehaviour
{
    // Start is called before the first frame update
    public override void OnNetworkSpawn(){
        
        
        transform.name = "Client Player" + OwnerClientId;
        
    }
}
