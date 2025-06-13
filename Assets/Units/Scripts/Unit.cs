using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public enum UnitState
    {
        Idle,
        Pursuing,
        Attacking,
        Dying,
    };

    static readonly float ATTACK_UPDATE_INTERVAL = 0.2f;

    [SerializeField]
    private int karmaValue = 0;

    public int KarmaValue()
    {
        return karmaValue;
    }

    [SerializeField]
    private bool isStatic = false;

    public bool IsStatic()
    {
        return isStatic;
    }

    [SerializeField]
    private float visionRange = 8;

    public float VisionRange()
    {
        return visionRange;
    }

    private Health health;

    public Health Health()
    {
        return health;
    }

    private AttackBehaviour basicAttack;
    private AttackBehaviour[] attackBehaviours;

    public AttackBehaviour BasicAttack()
    {
        return basicAttack;
    }

    public AttackBehaviour[] AttackBehaviours()
    {
        return attackBehaviours;
    }

    private MovementBehaviour movementBehaviour;

    public MovementBehaviour MovementBehaviour()
    {
        return movementBehaviour;
    }

    private Rigidbody2D rb;

    public Rigidbody2D Rigidbody()
    {
        return rb;
    }

    public UnitState State()
    {
        return state;
    }

    private Animator animator;

    private Unit targetUnit;
    private Vector2 moveDirection;

    private AttackBehaviour activeAttack;
    private float lastAttackCheckAt = Mathf.NegativeInfinity;

    private UnitState state = UnitState.Idle;

    private void Start()
    {
        health = GetComponent<Health>();

        attackBehaviours = GetComponents<AttackBehaviour>();
        basicAttack = System.Array.Find(attackBehaviours, attack => attack.IsBasicAttack());

        movementBehaviour = GetComponent<MovementBehaviour>();

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        health.onDeath.AddListener(OnDeath);

        UnitManager.Instance.RegisterUnit(this);
    }

    private void OnDestroy()
    {
        UnitManager.Instance?.UnregisterUnit(this);
    }

    private void FixedUpdate()
    {
        if (health.IsDead())
            return;

        if (health.IsBrokenCrystal())
        {
            activeAttack?.CancelAttack();
            return;
        }

        if (activeAttack?.IsActive() ?? false)
            return;

        if (Time.time - lastAttackCheckAt > ATTACK_UPDATE_INTERVAL)
        {
            lastAttackCheckAt = Time.time;

            foreach (var attackBehaviour in attackBehaviours)
            {
                if (attackBehaviour.IsOnCooldown())
                    continue;

                var attackTarget = attackBehaviour.GetTarget(targetUnit);
                if (attackTarget == null)
                    continue;

                activeAttack = attackBehaviour;

                FacePosition(attackTarget.transform.position);
                UpdateState(UnitState.Attacking, attackBehaviour.AnimationTrigger(), true);

                attackBehaviour.Attack(attackTarget);
                return;
            }
        }

        if (!isStatic && !moveDirection.Equals(Vector2.zero))
        {
            FacePosition(transform.position + new Vector3(moveDirection.x, moveDirection.y, 0f));
            UpdateState(UnitState.Pursuing, "Walk");
            movementBehaviour.Move(moveDirection);
            return;
        }

        UpdateState(UnitState.Idle, "Idle");
    }

    public void UpdateTargetFromManager()
    {
        // clear target if it's dead or out of range
        if (targetUnit != null)
        {
            if (
                targetUnit.Health().IsDead()
                || !UnitUtils.IsWithinRange(
                    health,
                    targetUnit.Health(),
                    basicAttack?.AttackRange() ?? visionRange
                )
            )
            {
                targetUnit = null;
                activeAttack?.CancelAttack();
            }
        }

        // look for a new target if we don't have one
        if (targetUnit == null)
        {
            targetUnit = UnitManager.Instance.FindNearestVisibleTarget(this);
        }

        if (isStatic)
            return;

        Vector2 targetDirection;
        float targetDistance = Mathf.Infinity;
        if (targetUnit != null)
        {
            // move towards target unit
            Vector2 toTarget = targetUnit.transform.position - transform.position;

            // dont move if we're in range of the target unit
            if (
                basicAttack != null
                && UnitUtils.IsWithinRange(health, targetUnit.Health(), basicAttack.AttackRange())
            )
            {
                moveDirection = Vector2.zero;
                return;
            }

            targetDirection = toTarget.normalized;
            targetDistance = toTarget.magnitude;
        }
        else
        {
            // move towards waypoint (precomputed pathfinding)
            targetDirection = WaypointManager.Instance.GetWaypointDirection(
                transform.position,
                health.Type()
            );
        }

        // adjust move direction to avoid other units
        moveDirection = UnitManager.Instance.GetMoveDirection(
            this,
            targetDirection,
            targetDistance
        );
    }

    private void UpdateState(
        UnitState newState,
        string animatorTrigger,
        bool forceRetrigger = false
    )
    {
        if (forceRetrigger || state != newState)
            animator?.SetTrigger(animatorTrigger);

        state = newState;
    }

    private void OnDeath()
    {
        GetComponent<CircleCollider2D>().enabled = false;

        if (animator != null)
            UpdateState(UnitState.Dying, "Die");
        else
            GetComponentInChildren<SpriteRenderer>().enabled = false;

        StartCoroutine(DestroyAfterAnimation());
    }

    private IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(1.0f);
        Destroy(gameObject);
    }

    private void FacePosition(Vector3 position)
    {
        var xScale = position.x - transform.position.x > 0 ? 1f : -1f;
        transform.localScale = new Vector3(xScale * health.ScaleFactor(), health.ScaleFactor(), 1f);

        switch (UnitUtils.GetDirection(transform.position, position))
        {
            case Direction.Down:
                animator?.SetInteger("Direction", 0);
                break;
            case Direction.Right:
            case Direction.Left:
                animator?.SetInteger("Direction", 1);
                break;
            case Direction.Up:
                animator?.SetInteger("Direction", 2);
                break;
        }
    }
}
