using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class TutorialPopup : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;       // Arka planý tamamen kaplayan (týklamalarý engelleyen) yarý saydam panel
    public TextMeshProUGUI titleText;   // Baþlýk
    public TextMeshProUGUI messageText; // Açýklama metni
    public Button continueButton;       // "Anladým / Devam" butonu

    private Action onCompleteCallback;  // Butona basýldýðýnda çalýþacak kod

    void Awake()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
        if (continueButton != null) continueButton.onClick.AddListener(ClosePopup);
    }

    // Dýþarýdan çaðrýlacak fonksiyon (Baþlýk, Mesaj ve Butona basýlýnca ne olacaðý)
    public void Show(string title, string message, Action onComplete = null)
    {
        titleText.text = title;
        messageText.text = message;
        onCompleteCallback = onComplete;

        popupPanel.SetActive(true);
        popupPanel.transform.SetAsLastSibling(); // En üstte görünsün
    }

    void ClosePopup()
    {
        popupPanel.SetActive(false);

        // Eðer bir callback (devam kodu) verildiyse onu çalýþtýr
        if (onCompleteCallback != null)
        {
            onCompleteCallback.Invoke();
        }
    }
}