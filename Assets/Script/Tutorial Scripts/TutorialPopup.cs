using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class TutorialPopup : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;       // Arka planı tamamen kaplayan (tıklamaları engelleyen) yarı saydam panel
    public TextMeshProUGUI titleText;   // Başlık
    public TextMeshProUGUI messageText; // Açıklama metni
    public Button continueButton;       // "Anladım / Devam" butonu

    private Action onCompleteCallback;  // Butona basıldığında çalışacak kod

    void Awake()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
        if (continueButton != null) continueButton.onClick.AddListener(ClosePopup);
    }

    // Dışarıdan çağrılacak fonksiyon (Başlık, Mesaj ve Butona basılınca ne olacağı)
    public void Show(string title, string message, Action onComplete = null)
    {
        titleText.text = title;
        messageText.text = message;
        onCompleteCallback = onComplete;

        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            popupPanel.transform.SetAsLastSibling(); // En üstte görünsün

            popupPanel.transform.localScale = Vector3.one * 0.85f;
            popupPanel.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);

            CanvasGroup cg = popupPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = popupPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.2f).SetUpdate(true);
        }
    }

    void ClosePopup()
    {
        if (popupPanel != null)
        {
            CanvasGroup cg = popupPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.DOFade(0f, 0.15f).SetUpdate(true);
            }
            popupPanel.transform.DOScale(Vector3.one * 0.85f, 0.15f).SetEase(Ease.InCubic).SetUpdate(true).OnComplete(() =>
            {
                popupPanel.SetActive(false);
                if (onCompleteCallback != null)
                {
                    onCompleteCallback.Invoke();
                }
            });
        }
        else
        {
            if (onCompleteCallback != null)
            {
                onCompleteCallback.Invoke();
            }
        }
    }
}