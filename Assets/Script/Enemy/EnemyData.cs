using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public float maxHealth;
    public float defense;
    public float moveSpeed;
    public int damageToCore;
    public WorldState originWorld;

    // Si quer�s m�s control:
    public EnemyType type; // Enum opcional si quer�s diferenciar visualmente
}
