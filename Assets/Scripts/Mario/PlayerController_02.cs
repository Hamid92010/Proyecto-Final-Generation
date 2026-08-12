using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Si el objeto no tiene Rigid lo generamos
[RequireComponent(typeof(Rigidbody))]

public class PlayerController_02 : MonoBehaviour
{
 
    [Header ("Movimiento")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public float gravityForce = -20f;


    [Header("Chequeo de suelo")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 moveInput;
    private bool jumpPressed;



    void Start(){

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }
    
    void Update()
    {

        float moveX = 0f;
        float moveZ = 0F;
        //float moveY = 0F;

        if (Keyboard.current.dKey.isPressed){
            moveX -= 1f;
        }
        if (Keyboard.current.aKey.isPressed){
            moveX += 1f;
        }
        if (Keyboard.current.sKey.isPressed){
            moveZ -= 1f;
        }
        if (Keyboard.current.wKey.isPressed){
            moveZ += 1f;
        }

        moveInput = new Vector3(moveX, 0F, moveZ);

        //Verificar que el objeto esta en el suelo
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        Debug.Log("isGrounded: " + isGrounded);

        // Generar Salto
        if(isGrounded && Keyboard.current.spaceKey.wasPressedThisFrame){
            jumpPressed = true;
        }

        if (jumpPressed){
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            jumpPressed = false;
        }

        //Interactur con objetos del escenario
        if (Keyboard.current.eKey.wasPressedThisFrame){
            interact();
        }
    }

    void FixedUpdate(){
        Vector3 movement = moveInput * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);

        if (isGrounded && rb.linearVelocity.y < 0)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -2f, rb.linearVelocity.z);
        }
        else
        {
            rb.linearVelocity += Vector3.up * gravityForce * Time.fixedDeltaTime;
        }
    }

    
    void interact(){

        Debug.Log("Interactuando...");

    }
}
