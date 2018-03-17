using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{
	public int maxTime;
	[Space]
	[SerializeField] Image timerBar;
	[SerializeField] Image timerBarBack;
	[SerializeField] Text timerText;

	Color defaultFrontColor;
	Color defaultBackColor;
	int time;
	bool timerOn = false;

	void Start()
	{
		defaultFrontColor = timerBar.color;
		defaultBackColor = timerBarBack.color;
		Time = maxTime - 1;
	}

	IEnumerator TimerTick()
	{
		while (timerOn)
		{
			Time--;
			yield return new WaitForSeconds(1);
		}
	}

	public int Time
	{
		get { return time; }
		set {
			time = value;

			float percent = (float)time / (float)(maxTime - 1);

			timerBar.fillAmount = percent;

			if (time <= 5)
			{
				timerText.color = Color.red;
				timerBar.color = Color.red;
				timerBarBack.color = new Color(0.5f, 0, 0);
			}
			else
			{
				timerText.color = defaultFrontColor;
				timerBar.color = defaultFrontColor;
				timerBarBack.color = defaultBackColor;
			}

			if(time >= 0)
				timerText.text = time.ToString();
		}
	}

	public void ResetTimer()
	{
		Time = maxTime - 1;
	}

	public void StopTimer()
	{
		timerOn = false;
		StopCoroutine(TimerTick());
	}

	public void StartTimer()
	{
		Time = maxTime;
		timerOn = true;
		StartCoroutine(TimerTick());
	}
}
