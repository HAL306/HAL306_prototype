using Unity.Mathematics;
using UnityEngine;

namespace Game.Terrain
{
    public class TerrainGridData
    {
        private GridCell[] _grid;   // グリッドデータ

        private int _width;         // グリッドの幅
        private int _height;        // グリッドの高さ
        private float _gridScale;   // 1マスのサイズ
        private int[] _visitedGen;
        private int[] _visitedId;

        public int Width => _width;
        public int Height => _height;
        public float GridScale => _gridScale;
        public int[] VisitedGen => _visitedGen;
        public int[] VisitedId => _visitedId;
        public GridCell[] RawCells => _grid;
        

        public TerrainGridData(int width, int height, float gridScale)
        {
            _width = width;
            _height = height;
            _gridScale = gridScale;
            
            int capacity = width * height;
            _visitedGen = new int[capacity];
            _visitedId = new int[capacity];
            _grid = new GridCell[capacity];
        }


        // グリッドデータにセルをセットする
        public void Set(int x, int y, GridCell cell)
        {
            _grid[Index(x, y)] = cell;
        }

        // 指定したグリッド座標のセルを取得する
        public ref GridCell Get(int x, int y)
        {
            return ref _grid[Index(x, y)];
        }

        // 指定したグリッド座標がグリッド内に収まっているか調べる
        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }


        // グリッド座標をデータ取得用インデックスに変換する
        private int Index(int x, int y)
        {
            return x + _width * y;
        }
    }

}