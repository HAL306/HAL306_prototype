using System;
using System.Reflection;

/// <summary>
/// 純粋なC#クラス用シングルトン基底クラス。
/// 遅延初期化とスレッドセーフを保証。
/// </summary>
public abstract class Singleton<T> where T : Singleton<T>
{
    // Lazy<T> によるスレッドセーフな遅延インスタンス生成
    private static readonly Lazy<T> _lazyInstance = new Lazy<T>(CreateInstance);

    /// <summary>
    /// シングルトンインスタンスにアクセスします。
    /// </summary>
    public static T Instance => _lazyInstance.Value;

    /// <summary>
    /// 継承先では必ず protected または private な引数なしコンストラクタを定義してください。
    /// </summary>
    protected Singleton() { }

    private static T CreateInstance()
    {
        // リフレクションを使って非公開コンストラクタを取得する（外部からの new T() を防ぐため）
        ConstructorInfo constructor = typeof(T).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic, 
            null, 
            Type.EmptyTypes, 
            null);

        if (constructor == null)
        {
            throw new InvalidOperationException($"[Singleton] {typeof(T).Name} には、非公開 (protected または private) の引数なしコンストラクタが必要です。");
        }

        return (T)constructor.Invoke(null);
    }
}