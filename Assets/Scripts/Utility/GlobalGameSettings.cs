using Game.Terrain;
using UnityEngine;

public class GlobalGameSettings : MonoBehaviour
{
    private static GlobalGameSettings _instance;
    public static GlobalGameSettings Instance => _instance;


    [SerializeField, Tooltip("プレイヤー設定情報")]
    private PlayerSettings _playerSettings;

    [SerializeField, Tooltip("地形設定情報")]
    private TerrainSettings _terrainSettings;

    public PlayerSettings PlayerSettings => _playerSettings;
    public TerrainSettings TerrainSettings => _terrainSettings;


    private void Awake()
    {
        if (_instance != null)
            Destroy(_instance.gameObject);

        _instance = this;
    }
}
