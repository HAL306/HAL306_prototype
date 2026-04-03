using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの弾を発射するコンポーネント
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    [SerializeField, Tooltip("発射する弾")]
    private GameObject BulletPrefab;


    private bool _inputShoot;           // ショット入力
    private Vector2 _inputAim;          // エイム方向入力

    private Vector2 _shootAimTarget;    // 現在のショットターゲット座標
    private float _cooldownTimer;       // ショット待ち時間計測用タイマー


    public void OnShoot(InputAction.CallbackContext context)
    {
        _inputShoot = context.performed;
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        _inputAim = context.ReadValue<Vector2>();
        Debug.Log(_inputAim);
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

    private void MoveAimTarget()
    {
        if (_inputAim != Vector2.zero)
        {
            _shootAimTarget = _inputAim.normalized;
        }
    }

    private void Shoot()
    {
        GameObject bulletObj = Instantiate(BulletPrefab, transform.position, Quaternion.identity);
        PlayerBullet bullet = bulletObj.GetComponent<PlayerBullet>();

        if (bullet == null)
            return;

        bullet.SetShootDirection(_shootAimTarget);
        _cooldownTimer = bullet.ShootInterval;
    }
}
