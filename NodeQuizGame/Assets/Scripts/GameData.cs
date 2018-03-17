[System.Serializable]
public class QandA
{
	public string question;
	public string[] answers;
	public int correctIndex;
}

[System.Serializable]
public class GameData
{
	public QandA[] questionsAndAnswers;
}


