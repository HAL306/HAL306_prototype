using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの弾を発射するコンポーネント
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    [SerializeField, Tooltip("発射する弾")]
    private GameObject _bulletPrefab;


    private bool _inputShoot;           // ショット入力
    private Vector2 _inputAim;          // エイム方向入力 (スティック限定)
    private bool _isMouseAim;           // マウス操作によるエイムを行うフラグ

    private Vector2 _shootAimTarget;    // 現在のショットターゲット座標
    private float _cooldownTimer;       // ショット待ち時間計測用タイマー


    public void OnShoot(InputAction.CallbackContext context)
    {
        _inputShoot = context.performed;
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        if (context.control.device.layout == "Mouse")
        {
            _isMouseAim = true;
        }
        else
        {
            _isMouseAim = false;
        }

        _inputAim = context.ReadValue<Vector2>();
    }


    private void Update()
    {
        MoveAimTarget();

        if (_cooldownTimer > 0.0f)
        {
            _cooldownTimer -= Time.deltaTime;
            return;
        }

        if(_inputShoot)
        {
            Shoot();
        }
    }

    // ショットターゲットを移動させる
    private void MoveAimTarget()
    {
        // ターゲット座標取得
        if (_isMouseAim)
        {
            // マウス操作
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector2 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
            _shootAimTarget = worldPosition - (Vector2)Camera.main.transform.position;

        }
        else
        {
            // スティック操作
            _shootAimTarget = _inputAim.normalized;
        }

        // ゼロ対策
        if (_shootAimTarget == Vector2.zero)
        {
            _shootAimTarget = Vector2.right;
        }
    }

    private void Shoot()
    {
        // 弾を生成
        GameObject bulletObj = Instantiate(_bulletPrefab, transform.position, Quaternion.identity);
        PlayerBullet bullet = bulletObj.GetComponent<PlayerBullet>();

        if (bullet == null)
            return;

        // 弾の方向を設定
        bullet.SetShootDirection(_shootAimTarget);

        // ショット待ち時間は弾の種類によって変動
        _cooldownTimer = bullet.ShootInterval;
    }
}
