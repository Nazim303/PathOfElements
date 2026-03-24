using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    [Header("References")]
    public TutorialPopup popupUI;

    [Header("Scripted Cards (Aþama 3-4)")]
    public CardData waterCard;    // Element döngüsü için (Su)
    public CardData earthCard;    // Element döngüsü için (Toprak - Zayýf)
    public CardData comboCard;    // Kombo için (Ayný elementten herhangi bir kart)

    [Header("Scripted Cards (Aþama 5 - Support)")]
    public CardData supportCard1; // Örn: Heal veya Draw yapan bir destek kartý
    public CardData supportCard2; // Örn: Gain Shield yapan bir destek kartý

    [Header("Scripted Cards (Aþama 6-7)")]
    public CardData terrainSupportCard; // Zemin deðiþtiren destek kartý
    public CardData terrainAttackCard;  // Zeminle ayný elementte saldýrý kartý
    public CardData fusionCardA;        // Fizyon Tarifindeki 1. Kart
    public CardData fusionCardB;        // Fizyon Tarifindeki 2. Kart

    private bool isStepCompleted = false;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        StartCoroutine(TutorialRoutine());
    }

    IEnumerator TutorialRoutine()
    {
        yield return null;

        // ==========================================
        // AÞAMA 1 & 2: DESTE SEÇÝMÝ VE HARÝTA
        // ==========================================
        isStepCompleted = false;
        popupUI.Show("Hoþ Geldin!", "Bu öðreticide oyunun temel mekaniklerini güvenli bir ortamda öðreneceðiz.\n\nÖncelikle kendi desteni oluþturmalýsýn. Açýk olan kartlardan 12 tanesini seçerek 'Belirle' butonuna bas.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => PathManager.instance.mapPanel.activeSelf);

        yield return new WaitForSeconds(0.5f);
        isStepCompleted = false;
        popupUI.Show("Patikalar", "Harika bir deste kurdun!\n\nEðitim boyunca sadece Savaþ (Kýlýç) patikalarýna gireceðiz. Ýlk antrenmanýmýz için kýlýç ikonlu düðüme týkla.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => PathManager.instance.combatPanel.activeSelf);

        // ==========================================
        // AÞAMA 3: SAVAÞIN TEMELLERÝ (RAUNT 1)
        // ==========================================
        yield return new WaitForSeconds(2.0f);
        isStepCompleted = false;
        popupUI.Show("Savaþýn Temelleri", "Karþýnda bir Antrenman Bebeði var. O bir Ateþ elementi ve sana saldýramaz.\n\nBir kartý boþ bir slota sürükle ve sol alttaki enerjinin azaldýðýný gör. Ardýndan saðdaki 'SALDIR' butonuna basarak turunu bitir.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        TurnManager tm = FindObjectOfType<TurnManager>();
        DeckManager dm = FindObjectOfType<DeckManager>();
        CombatManager cm = FindObjectOfType<CombatManager>();

        yield return new WaitUntil(() => tm.state == BattleState.ENEMYTURN);
        yield return new WaitUntil(() => tm.state == BattleState.PLAYERTURN);

        // ==========================================
        // AÞAMA 4: ELEMENT PUSULASI (RAUNT 2)
        // ==========================================
        yield return new WaitForSeconds(1.0f);
        dm.DiscardHand(); dm.drawPile.Clear(); dm.discardPile.Clear();
        dm.drawPile.Add(waterCard); dm.drawPile.Add(earthCard);
        dm.DrawCard(2);

        isStepCompleted = false;
        popupUI.Show("Element Pusulasý", "Elementlerin birbirine üstünlüðü vardýr.\nSu > Ateþ > Doða (Toprak) > Hava > Su\n\nDüþmanýmýz ATEÞ elementinde. Elindeki SU kartý ona 2 kat hasar vururken, TOPRAK kartý zayýf kalýp yarým hasar vuracaktýr.\n\nFarký görmek için Su kartýný oyna ve saldýr!", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => tm.state == BattleState.ENEMYTURN);
        yield return new WaitUntil(() => tm.state == BattleState.PLAYERTURN);

        // ==========================================
        // AÞAMA 5: KOMBO SÝSTEMÝ (RAUNT 3)
        // ==========================================
        yield return new WaitForSeconds(1.0f);
        dm.DiscardHand(); dm.drawPile.Clear(); dm.discardPile.Clear();
        dm.drawPile.Add(comboCard); dm.drawPile.Add(comboCard); dm.drawPile.Add(comboCard); dm.drawPile.Add(comboCard);
        dm.DrawCard(4);

        if (EnergyManager.instance != null) EnergyManager.instance.GainEnergy(5);
        isStepCompleted = false;
        popupUI.Show("Tam Kombo Zamaný!", "Eðer 4 slota da AYNI elementten kart koyarsan 'Tam Kombo' tetiklenir ve tüm düþmanlara Alan Hasarý (AOE) vurursun.\n\nElindeki 4 kartý da masaya yerleþtir ve gücü hisset!", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => tm.state == BattleState.ENEMYTURN);
        yield return new WaitUntil(() => tm.state == BattleState.PLAYERTURN);

        // ==========================================
        // AÞAMA 6: DESTEK KARTLARI VE FEDA (RAUNT 4)
        // ==========================================
        yield return new WaitForSeconds(1.0f);
        dm.DiscardHand(); dm.drawPile.Clear(); dm.discardPile.Clear();

        // Oyuncuya iki tane destek kartý verelim
        dm.drawPile.Add(supportCard1);
        dm.drawPile.Add(supportCard2);
        dm.DrawCard(2);

        isStepCompleted = false;
        popupUI.Show("Destek (Support) Kartlarý", "Sýrada Destek Kartlarý var!\n\nBu kartlar saldýrý yapmazlar, ancak sana anlýk avantajlar (Kalkan, Can, Ekstra Kart vb.) saðlarlar. Bir destek kartýný oynamak için onu ekranýn saðýndaki 'Feda (Exchange)' yuvasýna býrakmalýsýn.\n\nNormal kartlarýn aksine, destek kartlarýný buraya atmak senden can (HP) götürmez!\n\nHemen elindeki destek kartlarýný Exchange yuvasýna sürükle ve etkilerini gör. Ardýndan turunu bitir.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => tm.state == BattleState.ENEMYTURN);
        yield return new WaitUntil(() => tm.state == BattleState.PLAYERTURN);

        // ==========================================
        // AÞAMA 7: ZEMÝN (TERRAIN) MEKANÝÐÝ (RAUNT 5)
        // ==========================================
        yield return new WaitForSeconds(1.0f);
        dm.DiscardHand(); dm.drawPile.Clear(); dm.discardPile.Clear();
        dm.drawPile.Add(terrainSupportCard); dm.drawPile.Add(terrainAttackCard);
        dm.DrawCard(2);

        isStepCompleted = false;
        popupUI.Show("Alan Kontrolü (Terrain)", "Bazý çok özel Destek Kartlarý, savaþ alanýnýn zeminini kendi elementlerine göre deðiþtirebilir!\n\nÖnce elindeki Zemin Destek kartýný 'Feda (Exchange)' alanýna sürükle. Zemin deðiþtikten sonra, zeminle ayný elementte olan diðer saldýrý kartýný normal bir slota oynayarak ekstra 'Zemin Sinerjisi' hasarý vur!", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => tm.state == BattleState.ENEMYTURN);
        yield return new WaitUntil(() => tm.state == BattleState.PLAYERTURN);

        // ==========================================
        // AÞAMA 8: FÝZYON - BÜYÜK FÝNAL (RAUNT 6)
        // ==========================================
        yield return new WaitForSeconds(1.0f);
        dm.DiscardHand(); dm.drawPile.Clear(); dm.discardPile.Clear();
        dm.drawPile.Add(fusionCardA); dm.drawPile.Add(fusionCardB);
        dm.DrawCard(2);

        // Düþmanýn canýný 1'e indirelim
        if (cm.activeEnemies.Count > 0)
        {
            cm.activeEnemies[0].currentHealth = 1;
            cm.activeEnemies[0].UpdateHealthUI(1);
        }

        isStepCompleted = false;
        popupUI.Show("Fizyon (Birleþtirme)", "Ýþte son ve en güçlü mekaniðimiz!\n\nBazý uyumlu kartlarý masada üst üste koyarak o savaþlýk güçlü ve yepyeni kartlar yaratabilirsin.\n\nÞimdi bir kartý boþ slota koy, ardýndan diðer kartý sürükleyip doðrudan ONUN ÜSTÜNE býrak. Çýkan yeni fizyon kartýyla son saldýrýyý yap ve savaþý bitir!", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        // Savaþýn KAZANILMASINI bekle
        yield return new WaitUntil(() => tm.state == BattleState.WON);

        // ==========================================
        // AÞAMA 9: HARÝTAYA DÖNÜÞ VE TAVERNA HACK'Ý
        // ==========================================
        yield return new WaitUntil(() => PathManager.instance.mapPanel.activeSelf);

        // KUKLACI HACK'Ý: Sýradaki tüm düðümleri "Companion (Taverna)" türüne çevir
        if (PathManager.instance.currentNode != null && PathManager.instance.currentNode.nextNodes != null)
        {
            foreach (MapNode nextNode in PathManager.instance.currentNode.nextNodes)
            {
                nextNode.realPathType = PathType.Companion;
            }
        }

        yield return new WaitForSeconds(0.5f);
        isStepCompleted = false;
        popupUI.Show("Savaþ Bitti!", "Harika iþ çýkardýn, Antrenman Bebeðini yendin!\n\nSýradaki patika bir 'Taverna (Yoldaþ)' patikasý. Haritada açýlan yeni patikaya týklayarak Taverna'ya gir.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => PathManager.instance.companionPanel != null && PathManager.instance.companionPanel.activeSelf);

        // ==========================================
        // AÞAMA 10: KART KÝLÝDÝ AÇMA VE YOLDAÞLAR
        // ==========================================
        yield return new WaitForSeconds(0.5f);
        isStepCompleted = false;
        popupUI.Show("Taverna: Yeni Kartlar ve Yoldaþlar", "Tavernada karþýna desteni güçlendirecek rastgele 3 seçenek çýkar.\n\nEðer seçtiðin kart bir 'Yoldaþ (Companion)' kartýysa, bu sadece 1 savaþýnda sana yardým eder ve destende ayný anda en fazla 1 yoldaþ tutabilirsin.\n\nEðer normal bir saldýrý/þifa kartýysa, kilidi açýlýr ve kalýcý olarak destene eklenir!\n\nBir kart seçerek eðitimini tamamla.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        Debug.Log("--- TUTORIAL BÝTTÝ ---");
    }
}