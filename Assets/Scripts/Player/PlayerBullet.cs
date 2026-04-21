using UnityEngine;
using Game.Terrain;

/// <summary>
/// プレイヤーの弾の基本コンポーネント
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerBullet : MonoBehaviour
{
    [SerializeField, Tooltip("弾の初速")]
    [Range(0.0f, 100.0f)]
    private float _startSpeed = 50.0f;

    [SerializeField, Tooltip("着弾時の爆発の攻撃力")]
    [Range(0.0f, 100.0f)]
    private float _explodePower = 1.0f;

    [SerializeField, Tooltip("着弾時の爆発の半径")]
    [Range(0.0f, 100.0f)]
    private float _explodeRadius = 1.0f;

    [SerializeField, Tooltip("射撃間隔")]
    [Range(0.0f, 100.0f)]
    private float _shootInterval = 0.2f;

    [SerializeField, Tooltip("着弾時に発生するエフェクト")]
    private GameObject _hitEffect;


    private PlayerSettings _playerSettings;     // プレイヤーの全体設定
    private CircleCollider2D _circleCollider;
    private Rigidbody2D _rigidbody;

    private Vector2 _lastCenterPosition;        // 前のフレームのコライダー中心座標

    private static readonly float _lifeTime = 0.3f;     // 弾生存時間  

    public float ExplodePower => _explodePower;
    public float ExplodeRadius { get { return _explodeRadius; } set { _explodeRadius = value; } }
    public float ShootInterval => _shootInterval;
    

    private void Awake()
    {
        _circleCollider = GetComponent<CircleCollider2D>();
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _playerSettings = GlobalGameSettings.Instance.PlayerSettings;
        _lastCenterPosition = _circleCollider.bounds.center;
        Destroy(this.gameObject, _lifeTime);
    }

    private void FixedUpdate()
    {
        // 弾の当たり判定を調べる
        RaycastHit2D hit = CircleCast();
        if(hit)
        {
            // ヒットエフェクト生成
            Instantiate(_hitEffect, hit.point, Quaternion.identity);

            // ヒット時の座標を中心に発生する円形の範囲に当たったオブジェクトを取得
            Collider2D[] hitCollider = Physics2D.OverlapCircleAll(hit.point, _explodeRadius, _playerSettings.PlayerAttackHitLayer);

            foreach(Collider2D collider in hitCollider)
            {
                DestructibleTerrain terrain = collider.GetComponent<DestructibleTerrain>();

                if (terrain != null)
                {
                    // 地形に対するダメージ処理
                    terrain.HitAttack(hit.point, _explodeRadius, _explodePower);
                }
            }

            Destroy(this.gameObject);
        }

        // 現在の座礁を記録
        _lastCenterPosition = _circleCollider.bounds.center;
    }

    // ショットの方向から初速を設定する
    public void SetShootDirection(Vector2 direction)
    {
        if (direction == Vector2.zero)
            direction = Vector2.right;

        _rigidbody.linearVelocity = direction.normalized * _startSpeed;
    }

    // 自身の移動範囲から円形の判定を行う
    private RaycastHit2D CircleCast()
    {
        Vector2 direction;      // サークルキャスト方向
        float distance;         // サークルキャスト距離
        float radius;           // サークルキャスト半径

        // サークルキャスト用情報を求める
        Vector2 toCurrentPos = (Vector2)_circleCollider.bounds.center - _lastCenterPosition;
        direction = toCurrentPos.normalized;
        distance = toCurrentPos.magnitude;

        // 弾の半径はコライダーのサイズを使用
        radius = _circleCollider.radius;
        radius *= Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));

        return Physics2D.CircleCast(_lastCenterPosition, radius, direction, distance, _playerSettings.PlayerBulletHitLayer);
    }
}
