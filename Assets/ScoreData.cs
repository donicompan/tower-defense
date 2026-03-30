using System;
using System.Collections.Generic;

[Serializable]
public class RunRecord
{
    public int    score;
    public int    wave;
    public int    goldEarned;
    public int    enemiesKilled;
    public string date;
}

[Serializable]
public class Leaderboard
{
    public List<RunRecord> entries = new List<RunRecord>();
}
