using Unity.VisualScripting;
using UnityEngine;

namespace Game.Terrain
{
    /// <summary>
    /// 地形のダメージ判定を行い、地形を分離・消滅させるコンポーネント
    /// </summary>
    [RequireComponent (typeof(TerrainContext))]
    [RequireComponent (typeof(TerrainPolygon))]
    public class DestructibleTerrain : MonoBehaviour
    {
        private TerrainSettings _terrainSettings;   // 地形の全体設定
        private TerrainContext _terrainContext;     // 地形情報
        private TerrainPolygon _terrainPolygon;     // 地形のポリゴンデータ
        private Rigidbody2D _rigidbody;


        private void Awake()
        {
            _terrainContext = GetComponent<TerrainContext>();
            _terrainPolygon = GetComponent<TerrainPolygon>();
        }

        private void Start()
        {
            _terrainSettings = GlobalGameSettings.Instance.TerrainSettings;
        }


        public void Initialize(TerrainGridData terrainGrid)
        {
            if (_terrainContext == null) _terrainContext = GetComponent<TerrainContext>();
            if (_terrainPolygon == null) _terrainPolygon = GetComponent<TerrainPolygon>();

            _terrainContext.TerrainGrid = terrainGrid;
        
            _terrainPolygon.OnGridChanged();
        }

        // 基本のダメージ処理
        public void HitAttack(Vector2 center, float radius, float damage)
        {
            TerrainGridData terrainGrid = _terrainContext.TerrainGrid;
            if (terrainGrid == null)
                return;

            // 全てのセルに対して当たり判定をとる
            for (int y = 0; y < terrainGrid.Height; ++y)
            {
                for (int x = 0; x < terrainGrid.Width; ++x)
                {
                    if (terrainGrid.Get(x, y).solid == false)
                        continue;

                    // セルの中心のワールド座標を求める
                    Vector2 cellCenter = new Vector2(x, y) * terrainGrid.GridScale;
                    cellCenter = transform.TransformPoint(cellCenter);

                    // セルに対するダメージ判定
                    float dx = cellCenter.x - center.x;
                    float dy = cellCenter.y - center.y;
                    float sumRadius = terrainGrid.GridScale * 0.5f + radius;

                    if (dx * dx + dy * dy <= sumRadius * sumRadius)
                    {
                        DamegeCell(x, y, damage);
                    }
                }
            }
            
            // 地形分離判定を行う
            var splitResults = TerrainSplitDetector.Instance.ExecuteSplitCheck(terrainGrid);

            if (splitResults.Count > 0)
            {
                // 分離判定結果ごとに地形を生成
                foreach (SplitResult result in splitResults)
                {
                    DestructibleTerrain newChunk = Instantiate(this);

                    // 新しく生成された地形の位置補正
                    Vector3 localOffset = new Vector3(
                        result.Offset.x * terrainGrid.GridScale,
                        result.Offset.y * terrainGrid.GridScale,
                        0.0f
                    );
                    newChunk.transform.position = this.transform.TransformPoint(localOffset);
                    newChunk.transform.rotation = this.transform.rotation;

                    // テクスチャのオフセットを補正
                    if(newChunk.gameObject.TryGetComponent<MeshRenderer>(out var mr))
                    {
                        Vector2 offset = mr.material.GetVector("_TextureOffset");
                        mr.material.SetVector("_TextureOffset", offset + (Vector2)localOffset);
                    }

                    // Rigidbodyの追加と現在の速度の維持
                    Rigidbody2D newRb = null;
                    if (TryGetComponent<Rigidbody2D>(out var rb))
                    {
                        if (newChunk.TryGetComponent<Rigidbody2D>(out newRb))
                        {
                            // 速度コピー
                            newRb.linearVelocity = rb.linearVelocity;
                            newRb.angularVelocity = rb.angularVelocity;
                        }
                    }
                    else
                    {
                        // 固定地形判定
                        if (!result.GridData.IsStatic())
                        {
                            newRb = newChunk.AddComponent<Rigidbody2D>();
                        }
                    }

                    // 当たり判定初期化
                    if (newChunk.TryGetComponent<PolygonCollider2D>(out var col)) col.pathCount = 0;
                    
                    newChunk.Initialize(result.GridData);
                    if (newRb != null)
                        newRb.mass = newChunk._terrainContext.TerrainGrid.GetSumMass();
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

        private void OnTriggerStay2D(Collider2D collider)
        {
            TerrainGridData terrainGrid = _terrainContext.TerrainGrid;
            if (terrainGrid == null)
                return;

            // 全てのセルに対して当たり判定をとる
            for (int y = 0; y < terrainGrid.Height; ++y)
            {
                for (int x = 0; x < terrainGrid.Width; ++x)
                {
                    if (terrainGrid.Get(x, y).solid == false)
                        continue;

                    // セルの中心のワールド座標を求める
                    Vector2 center = new Vector2(x, y) * terrainGrid.GridScale;
                    center = transform.TransformPoint(center);

                    // セルに対するダメージ判定
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

            // セルごとのダメージ処理
            refCell.durability -= damage;
            
            if (refCell.durability <= 0.0f)
            {
                // 破壊エフェクト生成
                Vector3 effectPos = transform.TransformPoint(new Vector3(x, y, 0.0f) * terrainGrid.GridScale);
                Instantiate(_terrainSettings.DestroyEffect, effectPos, Quaternion.identity);

                refCell.solid = false;
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
