using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using  UnityEngine.SceneManagement;

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
            isTutoComplated = false; // KRÝTÝK: Sahnenin üst üste yüklenmesini engeller
            Time.timeScale = 1f;     // KRÝTÝK: Zamanýn donuk kalmasýný engeller
            SceneManager.LoadScene("SampleScene");
        }
    }

    public void SkipTuto()
    {
        Time.timeScale = 1f; // KRÝTÝK: Zamaný normale döndür
        SceneManager.LoadScene("SampleScene");
    }

    IEnumerator TutorialRoutine()
    {
        yield return null;

        // ==========================================
        // AÞAMA 1 & 2: DESTE SEÇÝMÝ VE HARÝTA
        // ==========================================
        isStepCompleted = false;
        popupUI.Show("Welcome!", "In this tutorial, we will learn the basic mechanics of the game in a safe environment.\n\nFirst, you must create your own deck. Choose 12 cards from the face-up cards and press the 'Select' button.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => PathManager.instance.mapPanel.activeSelf);

        yield return new WaitForSeconds(0.5f);
        isStepCompleted = false;
        popupUI.Show("Phats", "You've put together a fantastic deck!\n\nDuring training, we will only enter the Combat (Sword) paths. Click on the sword icon knot for our first training session.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => PathManager.instance.combatPanel.activeSelf);

        // ==========================================
        // AÞAMA 3: SAVAÞIN TEMELLERÝ (RAUNT 1)
        // ==========================================
        yield return new WaitForSeconds(2.0f);
        isStepCompleted = false;
        popupUI.Show("Fundamentals of War", "You are facing a Training Baby. It is a Fire element card and cannot attack you.\n\nDrag a card to an empty slot and watch the energy decrease in the bottom left. Then press the 'ATTACK' button on the right to end your turn.", () => { isStepCompleted = true; });
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
        popupUI.Show("Elemental Compass", "Some elements have a superiority over others.\nWater > Fire > Nature (Earth) > Air > Water\n\nOur enemy is of the FIRE element. Their WATER card will deal double damage, while their EARTH card will be weaker, dealing half damage.\n\nPlay the Water card and attack to see the difference!", () => { isStepCompleted = true; });
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
        popupUI.Show("It's Combo Time!", "If you place cards of the SAME element in all 4 slots, a 'Full Combo' will be triggered, dealing Area of ??Effect (AOE) damage to all enemies.\n\nPlace all four cards you have on the table and feel the power!", () => { isStepCompleted = true; });
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
        popupUI.Show("Support Cards", "Next up are Support Cards!\n\nThese cards don't attack, but they provide you with temporary advantages (Shield, Health, Extra Card, etc.). To play a support card, you must drop it into the 'Exchange' slot on the right side of the screen.\n\nUnlike normal cards, dropping support cards here doesn't reduce your health (HP)!\n\nImmediately drag your support cards to the Exchange slot and see their effects. Then end your turn.", () => { isStepCompleted = true; });
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
        popupUI.Show("Terrain Control", "Some very special Support Cards can change the battlefield's ground to match their element!\n\nFirst, drag your Ground Support card to the 'Exchange' area. After the ground changes, play another attack card of the same element as the ground into a normal slot to deal extra 'Ground Synergy' damage!", () => { isStepCompleted = true; });
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
        popupUI.Show("Fusion (Merging)", "Here's our final and most powerful mechanic!\n\nBy stacking some compatible cards on the table, you can create powerful, brand-new cards for that battle.\n\nNow place a card in an empty slot, then drag and drop another card directly on top of it. Unleash the final attack with the resulting new fusion card and end the battle!", () => { isStepCompleted = true; });
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
        popupUI.Show("The War is Over!", "Great job, you beat Training Baby!\n\nThe next trail is a 'Taverna (Companion)' trail. Click on the new trail that opens on the map to enter the Taverna.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);

        yield return new WaitUntil(() => PathManager.instance.companionPanel != null && PathManager.instance.companionPanel.activeSelf);

   
        yield return new WaitForSeconds(0.5f);
        isStepCompleted = false;
        popupUI.Show("Tavern: New Cards and Companions", "In the tavern, you'll be presented with 3 random options to strengthen your deck.\n\nIf the card you choose is a 'Companion' card, it will only help you in 1 battle, and you can only keep a maximum of 1 companion in your deck at a time.\n\nIf it's a regular attack/healing card, it will be unlocked and permanently added to your deck!\n\nComplete your tutorial by choosing a card.", () => { isStepCompleted = true; });
        yield return new WaitUntil(() => isStepCompleted);


        yield return new WaitForSeconds(0.5f);
        isTutoComplated = false;
        popupUI.Show("Tutorial is anded" , "Now you transfering to game" ,() => { isTutoComplated = true; });
        Debug.Log("--- TUTORIAL BÝTTÝ ---");
    }
}