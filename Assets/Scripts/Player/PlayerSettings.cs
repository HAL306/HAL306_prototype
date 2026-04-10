using UnityEngine;

/// <summary>
/// プレイヤーの全体設定
/// </summary>
[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Scriptable Objects/PlayerSettings")]
public class PlayerSettings : ScriptableObject
{
    [SerializeField, Tooltip("接地判定を取るレイヤー")]
    private LayerMask _groundLayer;

    [SerializeField, Tooltip("プレイヤーの弾が当たるレイヤー (破壊できない地形を含む)")]
    private LayerMask _playerBulletHitLayer;

    [SerializeField, Tooltip("プレイヤーの攻撃が当たるレイヤー")]
    private LayerMask _playerAttackHitLayer;

    [SerializeField, Tooltip("坂道とみなす最大角度")]
    [Range(0.0f, 90.0f)]
    private float _maxSlopeAngle = 50.0f;

    [SerializeField, Tooltip("坂道とみなす最小角度\nMaxSlopeAngleより小さい値を推奨します")]
    [Range(0.0f, 90.0f)]
    private float _minSlopeAngle = 10.0f;

    [SerializeField, Tooltip("先行入力の猶予時間")]
    [Range(0.0f, 1.0f)]
    private float _inputBufferDuration = 0.1f;


    public LayerMask GroundLayer => _groundLayer;
    public LayerMask PlayerBulletHitLayer => _playerBulletHitLayer;
    public LayerMask PlayerAttackHitLayer => _playerAttackHitLayer;
    public float MaxSlopeAngle => _maxSlopeAngle;
    public float MinSlopeAngle => _minSlopeAngle;
    public float InputBufferDuration => _inputBufferDuration;
}
