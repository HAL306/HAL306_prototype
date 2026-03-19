using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Terrain
{
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
        private int _currentGen;

    private Queue<int>[] _queues;
    private List<int>[] _cellsInChunk;
    private List<int> _mainChunkBuffer;
    private int[] _alias;

    protected TerrainSplitDetector()
    {
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

    public List<SplitResult> RemoveAndCheckSplit(TerrainGridData grid, int removeX, int removeY)
    {
        List<SplitResult> separatedChunks = new List<SplitResult>();

        if (!grid.InBounds(removeX, removeY)) return separatedChunks;

        GridCell[] rawCells = grid.RawCells;
        int gridWidth = grid.Width;
        int removeIdx = removeY * gridWidth + removeX;
        
        if (!rawCells[removeIdx].solid) return separatedChunks;

        rawCells[removeIdx] = default;

        if (++_currentGen == int.MaxValue)
        {
            Array.Clear(grid.VisitedGen, 0, grid.VisitedGen.Length);
            _currentGen = 1;
        }

        int activeRoots = 0;
        
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

        if (activeRoots <= 1) return separatedChunks;

        while (activeRoots > 1)
        {
            for (int i = 0; i < 4; i++)
            {
                if (_alias[i] != i) continue;

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

                TryExpand(currIdx - gridWidth, cx, cy - 1, grid, rawCells, ref i, ref activeRoots);
                TryExpand(currIdx + gridWidth, cx, cy + 1, grid, rawCells, ref i, ref activeRoots);
                TryExpand(currIdx - 1, cx - 1, cy, grid, rawCells, ref i, ref activeRoots);
                TryExpand(currIdx + 1, cx + 1, cy, grid, rawCells, ref i, ref activeRoots);

                if (activeRoots <= 1) break;
            }
        }

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

        private void TryExpand(int nIdx, int nx, int ny, TerrainGridData grid, GridCell[] rawCells, ref int myRoot,
            ref int activeRoots)
        {
            if (!grid.InBounds(nx, ny)) return;
            if (!rawCells[nIdx].solid) return;

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

        private int GetRoot(int[] alias, int id)
        {
            int root = id;
            while (alias[root] != -1 && alias[root] != root)
            {
                root = alias[root];
            }

            return root;
        }

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

        private SplitResult ExtractChunk(List<int> chunkCells, TerrainGridData originalGrid)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            int origWidth = originalGrid.Width;

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