using UnityEngine;

namespace Game.Terrain
{
    /// <summary>
    /// 地形の全体設定
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainSetting", menuName = "Scriptable Objects/TerrainSetting")]
    public class TerrainSetting : ScriptableObject
    {
        [SerializeField, Tooltip("地形破壊時に発生するエフェクト")]
        private GameObject _destroyEffect;

        [SerializeField, Tooltip("地形当たり判定の簡略化レベル")]
        [Range(0.001f, 1.0f)]
        private float _colliderEpsilon = 0.4f;

        [SerializeField, Tooltip("地形描画形状の簡略化レベル")]
        [Range(0.001f, 1.0f)]
        private float _rendererEpsilon = 0.2f;


        public GameObject DestroyEffect => _destroyEffect;
        public float RendererEpsilon => _rendererEpsilon;
        public float ColliderEpsilon => _colliderEpsilon;
    }
}
