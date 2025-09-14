using UnityEngine;

public enum AttackMode
{
    Nearest = 0,
    Farthest = 1,
    LowestHealth = 2,
    HighestHealth = 3
}

public class AttackModeManager : MonoBehaviour
{
    public static AttackModeManager Instance;

    public AttackMode currentAttackMode = AttackMode.Nearest; //por default ataca al mas cercano

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetAttackMode(int modeIndex)
    {
        currentAttackMode = (AttackMode)modeIndex;
        Debug.Log("Modo de ataque seleccionado: " + currentAttackMode);
    }
}
