using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    enum UnitState
    {
        Idle,
        Pursuing,
        Attacking,
        Dying,
    };

    static readonly float ATTACK_UPDATE_INTERVAL = 0.2f;

    [SerializeField]
    private bool isStatic = false;

    [SerializeField]
    private float visionRange = 8;

    private Health health;

    public Health Health()
    {
        return health;
    }

    private AttackBehaviour[] attackBehaviours;

    public AttackBehaviour[] AttackBehaviours()
    {
        return attackBehaviours;
    }

    private MovementBehaviour movementBehaviour;

    public MovementBehaviour MovementBehaviour()
    {
        return movementBehaviour;
    }

    private Animator animator;

    private Unit targetUnit;
    private GameObject pursueTarget;

    private float lastAttackTime = Mathf.NegativeInfinity;
    private float lastAttackDuration = 0f;
    private float lastAttackCheckAt = Mathf.NegativeInfinity;

    private UnitState state = UnitState.Idle;

    private void Start()
    {
        health = GetComponent<Health>();
        attackBehaviours = GetComponents<AttackBehaviour>();
        movementBehaviour = GetComponent<MovementBehaviour>();
        animator = GetComponent<Animator>();

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

        if (Time.time - lastAttackTime < lastAttackDuration)
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

                lastAttackTime = Time.time;
                lastAttackDuration = attackBehaviour.AttackDuration();

                FacePosition(attackTarget.transform.position);
                UpdateState(UnitState.Attacking, attackBehaviour.AnimationTrigger(), true);

                attackBehaviour.Attack(attackTarget);
                return;
            }
        }

        if (isStatic)
            return;

        if (pursueTarget == null)
        {
            UpdateState(UnitState.Idle, "Idle");
            return;
        }

        FacePosition(pursueTarget.transform.position);
        UpdateState(UnitState.Pursuing, "Walk");

        movementBehaviour.Move(pursueTarget);
    }

    public void UpdateTargetFromManager()
    {
        Debug.Log("UpdateTargetFromManager");
        if (isStatic)
            return;

        targetUnit = UnitManager.Instance.FindNearestVisibleTarget(this, visionRange);
        pursueTarget = targetUnit?.gameObject ?? UnitManager.Instance.FindBestWaypoint(this);
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
