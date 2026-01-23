using UnityEngine;
using System.Collections.Generic;

public class HelperSpirit : MonoBehaviour
{
    public enum SpiritState
    {
        Idle,
        Engaging,
        Attacking,
        Dashing,
        Returning,
        Despawning
    }

    [Header("Settings")]
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float orbitSpeed = 2f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float attackDamagePercent = 0.33f;
    [SerializeField] private float despawnDuration = 0.5f;

    private SpiritState _currentState = SpiritState.Idle;
    private BaseNode _parentNode;
    private Monster _targetMonster;
    private float _nextAttackTime;
    private float _orbitAngle;
    private Vector3 _startScale;
    private bool _isDespawning = false;
    private Vector3 _dashTargetPos;

    public void Initialize(BaseNode parentNode)
    {
        _parentNode = parentNode;
        _startScale = transform.localScale;
        transform.localScale = Vector3.zero;
        StartCoroutine(ScaleUp());
    }

    private System.Collections.IEnumerator ScaleUp()
    {
        float elapsed = 0f;
        while (elapsed < despawnDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, _startScale, elapsed / despawnDuration);
            yield return null;
        }
        transform.localScale = _startScale;
    }

    public void Despawn()
    {
        if (_isDespawning) return;
        _isDespawning = true;
        _currentState = SpiritState.Despawning;
        StartCoroutine(ScaleDownAndDestroy());
    }

    private System.Collections.IEnumerator ScaleDownAndDestroy()
    {
        float elapsed = 0f;
        Vector3 currentScale = transform.localScale;
        while (elapsed < despawnDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(currentScale, Vector3.zero, elapsed / despawnDuration);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void Update()
    {
        if (_isDespawning) return;

        switch (_currentState)
        {
            case SpiritState.Idle:
                UpdateIdle();
                break;
            case SpiritState.Engaging:
                UpdateEngaging();
                break;
            case SpiritState.Attacking:
                UpdateAttacking();
                break;
            case SpiritState.Dashing:
                UpdateDashing();
                break;
            case SpiritState.Returning:
                UpdateReturning();
                break;
        }
    }

    private void UpdateIdle()
    {
        OrbitAround(GetTargetCenter(_parentNode.gameObject));
        FindTarget();
    }

    private void UpdateEngaging()
    {
        if (_targetMonster == null || _targetMonster.IsDead)
        {
            _currentState = SpiritState.Returning;
            return;
        }

        Vector3 targetCenter = GetTargetCenter(_targetMonster.gameObject);
        float distance = Vector3.Distance(transform.position, targetCenter);

        if (distance <= orbitRadius + 0.5f)
        {
            _currentState = SpiritState.Attacking;
            _nextAttackTime = Time.time + attackInterval;
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, targetCenter, moveSpeed * Time.deltaTime);
        }
    }

    private void UpdateAttacking()
    {
        if (_targetMonster == null || _targetMonster.IsDead)
        {
            _targetMonster = null;
            _currentState = SpiritState.Returning;
            return;
        }

        Vector3 targetCenter = GetTargetCenter(_targetMonster.gameObject);
        OrbitAround(targetCenter);

        if (Time.time >= _nextAttackTime)
        {
            _dashTargetPos = targetCenter;
            _currentState = SpiritState.Dashing;
        }

        // Check if still in range, else return
        if (Vector3.Distance(transform.position, targetCenter) > detectionRadius * 1.5f)
        {
            _targetMonster = null;
            _currentState = SpiritState.Returning;
        }
    }

    private void UpdateDashing()
    {
        if (_targetMonster == null || _targetMonster.IsDead)
        {
            _currentState = SpiritState.Returning;
            return;
        }

        Vector3 targetCenter = GetTargetCenter(_targetMonster.gameObject);
        transform.position = Vector3.MoveTowards(transform.position, targetCenter, dashSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetCenter) < 0.1f)
        {
            Attack();
            _currentState = SpiritState.Attacking;
            _nextAttackTime = Time.time + attackInterval;
        }
    }

    private void UpdateReturning()
    {
        Vector3 targetCenter = GetTargetCenter(_parentNode.gameObject);
        float distance = Vector3.Distance(transform.position, targetCenter);

        if (distance <= 0.1f)
        {
            _currentState = SpiritState.Idle;
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, targetCenter, moveSpeed * Time.deltaTime);
        }
        
        // While returning, still look for targets
        FindTarget();
    }

    private void OrbitAround(Vector3 center)
    {
        _orbitAngle += orbitSpeed * Time.deltaTime;
        Vector3 offset = new Vector3(Mathf.Cos(_orbitAngle), 0.5f, Mathf.Sin(_orbitAngle)) * orbitRadius;
        transform.position = Vector3.Lerp(transform.position, center + offset, Time.deltaTime * 5f);
    }

    private Vector3 GetTargetCenter(GameObject obj)
    {
        if (obj == null) return Vector3.zero;
        
        // Try to find a renderer to get the true center of the visual
        var renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.center;
        }
        
        return obj.transform.position;
    }

    private void FindTarget()
    {
        if (MonsterAiManager.Instance == null) return;

        List<Monster> monsters = MonsterAiManager.Instance.ActiveMonsters;
        Monster closest = null;
        float minDistance = detectionRadius;

        foreach (var monster in monsters)
        {
            if (monster == null || monster.IsDead) continue;
            float dist = Vector3.Distance(_parentNode.transform.position, monster.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = monster;
            }
        }

        if (closest != null)
        {
            _targetMonster = closest;
            _currentState = SpiritState.Engaging;
        }
    }

    private void Attack()
    {
        if (_targetMonster == null) return;

        float playerDamage = 1f;
        if (PlayerProgressController.Instance != null)
        {
            playerDamage = PlayerProgressController.Instance.GetMonsterDamage();
        }

        float damage = playerDamage * attackDamagePercent;
        _targetMonster.TakeDamage(damage, transform.position);
        
        // Visual feedback (optional: could spawn a small particle effect here)
        Debug.Log($"HelperSpirit attacking {_targetMonster.name} for {damage} damage.");
    }
}
