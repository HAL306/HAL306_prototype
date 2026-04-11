using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent (typeof(Rigidbody2D))]
public class BossEnemyMove : MonoBehaviour
{
    [SerializeField, Tooltip("移動速度")]
    [Range(0.0f, 10.0f)]
    private float _moveSpeed = 1.0f;

    private Rigidbody2D _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        _rigidbody.linearVelocityX = _moveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }
}
