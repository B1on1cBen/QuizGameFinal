[System.Serializable]
public class HighScore
{
    public string name = "";
    public int score = 0;

	public HighScore(string name, int score)
	{
		this.name = name;
		this.score = score;
	}
}

[System.Serializable]
public class HighScores
{
    public HighScore[] highScores;

	public HighScores(HighScore[] highScores)
	{
		this.highScores = highScores;
	}
}
