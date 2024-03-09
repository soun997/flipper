using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int id;
    public string username;
    public string password;
    public bool isReady;
    public CharacterController controller;
    public float gravity = -9.81f;
    public float moveSpeed = 5f;
    public float jumpSpeed = 5f;

    private bool[] inputs;  // 클라이언트가 보낸 입력을 받을 배열
    private float yVelocity = 0f;

    private void Start()
    {
        gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
        moveSpeed *= Time.fixedDeltaTime;
        jumpSpeed *= Time.fixedDeltaTime;
    }

    public void Initialize(int id, string username, string password)
    {
        this.id = id;
        this.username = username;
        this.password = password;
        this.isReady = false;

        inputs = new bool[5];
    }

    public void FixedUpdate()
    {
        Vector2 inputDirection = Vector2.zero;
        if (inputs[0])
        {
            inputDirection.y += 1;
        }
        if (inputs[1])
        {
            inputDirection.y -= 1;
        }
        if (inputs[2])
        {
            inputDirection.x -= 1;
        }
        if (inputs[3])
        {
            inputDirection.x += 1;
        }
        Move(inputDirection);
    }

    // 클라이언트 움직임 동기화
    private void Move(Vector2 inputDirection)
    {
        Vector3 moveDirection = transform.right * inputDirection.x + transform.forward * inputDirection.y;
        moveDirection *= moveSpeed;

        // 땅에 닿아있을 때에만 점프 가능
        if (controller.isGrounded)
        {
            yVelocity = 0f;
            // 점프
            if (inputs[4])
            {
                yVelocity = jumpSpeed;
            }
        }
        yVelocity += gravity;
        moveDirection.y = yVelocity;

        controller.Move(moveDirection);

        // 플레이어의 position, rotation을 담은 패킷을 보냄 -> 모든 클라이언트끼리 동기화
        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this);
    }

    public void SetInput(bool[] inputs, Quaternion rotation)
    {
        this.inputs = inputs;
        transform.rotation = rotation;
    }
}
