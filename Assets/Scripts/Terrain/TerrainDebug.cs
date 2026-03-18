using UnityEngine;

namespace Game.Terrain
{
    /// <summary>
    /// 地形のデバッグ機能を扱うコンポーネント
    /// </summary>
    [RequireComponent (typeof(TerrainContext))]
    public class TerrainDebug : MonoBehaviour
    {
        [SerializeField, Tooltip("グリッドの描画設定")]
        private bool _gridDraw = true;

        [SerializeField, Tooltip("地形のデフォルト耐久値")]
        [Range(1.0f,100.0f)]
        private float _defaultDurability = 1.0f;

        private TerrainContext _terrainContext;     // 地形情報


        private void Awake()
        {
            _terrainContext = GetComponent<TerrainContext>();
            CreateDebugGridData();
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            if (!_gridDraw)
                return;

            TerrainGridData terrainGrid = _terrainContext.TerrainGrid;
            if (terrainGrid == null)
                return;

            for (int y = 0; y < terrainGrid.Height; ++y)
            {
                for (int x = 0; x < terrainGrid.Width; ++x)
                {
                    if (!terrainGrid.InBounds(x, y))
                        continue;

                    ref GridCell refCell = ref terrainGrid.Get(x, y);

                    if (!refCell.solid)
                        continue;

                    // 固定地形は色を変える
                    if (refCell.isStatic)
                    {
                        Gizmos.color = Color.yellow;
                    }
                    else
                    {
                        float damageRatio = (_defaultDurability - refCell.durability) / _defaultDurability;
                        Gizmos.color = Color.Lerp(Color.white, Color.red, damageRatio);
                    }

                    Vector3 pos = new Vector3(x, y, 0.0f) * terrainGrid.GridScale;
                    pos = transform.TransformPoint(pos);
                    Gizmos.DrawWireSphere(pos, terrainGrid.GridScale * 0.5f);
                }
            }
        }


        // デバッグ用地形を生成
        private void CreateDebugGridData()
        {
            TerrainGridData terrainGrid = new TerrainGridData(20, 20, 0.2f);
            for (int y = 0; y < 20; ++y)
            {
                for (int x = 0; x < 20; ++x)
                {
                    GridCell cell = new GridCell();
                    if (x + y < 6 || x + y > 30)
                    {
                        cell.solid = false;
                    }
                    else
                    {
                        cell.solid = true;
                    }
                    cell.durability = _defaultDurability;

                    terrainGrid.Set(x, y, cell);
                }
            }

            _terrainContext.TerrainGrid = terrainGrid;
        }
    }
}
