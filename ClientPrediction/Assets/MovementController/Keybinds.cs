
using UnityEngine;
public class Keybinds
{
    public KeyCode forward {get;set;}
    public KeyCode left {get;set;}
    public KeyCode right {get;set;}
    public KeyCode back {get;set;}
    public KeyCode jump {get;set;}
    public KeyCode sprint {get;set;}
    public KeyCode crouch {get;set;}
    public float vert_sens{get;set;}
    public float horiz_sens{get;set;}
    public Keybinds(){
        forward = KeyCode.W;
        left = KeyCode.A;
        right = KeyCode.D;
        back = KeyCode.S;
        jump = KeyCode.Space;
        sprint = KeyCode.LeftShift;
        crouch = KeyCode.LeftControl;
        horiz_sens = 1f;
        vert_sens = 1f;
    }

        
}
