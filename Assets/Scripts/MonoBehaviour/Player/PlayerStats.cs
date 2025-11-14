using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int MaxHealth = 1000;
    public int CurrentHealth = 1000;
    public int MaxSoul = 500;
    public int CurrentSoul = 0;

    void Update()
    {
        if (CurrentHealth < 0)
            Die();
        if (CurrentSoul < 0)
            CurrentSoul = 0;

    }

    private void Die()
    {
        Debug.Log("Player died!");
        // Implement on player death logic here
    }
}
