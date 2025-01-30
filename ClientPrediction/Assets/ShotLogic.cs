using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class ShotLogic : NetworkBehaviour
{
    // Start is called before the first frame update
    [SerializeField] Camera cam;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner){
            return;
        }
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f,0.5f,0));
        Debug.DrawRay(ray.origin,ray.direction*100,Color.red);
        if(Input.GetKey(KeyCode.Mouse0)){
            RaycastHit hit;
            if(Physics.Raycast(ray,out hit,Mathf.Infinity)){
                Debug.Log(hit.collider.name);
                if(IsServer){
                    hit.collider.gameObject.GetComponent<PlayerHealth>().playerHealth.Value -= 1;
                }
                else{
                    updatePlayerHealthServerRpc(hit.collider.gameObject.GetComponent<NetworkObject>().OwnerClientId);
                }
                
            }
            
        }
    }
    [Rpc(SendTo.Server)]
    void updatePlayerHealthServerRpc(ulong hitPlayerId){
        //oh my god this is so long there has to be a better way also this isn't updating the host player health on the client
        NetworkManager.Singleton.ConnectedClients[hitPlayerId].PlayerObject.gameObject.GetComponent<PlayerHealth>().playerHealth.Value -= 1;
    }
}
