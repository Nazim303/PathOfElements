using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public enum BattleState { START, PLAYERTURN, PLAYERATTACK, ENEMYTURN, WON, LOST, PATH_SELECTION }

public class TurnManager : MonoBehaviour
{
    [Header("State Info")]
    public BattleState state;

    [Header("Managers")]
    public CombatManager combatManager;
    public DeckManager deckManager;

    [Header("Enemy Configuration")]
    public EnemyData[] allEnemies; // Normal düþmanlar
    public EnemyData[] bossEnemies; // --- YENÝ: Boss listesi ---

  
   
    public int minEnemySpawn = 2; // En az kaç düþman çýksýn?
    public int maxEnemySpawn = 3; // En çok kaç düþman çýksýn?
    // --------------------------

    [Header("UI Feedback")]
    public TextMeshProUGUI turnIndicatorText;
    public TextMeshProUGUI roundText;     // --- YENÝ: Ekranda görünecek Raunt yazýsý ---
    private int currentRound = 1;         // --- YENÝ: Arka planda tutulan sayý ---


    // Savaþ türünü saklayalým
    private PathType currentBattleType;

    void Start() { }

    void SetTurnText(string text)
    {
        if (turnIndicatorText != null)
        {
            turnIndicatorText.text = text;
            turnIndicatorText.gameObject.SetActive(true);
        }
    }

    // --- YENÝ: Parametre alýyor ---
    public void StartNewBattle(PathType battleType)
    {
        state = BattleState.START;
        StartCoroutine(SetupBattle(battleType));
    }

    IEnumerator SetupBattle(PathType battleType)
    {

        currentRound = 1;       // Savaþ baþlarken Raunt 1'den baþlar
        UpdateRoundUI();        // Yazýyý günceller

        if (TerrainManager.instance != null) TerrainManager.instance.ResetTerrain();

        currentBattleType = battleType;

        // Yazýyý Türe Göre Ayarla
        if (battleType == PathType.Boss) SetTurnText("BOSS FIGHT!");
        else SetTurnText("PREPARÝNG FOR WAR...");

        // 1. Temizlik
        if (combatManager != null) StartCoroutine(combatManager.DestroyPlayedCards());

        // 2. Deste Hazýrlýðý
        if (deckManager != null) deckManager.InitializeBattle();

        yield return new WaitForSeconds(0.5f);

        // 3. Düþman Seçimi (Mantýk Deðiþti)
        List<EnemyData> enemiesToSpawn = new List<EnemyData>();

        if (battleType == PathType.Boss)
        {
            // --- BOSS MANTIÐI ---
            // Boss listesinden rastgele 1 tane seç
            if (bossEnemies.Length > 0)
            {
                enemiesToSpawn.Add(bossEnemies[Random.Range(0, bossEnemies.Length)]);
            }
            else
            {
                Debug.LogError("HATA: Boss Enemies listesi boþ!");
            }
        }
        else
        {
            // --- YENÝ: NORMAL SAVAÞ MANTIÐI (Dinamik Zorluk) ---
            int enemyCount = 1;

            // Oyuncunun haritada nerede olduðunu kontrol et
            if (PathManager.instance != null && PathManager.instance.currentNode != null && PathManager.instance.mapGenerator != null)
            {
                int currentLayer = PathManager.instance.currentNode.gridLayer;
                int totalLayers = PathManager.instance.mapGenerator.mapLength;

                // Haritanýn ortasýný hesapla (Örn: 10 katmansa ortasý 5)
                int middleLayer = totalLayers / 2;

                if (currentLayer < middleLayer)
                {
                    // 1. AÞAMA (Haritanýn baþlarý): 1 veya 2 düþman
                    enemyCount = Random.Range(1, 3); // (1 dahil, 3 hariç)
                }
                else
                {
                    // 2. AÞAMA (Haritanýn sonlarý): 2 veya 3 düþman
                    enemyCount = Random.Range(2, 4); // (2 dahil, 4 hariç)
                }
            }
            else
            {
                // Harita dýþýnda (örneðin direkt savaþ sahnesini test ederken) hata vermemesi için eski sistem:
                enemyCount = Random.Range(minEnemySpawn, maxEnemySpawn + 1);
            }

            for (int i = 0; i < enemyCount; i++)
            {
                if (allEnemies.Length > 0)
                    enemiesToSpawn.Add(allEnemies[Random.Range(0, allEnemies.Length)]);
            }
        }

        if (combatManager != null) combatManager.SetupEnemies(enemiesToSpawn);

        yield return new WaitForSeconds(1f);

        // 4. Baþlangýç Eli
        if (deckManager != null) deckManager.DrawCard(4);

        yield return new WaitForSeconds(1.5f);

        PlayerTurn();
    }

    // ... Kalan fonksiyonlar (PlayerTurn, Attack, EnemyTurn vs.) ayný kalacak ...
    // Sadece aþaðýya kopyalýyorum ki eksik kalmasýn:

    void UpdateRoundUI()
    {
        if (roundText != null) roundText.text = $"ROUND {currentRound}";
    }
    void PlayerTurn()
    {
        state = BattleState.PLAYERTURN;
        SetTurnText("SIRA SÝZDE");

        // Enerjiyi Fulle
        if (EnergyManager.instance != null)
        {
            EnergyManager.instance.RefillEnergy();
        }

        // --- YENÝ: Kalkaný Sýfýrla (Tur bitince kalkan gider) ---
        if (PlayerManager.instance != null)
        {
            PlayerManager.instance.ResetShield();
        }
    }

    public void OnAttackButton()
    {
        if (state != BattleState.PLAYERTURN) return;
        SetTurnText("");
        StartCoroutine(PlayerAttackSequence());
    }

    IEnumerator PlayerAttackSequence()
    {
        state = BattleState.PLAYERATTACK;
        yield return StartCoroutine(combatManager.OnAttackPhase());
        yield return new WaitForSeconds(0.5f);

        if (combatManager.activeEnemies.Count == 0)
        {
            state = BattleState.WON;
            SetTurnText("VÝCTORY!");
            StartCoroutine(GoToPathSelection());
        }
        else
        {
            state = BattleState.ENEMYTURN;
            StartCoroutine(EnemyTurn());
        }
    }

    IEnumerator EnemyTurn()
    {
        SetTurnText("THE ENEMY IS ATTACKING!");
        yield return new WaitForSeconds(0.5f);

        List<EnemyDisplay> enemiesActing = new List<EnemyDisplay>(combatManager.activeEnemies);

        foreach (EnemyDisplay enemy in enemiesActing)
        {
            if (enemy == null) continue;

            BossBehavior bossAI = enemy.GetComponent<BossBehavior>();

            if (bossAI != null)
            {
                yield return StartCoroutine(bossAI.ExecuteBossTurn());
            }
            else
            {
                yield return StartCoroutine(enemy.AttackAnimation());
                if (PlayerManager.instance != null)
                {
                    PlayerManager.instance.TakeDamage(enemy.enemyData.damage);
                }
            }
            yield return new WaitForSeconds(0.5f);
        }

        if (PlayerManager.instance.currentHealth <= 0)
        {
            state = BattleState.LOST;
            SetTurnText("YOU LOSE...");
        }
        else
        {
            yield return StartCoroutine(combatManager.DestroyPlayedCards());
            yield return new WaitForEndOfFrame();
            if (deckManager != null) deckManager.DrawCard(2);
            yield return new WaitForSeconds(0.8f);

            currentRound++;
            UpdateRoundUI();

            if (TerrainManager.instance != null)
            {
                TerrainManager.instance.ResetTerrain();
            }

            PlayerTurn();
        }
    }

    IEnumerator GoToPathSelection()
    {
        yield return new WaitForSeconds(1.5f);
        if (PathManager.instance != null) PathManager.instance.CompleteCurrentNode();
    }
}