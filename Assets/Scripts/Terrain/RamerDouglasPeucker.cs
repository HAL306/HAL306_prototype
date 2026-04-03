using System.Collections.Generic;
using UnityEngine;
// RamerDouglasPeuckeralgorithm
// ポリラインを簡略化するアルゴリズム。
// 始点と終点を結ぶ直線から最も離れている点を探し、
// その距離が引数epsilonより大きい場合はその点を残して再帰的に処理する。
//epsilon以下の場合は中間点をすべて削除し、始点と終点のみを残す。
//2026/03/18 石原作成
public static class RamerDouglasPeucker
{
    public static List<Vector2> RamerDouglasPeuckerAlgorithm(List<Vector2> point, float epsilon)
    {
        //点が3点未満の場合終了
        if (point == null || point.Count < 3)
            return new List<Vector2>(point);
        //最大距離用indexの初期化
        int index = -1;
        //最大距離を格納するディスタンス
        float maxDistance = 0f;

        //終了点
        Vector2 end = point[point.Count - 1];
        //開始点追加
        Vector2 start = point[0];

        //最も離れている点を探す
        for (int i = 1; i < point.Count - 1; i++)
        {
            //点と線の垂直距離を求める
            float distance = UprightDistance(point[i], start, end);

            //最大距離更新
            if (distance > maxDistance)
            {
                maxDistance = distance;
                index = i;
            }
        }
        //===========================================
        //引数epsilonより大きい場合は分割して再帰処理
        //===========================================
        //最大距離がepsilonより大きい場合
        if (maxDistance > epsilon)
        {
            //public List<T> GetRange(int index, int count);
            //再起処理左右
            List<Vector2> left = RamerDouglasPeuckerAlgorithm(point.GetRange(0, index + 1), epsilon);
            List<Vector2> right = RamerDouglasPeuckerAlgorithm(point.GetRange(index, point.Count - index), epsilon);

            //重複した場合は削除して結合
            List<Vector2> result = new List<Vector2>(left);
            //リストに追加する（重複する点を削除して追加）
            result.AddRange(right.GetRange(1, right.Count - 1));

            return result;
        }
        else
        {
            //もし最大距離がepsilon以下の場合は開始点と終了点のみを返す（誤差epsilon以下の直線状になっていた場合）
            return new List<Vector2> { start, end };
        }
    }


    //垂直距離を求める関数
    private static float UprightDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        //線が点と同じ場合は距離を求める
        if (lineStart == lineEnd)
            return Vector2.Distance(point, lineStart);
        //点と線の距離を求める公式
        //--------------------------------------------
        //比較するためにMathf.Absを使用して絶対値を取る
        //--------------------------------------------
        //点と線の距離を求める公式は点と線の両端を結ぶ三角形の面積を底辺の長さで割ることで求められる。らしい
        //分子・三角形の面積
        float numerator = Mathf.Abs(
            (lineEnd.x - lineStart.x) * (lineStart.y - point.y) -
            (lineStart.x - point.x) * (lineEnd.y - lineStart.y)
        );
        //分母・底辺の長さ
        float denominator = Vector2.Distance(lineStart, lineEnd);

        //計算結果を返す
        return numerator / denominator;
    }
}