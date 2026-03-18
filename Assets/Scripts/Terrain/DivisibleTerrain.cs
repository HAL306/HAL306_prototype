using UnityEngine;

namespace Game.Terrain
{
    [RequireComponent (typeof(TerrainContext))]
    [RequireComponent (typeof(TerrainPolygon))]
    public class DivisibleTerrain : MonoBehaviour
    {
        private TerrainContext _terrainContext;     // 地形情報
        private TerrainPolygon _terrainPolygon;     // 地形のポリゴンデータ


        private void Awake()
        {
            _terrainContext = GetComponent<TerrainContext>();
            _terrainPolygon = GetComponent<TerrainPolygon>();
        }

        private void OnTriggerStay2D(Collider2D collider)
        {
            TerrainGridData terrainGrid = _terrainContext.TerrainGrid;
            if (terrainGrid == null)
                return;

            for (int y = 0; y < terrainGrid.Height; ++y)
            {
                for (int x = 0; x < terrainGrid.Width; ++x)
                {
                    if (terrainGrid.Get(x, y).solid == false)
                        continue;

                    // セルの中心のワールド座標を求める
                    Vector2 center = new Vector2(x, y) * terrainGrid.GridScale;
                    center = transform.TransformPoint(center);

                    if (HitCell(collider, center, terrainGrid.GridScale * 0.5f))
                    {
                        DamegeCell(x, y, 1.0f);
                    }
                }
            }
        }

        private bool HitCell(Collider2D collider, Vector2 center, float radius)
        {
            // 半径を考慮しないヒット判定
            if (collider.OverlapPoint(center))
                return true;

            // 半径を考慮したヒット判定
            Vector2 closest = collider.ClosestPoint(center);

            float dx = closest.x - center.x;
            float dy = closest.y - center.y;

            return (dx * dx + dy * dy) <= radius * radius;
        }

        private void DamegeCell(int x, int y, float damage)
        {
            ref GridCell refCell = ref _terrainContext.TerrainGrid.Get(x, y);

            refCell.durability -= damage;
            if(refCell.durability <= 0.0f)
            {
                // 存在しないセルとして扱う
                refCell.solid = false;
                _terrainPolygon.OnGridChanged();
            }
        }
    }
}
