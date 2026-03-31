using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    Rigidbody2D rb;
    float InputX;
    bool IsJumpPressed;
    Vector2 MoveDir;
    //速度
    [SerializeField] float groundspeed = 5f;
    [SerializeField] float airspeed = 3f;
    [SerializeField] float airacc = 20f;
    [SerializeField] float jumpPower = 5f;
    [SerializeField] float MaxSlopeAngle = 45.0f;
    [SerializeField] float jumpBufferTime = 0.1f;
    [SerializeField] float stickToGroundForce = 5f;
    float jumpBufferTimer;

    [SerializeField] float coyoteTime = 0.1f;
    float coyoteTimer;



    [SerializeField] bool IsWalkableground;
    [SerializeField] bool IsDontWalkableSlope;
    Vector2 groundNomal = Vector2.up;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    void Update()
    {
        InputX = 0.0f;

        if (Keyboard.current.aKey.isPressed) InputX -= 1;
        if (Keyboard.current.dKey.isPressed) InputX += 1;

        //ジャンプ入力のバッファリング
        if ((Keyboard.current.spaceKey.isPressed))
        {
            IsJumpPressed = true;
            jumpBufferTimer = jumpBufferTime;
        }
    }
    void FixedUpdate()
    {


        Vector2 vel = rb.linearVelocity;
        //======================================================
        //コヨーテタイムの処理
        if (/*IsWalkableground*/true)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer = Mathf.Max(0.0f, coyoteTimer - Time.fixedDeltaTime);
        }
        //======================================================

        //======================================================
        //ジャンプバッファの処理
        if (jumpBufferTimer>0.0f)
        {
            jumpBufferTimer -= Time.fixedDeltaTime;
        }
        //======================================================

        //======================================================
        //移動の処理
        if (IsWalkableground)
        {
            Vector2 tangent = new Vector2(groundNomal.y, -groundNomal.x).normalized;
            if (Mathf.Sign(InputX) != Mathf.Sign(tangent.x))
            {
                tangent = -tangent;
            }

            Vector2 moveVel = tangent * (Mathf.Abs(InputX) * groundspeed);
            vel.x = moveVel.x;
            vel.y = moveVel.y;

            vel += -groundNomal * stickToGroundForce;
        }
        else
        {
            float targetX = InputX * airspeed;

            vel.x = Mathf.MoveTowards(vel.x, targetX, airacc * Time.fixedDeltaTime);

            if (IsDontWalkableSlope && Mathf.Abs(InputX) > 0.01f)
            {
                vel.x = Mathf.MoveTowards(vel.x, 0.0f, airacc * Time.fixedDeltaTime);
            }
        }
        //======================================================

        // ジャンプ
        if (jumpBufferTimer>0.0f && coyoteTimer > 0.0f)
        {
            vel.y = jumpPower;
            coyoteTimer = 0.0f;
            jumpBufferTimer = 0.0f;
        }


        rb.linearVelocity = vel;

        IsJumpPressed = false;



        IsWalkableground = false;
        IsDontWalkableSlope = false;

    }

    void OnCollisionStay2D(Collision2D collision)
    {
        CheckGround(collision);
    }

    void CheckGround(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer.value) == 0.0f)
        {
            return;
        }

        float StableNormalY = -1f;
        bool foumdwalkable = false;
        foreach (var contact in collision.contacts)
        {
            float angle = Vector2.Angle(contact.normal, Vector2.up);

            if (angle <= MaxSlopeAngle)
            {
                foumdwalkable = true;

                if (contact.normal.y > StableNormalY)
                {
                    StableNormalY = contact.normal.y;
                    groundNomal = contact.normal;
                }

            }
            else if (contact.normal.y > 0f)
            {
                IsDontWalkableSlope = true;

            }

        }
        if (foumdwalkable)
        {
            IsWalkableground = true;
        }
    }

    [SerializeField] LayerMask groundLayer;
}
