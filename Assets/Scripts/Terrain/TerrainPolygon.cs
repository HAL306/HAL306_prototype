using LibTessDotNet;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Terrain
{
    /// <summary>
    /// 地形のグリッド情報からポリゴンを生成するコンポーネント
    /// </summary>
    [RequireComponent (typeof(TerrainContext))]
    [RequireComponent (typeof(PolygonCollider2D))]
    [RequireComponent (typeof(MeshRenderer))]
    [RequireComponent (typeof(MeshFilter))]
    public class TerrainPolygon : MonoBehaviour
    {
        private TerrainContext _terrainContext;             // 地形情報
        private PolygonCollider2D _polygonCollider2D;       // コライダーコンポーネント
        private MeshRenderer _meshRenderer;                 // レンダラーコンポーネント
        private MeshFilter _meshFilter;                     // メッシュ情報

        private bool _rebuildFlag = false;                  // ポリゴン再構築フラグ


        private void Awake()
        {
            _terrainContext = GetComponent<TerrainContext>();
            _polygonCollider2D = GetComponent<PolygonCollider2D>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();
        }

        private void Start()
        {
            RebuildPolygon();
        }

        private void Update()
        {
            if (_rebuildFlag)
            {
                if(!RebuildPolygon())
                {
                    Debug.Log("地形ポリゴンの生成に失敗しました");
                }

                _rebuildFlag = false;
            }
        }

        public void OnGridChanged()
        {
            _rebuildFlag = true;
        }

#if UNITY_EDITOR
        public bool InitPolygon()
#else
        private bool InitPolygon()
#endif
        {
            _terrainContext = GetComponent<TerrainContext>();
            _polygonCollider2D = GetComponent<PolygonCollider2D>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();

            if(!RebuildPolygon())
            {
                return false;
            }

            return true;
        }
        // ポリゴンを再構築する
        private bool RebuildPolygon()
        {
            List<EdgeLoop> edgeLoops = new List<EdgeLoop>();    // 元となるエッジループ
            MarchingSquares marchingSquares = new MarchingSquares();
            marchingSquares.Init(_terrainContext);

            // 地形情報からエッジループを生成
            if (!marchingSquares.BuildEdgeLoops(out edgeLoops))
                return false;

            // コライダー形状を更新
            if (!RebuildCollider(edgeLoops))
                return false;

            // 描画用メッシュ形状を更新
            if (!RebuildRenderMesh(edgeLoops))
                return false;

            return true;
        }

        // コライダー用ポリゴンを再構築する
        private bool RebuildCollider(List<EdgeLoop> edgeLoops)
        {
            TerrainGridData terrainGrid = _terrainContext.TerrainGrid;
            if (terrainGrid == null)
                return false;

            _polygonCollider2D.pathCount = edgeLoops.Count;
            for (int i = 0; i < _polygonCollider2D.pathCount; ++i)
            {
                List<Vector2> colliderPath = edgeLoops[i].edgePoints;

                // エッジループ簡略化
                float epsilon = _terrainContext.TerrainSetting.ColliderEpsilon * terrainGrid.GridScale;
                colliderPath = RamerDouglasPeucker.RamerDouglasPeuckerAlgorithm(colliderPath, epsilon);

                // コライダー形状を更新
                _polygonCollider2D.SetPath(i, colliderPath);
            }

            return true;
        }

        // 描画用ポリゴンを再構築する
        private bool RebuildRenderMesh(List<EdgeLoop> edgeLoops)
        {
            TerrainGridData terrainGrid = _terrainContext.TerrainGrid;
            if (terrainGrid == null)
                return false;

            Tess tess = new Tess();

            for (int i = 0; i < edgeLoops.Count; ++i)
            {
                List<Vector2> renderMeshPath = edgeLoops[i].edgePoints;

                // エッジループ簡略化
                float epsilon = _terrainContext.TerrainSetting.RendererEpsilon * terrainGrid.GridScale;
                renderMeshPath = RamerDouglasPeucker.RamerDouglasPeuckerAlgorithm(renderMeshPath, epsilon);

                // 回転方向を取得
                ContourOrientation orientation;
                if (edgeLoops[i].isClockwise)
                {
                    orientation = ContourOrientation.Clockwise;
                }
                else
                {
                    orientation = ContourOrientation.CounterClockwise;
                }

                // エッジループを登録
                tess.AddContour(ToContour(renderMeshPath), orientation);
            }

            // エッジループを三角面化
            tess.Tessellate(WindingRule.EvenOdd, TessElementType.Polygons, 3);

            // 作成した三角面を取り出す
            Vector3[] vertices = new Vector3[tess.Vertices.Length];
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = new Vector3(tess.Vertices[i].Position.X, tess.Vertices[i].Position.Y, 0.0f);
            }

            // 新しくメッシュを生成
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = tess.Elements;

            _meshFilter.mesh = mesh;

            return true;
        }

        // LibTessDotNet用のエッジループに変換
        ContourVertex[] ToContour(List<Vector2> edgeLoop)
        {
            var result = new ContourVertex[edgeLoop.Count];

            for (int i = 0; i < edgeLoop.Count; i++)
            {
                result[i].Position = new Vec3(edgeLoop[i].x, edgeLoop[i].y, 0);
            }

            return result;
        }
    }
}