using UnityEngine;
using Game.Terrain;

/// <summary>
/// ゲーム全体設定
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "Scriptable Objects/GameSettings")]
public class GameSettings : ScriptableObject
{
    [SerializeField, Tooltip("プレイヤー設定情報")]
    private PlayerSettings _playerSettings;

    [SerializeField, Tooltip("地形設定情報")]
    private TerrainSettings _terrainSettings;

    public PlayerSettings PlayerSettings => _playerSettings;
    public TerrainSettings TerrainSettings => _terrainSettings;
}
