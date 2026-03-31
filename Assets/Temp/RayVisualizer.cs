using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.PlayerSettings;

public class RayVisualizer : MonoBehaviour
{
    LineRenderer line;

    [Header("Shoot")]
    [SerializeField] float shootDelay = 0.2f;
    float shootTimer = 0f;

    [SerializeField] float spread = 5f;
    [SerializeField] float distance = 100f;

    [SerializeField] GameObject hitEffectPrefab;
    [SerializeField] LayerMask hitLayer;
    void Start()
    {
        line = GetComponent<LineRenderer>();

        line.startColor = Color.red;
        line.endColor = Color.red;

        shootTimer = shootDelay;
    }

    void Update()
    {
        bool isShooting = Mouse.current.leftButton.isPressed;

        // 色変更
        SetLineColor(isShooting ? Color.yellow : Color.red);

        // Ray更新（常に描画）
        UpdateRay(false);

        // 射撃処理
        if (isShooting)
        {
            shootTimer -= Time.deltaTime;

            if (shootTimer <= 0f)
            {
                Shoot();
                shootTimer = shootDelay;
            }
        }
    }


    void UpdateRay(bool applySpread)
    {
        Vector3 myPos = transform.position;
        Vector3 mouseWorld = GetMouseWorld();

        Vector3 dir = mouseWorld - myPos;

        if (applySpread)
        {
            float randomAngle = Random.Range(-spread, spread);
            dir = Quaternion.Euler(0, 0, randomAngle) * dir;
        }

        float dist = dir.magnitude;
        dir.Normalize();

        RaycastHit2D hit = Physics2D.Raycast(myPos, dir, dist);

        Vector3 endPos = hit.collider != null
            ? (Vector3)hit.point
            : myPos + dir * dist;

        line.SetPosition(0, myPos);
        line.SetPosition(1, endPos);
    }

    void Shoot()
    {
        Vector3 myPos = transform.position;
        Vector3 mouseWorld = GetMouseWorld();

        Vector3 dir = mouseWorld - myPos;

        // ブレ
        float randomAngle = Random.Range(-spread, spread);
        dir = Quaternion.Euler(0, 0, randomAngle) * dir;
        dir.Normalize();

        RaycastHit2D hit = Physics2D.Raycast(myPos, dir, distance, hitLayer);

        Vector3 endPos;

        if (hit.collider != null)
        {
            endPos = hit.point;

            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, endPos, Quaternion.identity);
                Destroy(effect, 0.1f);
            }
        }
        else
        {
            endPos = myPos + dir * distance;
        }

        line.SetPosition(0, myPos);
        line.SetPosition(1, endPos);
    }


    Vector3 GetMouseWorld()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(mousePos);
        world.z = 0f;
        return world;
    }


    void SetLineColor(Color color)
    {
        line.startColor = color;
        line.endColor = color;
    }
}