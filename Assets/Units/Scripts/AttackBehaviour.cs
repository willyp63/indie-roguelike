using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AttackBehaviour : MonoBehaviour
{
    [SerializeField]
    private bool isBasicAttack = true;

    public bool IsBasicAttack()
    {
        return isBasicAttack;
    }

    [SerializeField]
    private int attackDamage = 10;

    public int AttackDamage()
    {
        return attackDamage;
    }

    [SerializeField]
    protected float attackCooldown = 0f;

    public float AttackCooldown()
    {
        return attackCooldown;
    }

    [SerializeField]
    protected float attackDuration = 1f;

    public float AttackDuration()
    {
        return attackDuration;
    }

    [SerializeField]
    protected bool isRanged = false;

    public bool IsRanged()
    {
        return isRanged;
    }

    [SerializeField]
    protected float attackRange = 0f;

    public float AttackRange()
    {
        return attackRange;
    }

    [SerializeField]
    protected float attackDelay = 0.5f;

    public float AttackDelay()
    {
        return attackDelay;
    }

    [SerializeField]
    protected string animationTrigger = "Attack";

    public string AnimationTrigger()
    {
        return animationTrigger;
    }

    [SerializeField]
    protected float pushForce = 0f; // negative for pull

    protected Health health;

    private float lastAttackTime = Mathf.NegativeInfinity;
    private Coroutine attackCoroutine;

    private void Start()
    {
        health = GetComponent<Health>();
    }

    public virtual Health GetTarget(Unit targetUnit)
    {
        if (targetUnit != null && UnitUtils.IsWithinRange(health, targetUnit.Health(), attackRange))
            return targetUnit.Health();

        return null;
    }

    public virtual bool IsOnCooldown()
    {
        return Time.time - lastAttackTime < attackCooldown;
    }

    public virtual bool IsActive()
    {
        return Time.time - lastAttackTime < attackDuration;
    }

    public void Attack(Health target)
    {
        lastAttackTime = Time.time;

        // Cancel any existing attack coroutine before starting a new one
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }

        attackCoroutine = StartCoroutine(AreaAttackAfterDelay(target));
    }

    public void CancelAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
    }

    private IEnumerator AreaAttackAfterDelay(Health target)
    {
        yield return new WaitForSeconds(attackDelay);

        // Check that the unit is still alive and the target is still alive
        if (!health.IsDead() && target != null && !target.IsDead())
            PerformAttack(target);

        // Clear the coroutine reference when it completes naturally
        attackCoroutine = null;
    }

    protected abstract void PerformAttack(Health target);
}
