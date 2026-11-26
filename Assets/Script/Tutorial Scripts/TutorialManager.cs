using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using  UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    [Header("References")]
    public TutorialPopup popupUI;

    [Header("Scripted Cards (Aşama 3-4)")]
    public CardData waterCard;    // Element döngüsü için (Su)
    public CardData earthCard;    // Element döngüsü için (Toprak - Zayıf)
    public CardData comboCard;    // Kombo için (Aynı elementten herhangi bir kart)

    [Header("Scripted Cards (Aşama 5 - Support)")]
    public CardData supportCard1; // Örn: Heal veya Draw yapan bir destek kartı
    public CardData supportCard2; // Örn: Gain Shield yapan bir destek kartı

    [Header("Scripted Cards (Final Saldırısı)")]
    [UnityEngine.Serialization.FormerlySerializedAs("fusionCardA")] public CardData finalCardA;
    [UnityEngine.Serialization.FormerlySerializedAs("fusionCardB")] public CardData finalCardB;

    private bool isStepCompleted = false;
    private bool isTutoComplated =false;

    void Awake()
    {
        if (instance == null) instance = this;
    }


    void Start()
    {
        StartCoroutine(TutorialRoutine());
    }


    private void Update()
    {
        if (isTutoComplated == true)
        {
            isTutoComplated = false; // KRİTİK: Sahnenin üst üste yüklenmesini engeller
            Time.timeScale = 1f;     // KRİTİK: Zamanın donuk kalmasını engeller
            SceneManager.LoadScene("SampleScene");
        }
    }

    public void SkipTuto()
    {
        Time.timeScale = 1f; // KRİTİK: Zamanı normale döndür
        SceneManager.LoadScene("SampleScene");
    }

    IEnumerator TutorialRoutine()
    {
        yield return null;

        // ==========================================
        // AŞAMA 1 & 2: DESTE SEÇİMİ VE HARİTA
        // ==========================================
        isStepCompleted = false;
        popupUI.Show("Welcome!", "In this tutorial, we will learn the basic mechanics of the game in a safe environment.\n\nFirst, you must create your own deck. Choose 12 cards from the face-up cards and press the 'Select' button.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => PathManager.instance.mapPanel.activeSelf);

        yield return new WaitForSeconds(0.5f);
        isStepCompleted = false;
        popupUI.Show("Paths", "You've put together a fantastic deck!\n\nDuring training, we will only enter the Combat (Sword) paths. Click on the sword icon node for our first training session.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => PathManager.instance.combatPanel.activeSelf);

        // ==========================================
        // AŞAMA 3: SAVAŞIN TEMELLERİ (RAUNT 1)
        // ==========================================
        yield return new WaitForSeconds(2.0f);

        TurnManager tm = TurnManager.instance != null ? TurnManager.instance : FindFirstObjectByType<TurnManager>();
        DeckManager dm = DeckManager.instance != null ? DeckManager.instance : FindFirstObjectByType<DeckManager>();
        CombatManager cm = CombatManager.instance != null ? CombatManager.instance : FindFirstObjectByType<CombatManager>();

        // Erken aşamalarda kuklanın ölüp tutorial'ı kilitlemesini önlemek için canını güvenceye alıyoruz
        if (cm != null && cm.activeEnemies.Count > 0)
        {
            cm.activeEnemies[0].currentHealth = 999;
            cm.activeEnemies[0].UpdateHealthUI(999);
        }

        isStepCompleted = false;
        popupUI.Show("Fundamentals of War", "You are facing a Training Baby. It is a Fire element card and cannot attack you.\n\nDrag a card to an empty slot and watch the energy decrease in the bottom left. Then press the 'ATTACK' button on the right to end your turn.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => tm.state == BattleState.ENEMYTURN);
        yield return new WaitUntil(() => tm.state == BattleState.PLAYERTURN);

        // ==========================================
        // AŞAMA 4: ELEMENT PUSULASI (RAUNT 2)
        // ==========================================
        yield return new WaitForSeconds(1.0f);
        dm.DiscardHand(); dm.drawPile.Clear(); dm.discardPile.Clear();
        dm.drawPile.Add(waterCard); dm.drawPile.Add(earthCard);
        dm.DrawCard(2);

        isStepCompleted = false;
        popupUI.Show("Elemental Compass", "Some elements have a superiority over others.\nWater > Fire > Nature (Earth) > Air > Water\n\nOur enemy is of the FIRE element. Their WATER card will deal double damage, while their EARTH card will be weaker, dealing half damage.\n\nPlay the Water card and attack to see the difference!", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => tm.state == BattleState.ENEMYTURN);
        yield return new WaitUntil(() => tm.state == BattleState.PLAYERTURN);

        // ==========================================
        // AŞAMA 5: KOMBO SİSTEMİ (RAUNT 3)
        // ==========================================
        yield return new WaitForSeconds(1.0f);
        dm.DiscardHand(); dm.drawPile.Clear(); dm.discardPile.Clear();
        dm.drawPile.Add(comboCard); dm.drawPile.Add(comboCard); dm.drawPile.Add(comboCard); dm.drawPile.Add(comboCard);
        dm.DrawCard(4);

        if (EnergyManager.instance != null) EnergyManager.instance.GainEnergy(5);
        isStepCompleted = false;
        popupUI.Show("It's Combo Time!", "If you place cards of the SAME element in all 4 slots, a 'Full Combo' will be triggered, dealing Area of Effect (AOE) damage to all enemies.\n\nPlace all four cards you have on the table and feel the power!", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => tm.state == BattleState.ENEMYTURN);
        yield return new WaitUntil(() => tm.state == BattleState.PLAYERTURN);

        // ==========================================
        // AŞAMA 6: DESTEK KARTLARI VE FEDA (RAUNT 4)
        // ==========================================
        yield return new WaitForSeconds(1.0f);
        dm.DiscardHand(); dm.drawPile.Clear(); dm.discardPile.Clear();

        // Oyuncuya iki tane destek kartı verelim
        dm.drawPile.Add(supportCard1);
        dm.drawPile.Add(supportCard2);
        dm.DrawCard(2);

        isStepCompleted = false;
        popupUI.Show("Support Cards", "Next up are Support Cards!\n\nThese cards don't attack, but they provide you with temporary advantages (Shield, Health, Extra Card, etc.). To play a support card, you must drop it into the 'Exchange' slot on the right side of the screen.\n\nUnlike normal cards, dropping support cards here doesn't reduce your health (HP)!\n\nImmediately drag your support cards to the Exchange slot and see their effects. Then end your turn.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => tm.state == BattleState.ENEMYTURN);
        yield return new WaitUntil(() => tm.state == BattleState.PLAYERTURN);

        // ==========================================
        // AŞAMA 7: FİNAL SALDIRISI (RAUNT 5)
        // ==========================================
        yield return new WaitForSeconds(1.0f);
        dm.DiscardHand(); dm.drawPile.Clear(); dm.discardPile.Clear();
        dm.drawPile.Add(finalCardA); dm.drawPile.Add(finalCardB);
        dm.DrawCard(2);

        // Düşmanın canını 1'e indirelim
        if (cm.activeEnemies.Count > 0)
        {
            cm.activeEnemies[0].currentHealth = 1;
            cm.activeEnemies[0].UpdateHealthUI(1);
        }

        isStepCompleted = false;
        popupUI.Show("Finish It!", "The enemy is barely standing.\n\nYou now know everything you need: elements, combos and support cards.\n\nPlace your cards into the slots and end the battle with a final attack!", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        // Savaşın KAZANILMASINI bekle
        yield return new WaitUntil(() => tm.state == BattleState.WON);

        // ==========================================
        // AŞAMA 8: HARİTAYA DÖNÜŞ VE TAVERNA HACK'İ
        // ==========================================
        yield return new WaitUntil(() => PathManager.instance.mapPanel.activeSelf);

        // KUKLACI HACK'İ: Sıradaki tüm düğümleri "Companion (Taverna)" türüne çevir
        if (PathManager.instance.currentNode != null && PathManager.instance.currentNode.nextNodes != null)
        {
            foreach (MapNode nextNode in PathManager.instance.currentNode.nextNodes)
            {
                nextNode.realPathType = PathType.Companion;
            }
        }

        yield return new WaitForSeconds(0.5f);
        isStepCompleted = false;
        popupUI.Show("The War is Over!", "Great job, you beat Training Baby!\n\nThe next trail is a 'Taverna (Companion)' trail. Click on the new trail that opens on the map to enter the Taverna.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => PathManager.instance.companionPanel != null && PathManager.instance.companionPanel.activeSelf);

   
        yield return new WaitForSeconds(0.5f);
        isStepCompleted = false;
        popupUI.Show("Tavern: New Cards and Companions", "In the tavern, you'll be presented with 3 random options to strengthen your deck.\n\nIf the card you choose is a 'Companion' card, it will only help you in 1 battle, and you can only keep a maximum of 1 companion in your deck at a time.\n\nIf it's a regular attack/healing card, it will be unlocked and permanently added to your deck!\n\nComplete your tutorial by choosing a card.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);


        yield return new WaitForSeconds(0.5f);
        isTutoComplated = false;
        popupUI.Show("Tutorial Completed!", "You have learned the elemental mechanics. Now entering the main game...", () => { isTutoComplated = true; });
        Debug.Log("--- TUTORIAL BİTTİ ---");
    }
}