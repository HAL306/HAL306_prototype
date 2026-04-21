using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//RamerDouglasPeuckeralgorithmのテスト用スクリプト
public class NewBehaviourScript : MonoBehaviour
{
    //簡略化の許容誤差
    //publicで宣言されているのでUnityエディタ上で調整してくださいby石原
    public float epsilon = 0.1f;
    public bool isTestLine = true; // テスト用の線を使用するかどうかのフラグ
    public bool isTestdisc = true; // テスト用の円を使用するかどうかのフラグ
    private bool isSimplified = true; // 簡略化されたかどうかのフラグ
    List<Vector2> original = new List<Vector2>();   // 元の線
    List<Vector2> simplified = new List<Vector2>(); // 簡略化後の線

    void Start()
    {
        if(isTestLine)
        {
           //テスト用の線
           original.Add(new Vector2(0, 0));
           original.Add(new Vector2(1, 0.2f));
           original.Add(new Vector2(2, -0.1f));
           original.Add(new Vector2(3, 0));
           original.Add(new Vector2(4, 1));
           original.Add(new Vector2(5, 1.1f));
           original.Add(new Vector2(6, 1));
           original.Add(new Vector2(7, 0.5f));
           original.Add(new Vector2(8, 0.2f));
           original.Add(new Vector2(9, 0.1f));
           original.Add(new Vector2(8, -0.4f));
           original.Add(new Vector2(7, -0.7f));
           original.Add(new Vector2(6, -0.8f));
           original.Add(new Vector2(5, -1.3f));
           original.Add(new Vector2(4, -1.1f));
           original.Add(new Vector2(3, -1.5f));
           original.Add(new Vector2(2, -1.2f));
        }

        if (isTestdisc)
        {
            int segments = 50;   //円を構成する点の数
            float radius = 3f;   //半径

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;

                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;

                original.Add(new Vector2(x, y));
            }
        }
        simplified = RamerDouglasPeucker.RamerDouglasPeuckerAlgorithm(original, epsilon);
    }

    void Update()
    {

        //簡略化後（赤）
        for (int i = 0; i < simplified.Count - 1; i++)
        {
            Debug.DrawLine(simplified[i], simplified[i + 1], Color.red);
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (isSimplified) { isSimplified = false; }
            else { isSimplified = true; }
        }
        //比較用の元の線（白）
        if (isSimplified)
        {
            for (int i = 0; i < original.Count - 1; i++)
            {
                Debug.DrawLine(original[i], original[i + 1], Color.white);
            }
        }
    }
}

