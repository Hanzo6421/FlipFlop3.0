using UnityEngine;
using UnityEngine.Events;

namespace FlipFlop.Game.Shared
{
    public class Health : MonoBehaviour
    {
        [Tooltip("Maximum amount of health")]
        public float maxHealth = 100f;

        [Tooltip("Health ratio at which the critical health vignette starts appearing")]
        public float criticalHealthRatio = 0.3f;

        public UnityAction<float, GameObject> OnDamaged;
        public UnityAction<float> OnHealed;
        public UnityAction OnDie;

        public float currentHealth { get; set; }
        public bool invincible { get; set; }
        public bool CanPickup() => currentHealth < maxHealth;

        public float GetRatio() => currentHealth / maxHealth;
        public bool IsCritical() => GetRatio() <= criticalHealthRatio;

        private bool isDead;

        void Start()
        {
            currentHealth = maxHealth;
        }

        public void Heal(float healAmount)
        {
            float healthBefore = currentHealth;
            currentHealth += healAmount;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            // call OnHeal action
            float trueHealAmount = currentHealth - healthBefore;
            if (trueHealAmount > 0f)
            {
                OnHealed?.Invoke(trueHealAmount);
            }
        }

        public void TakeDamage(float damage, GameObject damageSource)
        {
            if (invincible)
                return;

            float healthBefore = currentHealth;
            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

            // call OnDamage action
            float trueDamageAmount = healthBefore - currentHealth;
            if (trueDamageAmount > 0f)
            {
                OnDamaged?.Invoke(trueDamageAmount, damageSource);
            }

            HandleDeath();
        }

        public void Kill()
        {
            currentHealth = 0f;

            // call OnDamage action
            OnDamaged?.Invoke(maxHealth, null);

            HandleDeath();
        }

        void HandleDeath()
        {
            if (isDead)
                return;

            // call OnDie action
            if (currentHealth <= 0f)
            {
                isDead = true;
                OnDie?.Invoke();
            }
        }
    }
}