using UnityEngine;
using UnityEngine.InputSystem;

public class SamplePlayer : MonoBehaviour
{
    [SerializeField]
    private Rigidbody2D _rigidbody;

    [SerializeField]
    private InputActionReference _move;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        //float inputMove = InputSystem.actions.FindAction()
    }
}
