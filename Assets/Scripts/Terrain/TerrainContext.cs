using UnityEngine;

namespace Game.Terrain
{
    /// <summary>
    /// 地形のコアコンポーネント
    /// グリッドデータの生成・保持を行う
    /// </summary>
    public class TerrainContext : MonoBehaviour
    {
        private TerrainGridData _terrainGrid;       // 地形データ

        public TerrainGridData TerrainGrid
        {
            get
            {
                return _terrainGrid;
            }
            set
            {
                _terrainGrid = value;
            }
        }

        private void Awake()
        {
            
        }
    }
}
