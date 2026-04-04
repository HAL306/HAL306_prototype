using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの移動を行うコンポーネント
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    [Header("基本設定")]
    [SerializeField, Tooltip("地上移動速度")]
    [Range(0.0f, 100.0f)]
    private float _groundSpeed = 5.0f;

    [SerializeField, Tooltip("空中移動速度")]
    [Range(0.0f, 100.0f)]
    private float _airSpeed = 3.0f;

    [SerializeField, Tooltip("ジャンプ速度")]
    [Range(0.0f, 100.0f)]
    private float _jumpPower = 15.0f;

    [SerializeField, Tooltip("重力加速度")]
    [Range(0.0f, 100.0f)]
    private float _gravity = 30.0f;


    [Header("詳細設定")]
    [SerializeField, Tooltip("地上移動加速度")]
    [Range(0.0f, 100.0f)]
    public float _groundAcceleration = 50.0f;

    [SerializeField, Tooltip("空中移動加速度")]
    [Range(0.0f, 100.0f)]
    public float _airAcceleration = 30.0f;

    [SerializeField, Tooltip("落下初速")]
    [Range(0.0f, 100.0f)]
    public float _fallStartSpeed = 2.0f;

    [SerializeField, Tooltip("最大落下速度")]
    [Range(0.0f, 100.0f)]
    public float _maxFallSpeed = 20.0f;

    [SerializeField, Tooltip("コヨーテタイム (地面から離れてからジャンプできる猶予時間)")]
    [Range(0.0f, 1.0f)]
    public float _coyoteTime = 0.1f;


    private PlayerSettings _playerSettings;     // プレイヤーの全体設定
    private Rigidbody2D _rigidbody;

    private Vector2 _moveInput;                 // 移動入力
    private bool _inputJump;                    // ジャンプ入力

    private Vector2 _currentVelicity;           // 現在の移動速度
    private Vector2 _groundNormal;              // 地面の法線方向

    private bool _isGround;                     // 接地判定フラグ
    private bool _wasGround;                    // 前フレームの接地判定フラグ
    private bool _isJumping;                    // ジャンプ中フラグ
    private bool _isLandingSlope;               // 坂に着地した瞬間のフラグ

    private float _coyoteTimer;                 // コヨーテタイム計測用タイマー
    private float _jumpBufferTimer;             // ジャンプ先行入力計測用タイマー


    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();    }

    public void OnJump(InputAction.CallbackContext context)
    {
        _inputJump = context.performed;
        _jumpBufferTimer = _playerSettings.InputBufferDuration;
    }


    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    private void Start()
    {
        _playerSettings = GlobalGameSettings.Instance.PlayerSettings;
    }

    private void FixedUpdate()
    {
        // 現在の速度を取得
        _currentVelicity = _rigidbody.linearVelocity;

        // 移動処理
        Move();
        Jump();
        AddGravity();
        _rigidbody.linearVelocity = _currentVelicity;

        // 各種タイマー・フラグ更新
        UpdateTimer(Time.fixedDeltaTime);
        UpdateFlags();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // 接地判定
        CheckGround(collision);
    }


    // 移動処理
    private void Move()
    {
        if (_isGround && !_isJumping)
        {
            // 地上移動
            Vector2 rightMoveDir;       // 右入力時の移動方向
            float tangentVelocity;      // 地面に沿った移動速度
            float targetVelocity;       // 目標速度

            // 地面の向きから移動方向を求める
            rightMoveDir = new Vector2(_groundNormal.y, -_groundNormal.x);
            rightMoveDir = rightMoveDir.normalized;

            // 現在の地面に沿った移動速度を求める
            tangentVelocity = Vector2.Dot(rightMoveDir, _currentVelicity);

            // 目標速度を求める
            targetVelocity = _moveInput.x * _groundSpeed;

            if(_isLandingSlope)
            {
                // 坂道に着地時にRigidbodyの速度を一瞬リセットしているので即座に目標速度に変化させる
                tangentVelocity = targetVelocity;
            }
            else
            {
                // 移動速度を目標速度に近づける
                tangentVelocity = Mathf.MoveTowards(tangentVelocity, targetVelocity, _groundAcceleration * Time.fixedDeltaTime);
            }
            _currentVelicity = rightMoveDir * tangentVelocity;
        }
        else
        {
            // 空中移動
            float targetVelocity;       // 目標速度

            // 目標速度を求める
            targetVelocity = _moveInput.x * _airSpeed;

            // 移動速度を目標速度に近づける
            _currentVelicity.x = Mathf.MoveTowards(_currentVelicity.x, targetVelocity, _airAcceleration * Time.fixedDeltaTime);
        }
    }

    // ジャンプ処理
    private void Jump()
    {
        // ジャンプ可能か判定
        if (_isJumping || _coyoteTimer <= 0.0f)
            return;

        // ジャンプ処理
        if (_inputJump && _jumpBufferTimer > 0.0f)
        {
            _currentVelicity.y = _jumpPower;
            _isJumping = true;
            _inputJump = false;
        }
    }

    // 重力処理
    private void AddGravity()
    {
        // 重力
        if (_isGround)
        {
            // Collisionイベントを発生させるために微小な重力を与える
            _currentVelicity.y -= 0.001f;
        }
        else
        {
            if (_wasGround && !_isJumping)
            {
                // ジャンプ以外で地面から離れた瞬間に落下初速を与える
                _currentVelicity.y = -_fallStartSpeed;
            }
            else
            {
                _currentVelicity.y -= _gravity * Time.deltaTime;
            }
        }

        // 落下速度上限
        if (_currentVelicity.y < -_maxFallSpeed)
        {
            _currentVelicity.y = -_maxFallSpeed;
        }
    }

    // 各種タイマー更新
    private void UpdateTimer(float delta)
    {
        //コヨーテタイムの処理
        if (_isJumping)
        {
            _coyoteTimer = 0.0f;
        }
        else
        {
            if (_isGround)
            {
                _coyoteTimer = _coyoteTime;
            }
            else
            {
                _coyoteTimer = Mathf.Max(0.0f, _coyoteTimer - delta);
            }
        }

        //ジャンプバッファの処理
        if (_jumpBufferTimer > 0.0f)
        {
            _jumpBufferTimer = Mathf.Max(0.0f, _jumpBufferTimer - delta);
        }
    }

    // 各種フラグ更新
    private void UpdateFlags()
    {
        // ジャンプ中フラグ更新
        if (_currentVelicity.y <= 0.0f && _isGround)
        {
            _isJumping = false;
        }

        // 接地状態更新
        _wasGround = _isGround;
        _isGround = false;
        _isLandingSlope = false;
    }

    // 接地判定
    private void CheckGround(Collision2D collision)
    {
        // 地面レイヤー判定
        if ((_playerSettings.GroundLayer.value & (1 << collision.gameObject.layer)) == 0)
            return;

        float minGroundAngle = 90.0f;
        foreach (var contact in collision.contacts)
        {
            // 地面として扱う角度か判定
            float angle = Vector2.Angle(contact.normal, Vector2.up);
            if (angle > _playerSettings.MaxSlopeAngle)
                continue;

            _isGround = true;

            // 最も水平に近い地面の角度と法線方向を記録
            if (angle < minGroundAngle)
            {
                minGroundAngle = angle;
                _groundNormal = contact.normal;
            }
        }

        // 坂道に着地した瞬間は一瞬速度をゼロにする(滑り落ち対策)
        if (_isGround && !_wasGround &&
            minGroundAngle > _playerSettings.MinSlopeAngle)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _isLandingSlope = true;
        }
    }
}
