public class ResultData
{
    public float timePlayed;
    public int wavesCompleted;
    public int sessionNormalEssence;
    public int sessionOtherWorldEssence;

    private static ResultData instance;

    public static void SetData(float timePlayed, int wavesCompleted, int normalEssence, int otherWorldEssence)
    {
        instance = new ResultData
        {
            timePlayed = timePlayed,
            wavesCompleted = wavesCompleted,
            sessionNormalEssence = normalEssence,
            sessionOtherWorldEssence = otherWorldEssence
        };

    }

    public static ResultData GetData()
    {
        return instance ?? new ResultData();
    }
}

