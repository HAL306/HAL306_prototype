using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Terrain
{
    // 分離された地形データと元の座標からのオフセットを保持する構造体
    public struct SplitResult
    {
        public TerrainGridData GridData;
        public Vector2Int Offset;

        public SplitResult(TerrainGridData gridData, Vector2Int offset)
        {
            GridData = gridData;
            Offset = offset;
        }
    }
    
    public class TerrainSplitDetector : Singleton<TerrainSplitDetector>
    {
        // 探索ごとの訪問フラグを初期化コストなしで管理するための世代カウンター
        private int _currentGen;

    // 4方向の隣接セルを起点とした探索用のキュー、チャンクリスト、エイリアスの事前確保
    private Queue<int>[] _queues;
    private List<int>[] _cellsInChunk;
    private List<int> _mainChunkBuffer;
    private int[] _alias;

    protected TerrainSplitDetector()
    {
        // 実行時のメモリアロケーションを回避するための初期キャパシティ設定
        int initialCapacity = 1024;
        _queues = new Queue<int>[4];
        _cellsInChunk = new List<int>[4];
        _alias = new int[4];
        _mainChunkBuffer = new List<int>(initialCapacity);

        for (int i = 0; i < 4; i++)
        {
            _queues[i] = new Queue<int>(initialCapacity);
            _cellsInChunk[i] = new List<int>(initialCapacity);
        }
    }

    // 指定座標のセルを削除し地形が分離した場合はそれぞれのチャンクを抽出するメイン処理
    public List<SplitResult> RemoveAndCheckSplit(TerrainGridData grid, int removeX, int removeY)
    {
        List<SplitResult> separatedChunks = new List<SplitResult>();

        if (!grid.InBounds(removeX, removeY)) return separatedChunks;

        GridCell[] rawCells = grid.RawCells;
        int gridWidth = grid.Width;
        int removeIdx = removeY * gridWidth + removeX;
        
        if (!rawCells[removeIdx].solid) return separatedChunks;

        rawCells[removeIdx] = default;

        // オーバーフロー時のVisitedGen配列の安全なリセット処理
        if (++_currentGen == int.MaxValue)
        {
            Array.Clear(grid.VisitedGen, 0, grid.VisitedGen.Length);
            _currentGen = 1;
        }

        int activeRoots = 0;
        
        // 削除されたセルの上下左右を起点として独立した探索ルートを構築
        for (int i = 0; i < 4; i++)
        {
            _queues[i].Clear();
            _cellsInChunk[i].Clear();
            _alias[i] = -1;

            int nx = removeX;
            int ny = removeY;
            int nIdx = removeIdx;

            if (i == 0)      { ny--; nIdx -= gridWidth; } 
            else if (i == 1) { ny++; nIdx += gridWidth; } 
            else if (i == 2) { nx--; nIdx -= 1; }         
            else if (i == 3) { nx++; nIdx += 1; }         

            if (!grid.InBounds(nx, ny)) continue;

            // 起点が実体を持つ場合のみ探索ルートとして登録
            if (rawCells[nIdx].solid)
            {
                _alias[i] = i;
                _queues[i].Enqueue(nIdx);
                _cellsInChunk[i].Add(nIdx);
                grid.VisitedGen[nIdx] = _currentGen;
                grid.VisitedId[nIdx] = i;
                activeRoots++;
            }
        }

        // 起点が1つ以下の場合は分離が発生しないため早期リターン
        if (activeRoots <= 1) return separatedChunks;

        // 複数のルートがアクティブな間、並列に幅優先探索(BFS)を進行
        while (activeRoots > 1)
        {
            for (int i = 0; i < 4; i++)
            {
                if (_alias[i] != i) continue;

                // キューが空になったルートは他ルートと未接続の独立したチャンクとして抽出
                if (_queues[i].Count == 0)
                {
                    separatedChunks.Add(ExtractChunk(_cellsInChunk[i], grid));
                    _alias[i] = -1;
                    activeRoots--;
                    
                    if (activeRoots <= 1) break;
                    continue;
                }

                int currIdx = _queues[i].Dequeue();
                int cx = currIdx % gridWidth;
                int cy = currIdx / gridWidth;

                // 隣接する4方向への探索拡張処理
                TryExpand(currIdx - gridWidth, cx, cy - 1, grid, rawCells, ref i, ref activeRoots);
                TryExpand(currIdx + gridWidth, cx, cy + 1, grid, rawCells, ref i, ref activeRoots);
                TryExpand(currIdx - 1, cx - 1, cy, grid, rawCells, ref i, ref activeRoots);
                TryExpand(currIdx + 1, cx + 1, cy, grid, rawCells, ref i, ref activeRoots);

                if (activeRoots <= 1) break;
            }
        }

        // 分離が発生した場合の残余チャンクの抽出処理
        if (separatedChunks.Count > 0)
        {
            _mainChunkBuffer.Clear();
            for (int i = 0; i < rawCells.Length; i++)
            {
                if (rawCells[i].solid)
                {
                    _mainChunkBuffer.Add(i);
                }
            }

            if (_mainChunkBuffer.Count > 0)
            {
                separatedChunks.Add(ExtractChunk(_mainChunkBuffer, grid));
            }
        }

        return separatedChunks;
    }

        // 隣接セルの評価と他ルートとの衝突検知時のマージ処理
        private void TryExpand(int nIdx, int nx, int ny, TerrainGridData grid, GridCell[] rawCells, ref int myRoot,
            ref int activeRoots)
        {
            if (!grid.InBounds(nx, ny)) return;
            if (!rawCells[nIdx].solid) return;

            // 既に訪問済みの場合は所属ルートの衝突確認と統合処理
            if (grid.VisitedGen[nIdx] == _currentGen)
            {
                int otherRoot = GetRoot(_alias, grid.VisitedId[nIdx]);
                if (otherRoot != -1 && otherRoot != myRoot)
                {
                    myRoot = MergeRoots(myRoot, otherRoot, ref activeRoots);
                }
            }
            else
            {
                grid.VisitedGen[nIdx] = _currentGen;
                grid.VisitedId[nIdx] = myRoot;

                _queues[myRoot].Enqueue(nIdx);
                _cellsInChunk[myRoot].Add(nIdx);
            }
        }

        // Union-Findアルゴリズムに基づく所属ルートの走査
        private int GetRoot(int[] alias, int id)
        {
            int root = id;
            while (alias[root] != -1 && alias[root] != root)
            {
                root = alias[root];
            }

            return root;
        }

        // 衝突した2つの探索ルートの統合と保有データの結合処理
        private int MergeRoots(int rootA, int rootB, ref int activeRoots)
        {
            _alias[rootA] = rootB;

            var listA = _cellsInChunk[rootA];
            var listB = _cellsInChunk[rootB];
            for (int i = 0; i < listA.Count; i++) listB.Add(listA[i]);
            listA.Clear();

            var queueA = _queues[rootA];
            var queueB = _queues[rootB];
            while (queueA.Count > 0) queueB.Enqueue(queueA.Dequeue());

            activeRoots--;
            return rootB;
        }

        // 分離したチャンクのバウンディングボックス計算と新規グリッドへの移譲処理
        private SplitResult ExtractChunk(List<int> chunkCells, TerrainGridData originalGrid)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            int origWidth = originalGrid.Width;

            // チャンク内セルの最小・最大座標の走査
            for (int i = 0; i < chunkCells.Count; i++)
            {
                int idx = chunkCells[i];
                int x = idx % origWidth;
                int y = idx / origWidth;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            int newWidth = maxX - minX + 1;
            int newHeight = maxY - minY + 1;

            TerrainGridData newGrid = new TerrainGridData(newWidth, newHeight, originalGrid.GridScale);

            GridCell[] origRawCells = originalGrid.RawCells;
            GridCell[] newRawCells = newGrid.RawCells;

            // 新規グリッドへのセルの再配置と元グリッドからの消去
            for (int i = 0; i < chunkCells.Count; i++)
            {
                int idx = chunkCells[i];
                int x = idx % origWidth;
                int y = idx / origWidth;

                int newIdx = (y - minY) * newWidth + (x - minX);

                newRawCells[newIdx] = origRawCells[idx];
                origRawCells[idx] = default;
            }

            return new SplitResult(newGrid, new Vector2Int(minX, minY));
        }
    }
}