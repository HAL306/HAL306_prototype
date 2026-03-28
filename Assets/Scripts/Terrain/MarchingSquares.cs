using UnityEngine;
using System.Collections.Generic;

namespace Game.Terrain
{
    public struct EdgeLoop
    {
        public List<Vector2> edgePoints;    // 頂点座標
        public bool isClockwise;            // エッジループの回転方向
    }

    /// <summary>
    /// MarchingSquaresアルゴリズムによるエッジ生成を行うクラス
    /// </summary>
    public class MarchingSquares
    {
        //------------------
        //      型定義
        //------------------

        // エッジ接続点の種類
        private enum EdgePointType
        {
            NONE,
            TOP,
            BOTTOM,
            LEFT,
            RIGHT
        }

        // 輪郭追跡エッジの生成用情報
        private struct EdgeTraceInfo
        {
            public Vector2Int nextGridPos;          // 次に探索するグリッド座標
            public EdgePointType nextStartPoint;    // 次のエッジの始点
            public Vector2 edgePos;                 // 生成されたエッジの頂点
        }


        //------------------------------
        //      定数・テーブル定義
        //------------------------------

        private static readonly int _maxEdgePoint = 10000;        // 最大エッジ生成数 (無限ループ対策)
        private static readonly int _maxEdgeLoop = 100;           // 最大エッジループ生成数 (無限ループ対策)

        // MarchingSquaresにおける頂点順を定義したテーブル
        private static readonly Vector2Int[] _squareIndexTable =
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(1, 1),
            new Vector2Int(0, 1)
        };

        // MarchingSquaresで求めるエッジの種類を接続点で定義したテーブル
        // 0と1, 2と3番目の要素を一つのエッジとして扱います
        private static readonly EdgePointType[,] _edgeTypeTable =
        {
            { EdgePointType.NONE,   EdgePointType.NONE,   EdgePointType.NONE,   EdgePointType.NONE },   // 0000   (0)
            { EdgePointType.BOTTOM, EdgePointType.LEFT,   EdgePointType.NONE,   EdgePointType.NONE },   // 0001   (1)
            { EdgePointType.BOTTOM, EdgePointType.RIGHT,  EdgePointType.NONE,   EdgePointType.NONE },   // 0010   (2)
            { EdgePointType.LEFT,   EdgePointType.RIGHT,  EdgePointType.NONE,   EdgePointType.NONE },   // 0011   (3)
            { EdgePointType.TOP,    EdgePointType.RIGHT,  EdgePointType.NONE,   EdgePointType.NONE },   // 0100   (4)
            { EdgePointType.TOP,    EdgePointType.RIGHT,  EdgePointType.BOTTOM, EdgePointType.LEFT },   // 0101   (5)
            { EdgePointType.TOP,    EdgePointType.BOTTOM, EdgePointType.NONE,   EdgePointType.NONE },   // 0110   (6)
            { EdgePointType.TOP,    EdgePointType.LEFT,   EdgePointType.NONE,   EdgePointType.NONE },   // 0111   (7)
            { EdgePointType.TOP,    EdgePointType.LEFT,   EdgePointType.NONE,   EdgePointType.NONE },   // 1000   (8)
            { EdgePointType.TOP,    EdgePointType.BOTTOM, EdgePointType.NONE,   EdgePointType.NONE },   // 1001   (9)
            { EdgePointType.TOP,    EdgePointType.LEFT,   EdgePointType.BOTTOM, EdgePointType.RIGHT },  // 1010  (10)
            { EdgePointType.TOP,    EdgePointType.RIGHT,  EdgePointType.NONE,   EdgePointType.NONE },   // 1011  (11)
            { EdgePointType.LEFT,   EdgePointType.RIGHT,  EdgePointType.NONE,   EdgePointType.NONE },   // 1100  (12)
            { EdgePointType.BOTTOM, EdgePointType.RIGHT,  EdgePointType.NONE,   EdgePointType.NONE },   // 1101  (13)
            { EdgePointType.BOTTOM, EdgePointType.LEFT,   EdgePointType.NONE,   EdgePointType.NONE },   // 1110  (14)
            { EdgePointType.NONE,   EdgePointType.NONE,   EdgePointType.NONE,   EdgePointType.NONE }    // 1111  (15)
        };


        //--------------------
        //      変数定義
        //--------------------

        private TerrainContext _terrainContext;         // 地形情報
        private bool[,] _visited;                       // 探索済みフラグ (MarchingSquaresの右上を基準にy,xで記録)


        //--------------------------
        //      メイン関数定義
        //--------------------------

        public void Init(TerrainContext terrrainContext)
        {
            _terrainContext = terrrainContext;
        }


        // 地形グリッドからエッジループを生成する
        public bool BuildEdgeLoops(out List<EdgeLoop> edgeLoops)
        {
            edgeLoops = new List<EdgeLoop>();

            if (_terrainContext == null)
                return false;

            TerrainGridData terrainGrid = _terrainContext.TerrainGrid;
            if (terrainGrid == null)
                return false;

            // 範囲外を空のセルとして扱うので一つ大きく確保
            _visited = new bool[terrainGrid.Height + 1, terrainGrid.Width + 1];

            for (int i = 0; i < _maxEdgeLoop; ++i)
            {
                // 最初のセルを見つける
                Vector2Int startGridPos;
                bool isHole;
                if (!FindStartCell(out startGridPos, out isHole))
                {
                    if (i == 0)
                    {
                        // 一つも見つからなかったら失敗を返す
                        return false;
                    }
                    else
                    {
                        // 見つからなくなったら終了
                        return true;
                    }
                }

                // エッジループを生成
                EdgeLoop edgeLoop = new EdgeLoop();
                if (!GenerateEdgeLoop(startGridPos, out edgeLoop.edgePoints))
                    return false;

                // エッジループの向きを求める
                // 符号付き面積が負の値なら時計回り
                float area = GetSignedArea(edgeLoop.edgePoints);
                edgeLoop.isClockwise = area < 0.0f;

                // 外周は反時計回り・穴は時計回りに補正
                if (isHole != edgeLoop.isClockwise)
                {
                    // エッジループの向きを反転
                    edgeLoop.isClockwise = !edgeLoop.isClockwise;
                    edgeLoop.edgePoints.Reverse();
                }

                edgeLoops.Add(edgeLoop);
            }

            return false;
        }


        //------------------------
        //      補助関数定義
        //------------------------

        // 開始位置のセルを見つける
        private bool FindStartCell(out Vector2Int gridPos, out bool isHole)
        {
            gridPos = new Vector2Int();
            TerrainGridData terrainGrid = _terrainContext.TerrainGrid;

            for (int y = 0; y < terrainGrid.Height; ++y)
            {
                bool lastCellSolid = false;

                for (int x = 0; x < terrainGrid.Width; ++x)
                {
                    // エッジがあるか判定
                    if (terrainGrid.Get(x, y).solid == lastCellSolid)
                    {
                        continue;
                    }
                    else
                    {
                        lastCellSolid = !lastCellSolid;
                    }

                    // 未探索のエッジか調べる
                    if (!_visited[y, x])
                    {
                        _visited[y, x] = true;

                        // 見つかった開始位置の右上セルが存在しないなら穴のエッジとする
                        isHole = !terrainGrid.Get(x, y).solid;

                        gridPos.x = x - 1;
                        gridPos.y = y - 1;

                        return true;
                    }
                    else
                    {
                        _visited[y, x] = true;
                    }
                }
            }

            gridPos = default;
            isHole = default;
            return false;
        }

        // グリッド上の座標からMarchingSquaresのパターン(0～15)を求める
        private int GetMarchingSquareType(int x, int y)
        {
            int marchingType = 0;
            TerrainGridData terrainGrid = _terrainContext.TerrainGrid;

            for (int i = 0; i < 4; ++i)
            {
                // 調べるグリッド座標を求める
                Vector2Int index = _squareIndexTable[i];
                index.x += x;
                index.y += y;

                // 範囲チェック
                if (terrainGrid.InBounds(index.x, index.y))
                {
                    // 指定したインデックスにセルが存在するか
                    if (terrainGrid.Get(index.x, index.y).solid)
                        marchingType |= 1 << i;
                }
            }

            return marchingType;
        }

        // 現在のエッジ始点と矩形のパターンからエッジ終点を求める
        private bool GetEndEdgePoint(EdgePointType startPoint, int marchingType, out EdgePointType endPoint)
        {
            // 最初のエッジの処理
            if (startPoint == EdgePointType.NONE)
            {
                endPoint = _edgeTypeTable[marchingType, 0];
                return true;
            }

            for (int i = 0; i < 4; ++i)
            {
                if (startPoint != _edgeTypeTable[marchingType, i])
                    continue;

                // エッジ始点に対応するエッジ終点を返す
                if (i % 2 == 0)
                {
                    endPoint = _edgeTypeTable[marchingType, i + 1];
                    return true;
                }
                else
                {
                    endPoint = _edgeTypeTable[marchingType, i - 1];
                    return true;
                }
            }

            endPoint = EdgePointType.NONE;
            return false;
        }

        // エッジ接続点を反転する
        private EdgePointType ReverseEdgePoint(EdgePointType edgePoint)
        {
            switch (edgePoint)
            {
                case EdgePointType.TOP:
                    return EdgePointType.BOTTOM;

                case EdgePointType.BOTTOM:
                    return EdgePointType.TOP;

                case EdgePointType.LEFT:
                    return EdgePointType.RIGHT;

                case EdgePointType.RIGHT:
                    return EdgePointType.LEFT;
            }

            return EdgePointType.NONE;
        }

        // グリッド座標と接続点の種類から実際のエッジ頂点を求める
        private Vector2 GetEdgePos(Vector2Int gridPos, EdgePointType edgePoint)
        {
            TerrainGridData terrainGrid = _terrainContext.TerrainGrid;

            Vector2 a = new Vector2();
            Vector2 b = new Vector2();

            // 線形補完に使用するセルの座標を取得
            switch (edgePoint)
            {
                case EdgePointType.TOP:
                    a = terrainGrid.GetCellLocalPos(gridPos.x, gridPos.y + 1);
                    b = terrainGrid.GetCellLocalPos(gridPos.x + 1, gridPos.y + 1);
                    break;

                case EdgePointType.BOTTOM:
                    a = terrainGrid.GetCellLocalPos(gridPos.x, gridPos.y);
                    b = terrainGrid.GetCellLocalPos(gridPos.x + 1, gridPos.y);
                    break;

                case EdgePointType.LEFT:
                    a = terrainGrid.GetCellLocalPos(gridPos.x, gridPos.y);
                    b = terrainGrid.GetCellLocalPos(gridPos.x, gridPos.y + 1);
                    break;

                case EdgePointType.RIGHT:
                    a = terrainGrid.GetCellLocalPos(gridPos.x + 1, gridPos.y);
                    b = terrainGrid.GetCellLocalPos(gridPos.x + 1, gridPos.y + 1);
                    break;
            }

            return Vector2.Lerp(a, b, 0.5f);
        }

        // グリッド座標とエッジ終点から次のグリッド座標を求める
        private Vector2Int GetNextGridPos(Vector2Int gridPos, EdgePointType endPoint)
        {
            Vector2Int result = gridPos;

            switch (endPoint)
            {
                case EdgePointType.TOP:
                    result += new Vector2Int(0, 1);
                    break;

                case EdgePointType.BOTTOM:
                    result += new Vector2Int(0, -1);
                    break;

                case EdgePointType.LEFT:
                    result += new Vector2Int(-1, 0);
                    break;

                case EdgePointType.RIGHT:
                    result += new Vector2Int(1, 0);
                    break;
            }

            return result;
        }

        // 前回のエッジ生成情報から次のエッジを生成する
        private bool AddEdge(EdgeTraceInfo prev, out EdgeTraceInfo result)
        {
            result = new EdgeTraceInfo();

            // 1.現在のセルを左下とした2×2の矩形のパターンをビット列として求める
            // ビット位置は下位ビットから順に 左下→右下→右上→左上
            int marchingType = 0;
            marchingType = GetMarchingSquareType(prev.nextGridPos.x, prev.nextGridPos.y);

            // 2.矩形のパターンとエッジの始点からエッジ終点を求める
            EdgePointType endPoint;
            if (!GetEndEdgePoint(prev.nextStartPoint, marchingType, out endPoint))
                return false;

            // 3.エッジ終点を反転して次のエッジの始点を求める
            result.nextStartPoint = ReverseEdgePoint(endPoint);

            // 4.現在のグリッド座標とエッジ終点から次のグリッド座標を求める
            result.nextGridPos = GetNextGridPos(prev.nextGridPos, endPoint);

            // 5.現在のグリッド座標とエッジ終点から実際のエッジ頂点を求める
            result.edgePos = GetEdgePos(prev.nextGridPos, endPoint);

            // 探索済みセルに追加 (左下基準から右上基準に変換)
            _visited[prev.nextGridPos.y + 1, prev.nextGridPos.x + 1] = true;

            return true;
        }

        // 最初のグリッド座標からエッジループを作成する
        private bool GenerateEdgeLoop(Vector2Int startGridPos, out List<Vector2> edgeLoop)
        {
            edgeLoop = new List<Vector2>();

            // 最初のエッジ生成用情報を作成
            EdgeTraceInfo traceInfo;
            traceInfo.nextGridPos = startGridPos;
            traceInfo.nextStartPoint = EdgePointType.NONE;
            traceInfo.edgePos = Vector2.zero;

            EdgeTraceInfo firstResult = new EdgeTraceInfo();
            EdgeTraceInfo traceResult = new EdgeTraceInfo();

            for (int i = 0; i < _maxEdgePoint; ++i)
            {
                // エッジ生成
                if (!AddEdge(traceInfo, out traceResult))
                {
                    return false;
                }

                // 終了判定
                if (i == 0)
                {
                    // 最初のエッジ生成結果を保持する
                    firstResult = traceResult;
                }
                else
                {
                    // 最初のエッジ生成結果と同じになったら終了
                    // ※同じグリッド座標を二回通る可能性があるためグリッド座標の比較のみでは終了判定がとれない
                    if (firstResult.nextGridPos == traceResult.nextGridPos &&
                        firstResult.nextStartPoint == traceResult.nextStartPoint)
                    {
                        return true;
                    }
                }

                // 頂点を追加し次のエッジ生成の準備を行う
                edgeLoop.Add(traceResult.edgePos);
                traceInfo = traceResult;
            }

            return false;
        }

        // 符号付き面積を求める
        float GetSignedArea(List<Vector2> edgeLoop)
        {
            float area = 0.0f;

            for (int i = 0; i < edgeLoop.Count; ++i)
            {
                Vector2 a = edgeLoop[i];
                Vector2 b = edgeLoop[(i + 1) % edgeLoop.Count];

                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }
    }
}
