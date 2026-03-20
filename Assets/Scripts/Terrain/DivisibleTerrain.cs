using System.Linq;
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
        
        public void Initialize(TerrainGridData terrainGrid)
        {
            if (_terrainContext == null) _terrainContext = GetComponent<TerrainContext>();
            if (_terrainPolygon == null) _terrainPolygon = GetComponent<TerrainPolygon>();

            _terrainContext.TerrainGrid = terrainGrid;
        
            _terrainPolygon.OnGridChanged();
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
            TerrainGridData terrainGrid = _terrainContext.TerrainGrid;
            ref GridCell refCell = ref terrainGrid.Get(x, y);

            if (!refCell.solid) return;

            refCell.durability -= damage;
            
            if (refCell.durability <= 0.0f)
            {
                var splitResults = TerrainSplitDetector.Instance.RemoveAndCheckSplit(terrainGrid, x, y);

                if (splitResults.Count > 0)
                {
                    foreach (SplitResult result in splitResults)
                    {
                        DivisibleTerrain newChunk = Instantiate(this);

                        Vector3 localOffset = new Vector3(
                            result.Offset.x * terrainGrid.GridScale,
                            result.Offset.y * terrainGrid.GridScale,
                            0f
                        );
                        
                        newChunk.transform.position = this.transform.TransformPoint(localOffset);
                        newChunk.transform.rotation = this.transform.rotation;

                        if (TryGetComponent<Rigidbody2D>(out var rb) && newChunk.TryGetComponent<Rigidbody2D>(out var newRb))
                        {
                            newRb.linearVelocity = rb.linearVelocity; 
                            newRb.angularVelocity = rb.angularVelocity;
                        }

                        if (newChunk.TryGetComponent<PolygonCollider2D>(out var col)) col.pathCount = 0;
                        
                        newChunk.Initialize(result.GridData);
                    }

                    Destroy(gameObject);
                }
                else
                {
                    if (CheckIfEmpty(terrainGrid))
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        _terrainPolygon.OnGridChanged();
                    }
                }
            }
        }
        
        private bool CheckIfEmpty(TerrainGridData grid)
        {
            GridCell[] rawCells = grid.RawCells;
            for (int i = 0; i < rawCells.Length; i++)
            {
                if (rawCells[i].solid) return false;
            }
            return true;
        }
    }
}
