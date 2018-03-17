using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class AcceptButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] Image shadow;
    [SerializeField] Color shadowEnterColor;
    Color shadowExitColor;

    [Space]

    [SerializeField] Image front;
    [SerializeField] Color frontEnterColor;
    Color frontExitColor;

    [Space]

    [SerializeField] Image arrow;
    [SerializeField] Sprite arrowEnter;
    [SerializeField] Sprite arrowExit;

    [Space]

    [SerializeField] AudioClip enterSound;
    [SerializeField] AudioClip clickSound;
    AudioSource audioSource;

    [Space]

    [SerializeField] UnityEvent OnClick;

    void Start() {
        audioSource = GameObject.Find("Canvas").GetComponent<AudioSource>();
        shadowExitColor = shadow.color;
        frontExitColor = front.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        arrow.sprite = arrowEnter;
        audioSource.PlayOneShot(enterSound);
        shadow.color = shadowEnterColor;
        front.color = frontEnterColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        arrow.sprite = arrowExit;
        shadow.color = shadowExitColor;
        front.color = frontExitColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        audioSource.PlayOneShot(clickSound);
        OnClick.Invoke();
    }
}
