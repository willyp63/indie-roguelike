using System;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField]
    private UnitType type = UnitType.Neutral;

    public UnitType Type()
    {
        return type;
    }

    public void SetUnitType(UnitType newType)
    {
        type = newType;
    }

    public UnitType OppositeType()
    {
        return UnitUtils.GetOppositeUnitType(type);
    }

    public bool IsNeutral()
    {
        return type == UnitType.Neutral;
    }

    public bool IsEnemy()
    {
        return type == UnitType.Enemy;
    }

    public bool IsFriend()
    {
        return type == UnitType.Friend;
    }

    [SerializeField]
    private int scaleFactor = 1;

    public int ScaleFactor()
    {
        return scaleFactor;
    }

    public void SetScaleFactor(int newScaleFactor)
    {
        scaleFactor = newScaleFactor;
        transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
        rb.mass = originalMass * Mathf.Pow(scaleFactor, 2);
    }

    [SerializeField]
    private int maxHealth = 100;

    public int MaxHealth()
    {
        return maxHealth;
    }

    private float hitBoxRadius;

    public float HitBoxRadius()
    {
        return hitBoxRadius * scaleFactor;
    }

    private int currentHealth;

    public int CurrentHealth()
    {
        return currentHealth;
    }

    public bool IsDead()
    {
        return currentHealth <= 0 && !isImmortal;
    }

    public bool IsFullHealth()
    {
        return currentHealth >= maxHealth;
    }

    [SerializeField]
    private bool isImmortal = false;

    [NonSerialized]
    public UnityEvent<int> onDamageTaken;

    [NonSerialized]
    public UnityEvent<int> onHealed;

    [NonSerialized]
    public UnityEvent onDeath;

    private float originalMass;
    private Rigidbody2D rb;

    void Awake()
    {
        currentHealth = maxHealth;

        rb = GetComponent<Rigidbody2D>();
        originalMass = rb.mass;

        hitBoxRadius = GetComponent<CircleCollider2D>().radius;

        SetScaleFactor(scaleFactor);

        onDamageTaken ??= new UnityEvent<int>();
        onHealed ??= new UnityEvent<int>();
        onDeath ??= new UnityEvent();
    }

    public void Damage(int damage)
    {
        if (IsDead())
            return;

        if (damage < 0)
        {
            Debug.LogError("Damage cannot be negative.");
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        onDamageTaken?.Invoke(damage);

        // check if dead
        if (IsDead())
        {
            currentHealth = 0;
            onDeath?.Invoke();
        }
    }

    // Method to heal
    public void Heal(int healAmount)
    {
        if (IsDead())
            return;

        if (healAmount < 0)
        {
            Debug.LogError("Heal amount cannot be negative.");
            return;
        }

        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        onHealed?.Invoke(healAmount);
    }
}
