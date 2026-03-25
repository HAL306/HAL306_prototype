using System;
using System.Collections.Generic;
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

        [SerializeField, Tooltip("グリッド矩形の描画設定")]
        private bool _gridDrawRect = true;

        [SerializeField, Tooltip("グリッド生成設定")]
        private bool _gridCreate = false;

        [SerializeField, Tooltip("地形のデフォルト耐久値")]
        [Range(1.0f,100.0f)]
        private float _defaultDurability = 1.0f;

        private TerrainContext _terrainContext;     // 地形情報


        private void Awake()
        {
            _terrainContext = GetComponent<TerrainContext>();

            if(_gridCreate)
            {
                CreateDebugGridData();
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            if (_gridDraw)
            {
                DrawGridCell();
            }

            if (_gridDrawRect)
            {
                DrawGridRect();
            }
        }

        private void DrawGridCell()
        {
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

        private void DrawGridRect()
        {
            TerrainGridData terrainGrid = _terrainContext.TerrainGrid;
            if (terrainGrid == null)
                return;

            Gizmos.color = Color.magenta;

            float offset = -terrainGrid.GridScale * 0.5f;
            float top = terrainGrid.Height * terrainGrid.GridScale + offset;
            float bottom = offset;
            float left = offset;
            float right = terrainGrid.Width* terrainGrid.GridScale + offset;

            Vector3 LT = transform.TransformPoint(new Vector3(left, top, 0.0f));
            Vector3 LB = transform.TransformPoint(new Vector3(left, bottom, 0.0f));
            Vector3 RT = transform.TransformPoint(new Vector3(right, top, 0.0f));
            Vector3 RB = transform.TransformPoint(new Vector3(right, bottom, 0.0f));

            Gizmos.DrawLine(LT, LB);
            Gizmos.DrawLine(LB, RB);
            Gizmos.DrawLine(RB, RT);
            Gizmos.DrawLine(RT, LT);
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
