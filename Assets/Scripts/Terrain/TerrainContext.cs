using UnityEngine;

namespace Game.Terrain
{
    /// <summary>
    /// 地形のコアコンポーネント
    /// グリッドデータの生成・保持を行う
    /// </summary>
    public class TerrainContext : MonoBehaviour
    {
        [SerializeField, Tooltip("地形の全体設定")]
        private TerrainSetting _terrainSetting;

        [SerializeField]
        private TerrainGridData _terrainGrid;       // 地形データ


        public TerrainSetting TerrainSetting => _terrainSetting;


        public TerrainGridData TerrainGrid
        {
            get { return _terrainGrid; }
            set { _terrainGrid = value; }
        }
    }
}
