using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // YENİ: Image materyallerine erişmek için

public class CombatManager : MonoBehaviour
{
    public static CombatManager instance;

    void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);
    }

    [Header("Player Board")]
    public CardData[] playerSlots = new CardData[4];
    public Transform[] slotTransforms;

    [Header("Enemy Spawning")]
    public GameObject enemyPrefab;
    public Transform enemySpawnPoint;
    public List<EnemyDisplay> activeEnemies = new List<EnemyDisplay>();
    public int activeEnemyCount;
    public BossBehavior currentBoss;
    public EnemyData defaultMinion;
    public GameObject enemyDeathEffectPrefab;

    public GameObject cardPrefab;

    [Header("Visuals")]
    public Transform discardPoint;

    // --- YENİ EFEKT REFERANSLARI (GÜNCELLENDİ) ---
    [Header("Particle & Shader Effects")]
    public GameObject dissolveBurningPrefab;
    public GameObject enemySmokeDeathPrefab;
    public GameObject spellChargePrefab;

    [Header("Level 1 Attack Effects")]
    public GameObject vfx_FireAttack01;
    public GameObject vfx_WaterAttack01;
    public GameObject vfx_EarthAttack01;
    public GameObject vfx_AirAttack01;

    [Header("Level 2 Attack Effects")]
    public GameObject vfx_FireAttack02;
    public GameObject vfx_WaterAttack02;
    public GameObject vfx_EarthAttack02;
    public GameObject vfx_AirAttack02;

    [Header("Level 3 Attack Effects")]
    public GameObject vfx_FireAttack03;
    public GameObject vfx_WaterAttack03;
    public GameObject vfx_EarthAttack03;
    public GameObject vfx_AirAttack03;

    [Header("Level 1 Impact Effects")]
    public GameObject vfx_FireImpact01;
    public GameObject vfx_WaterImpact01;
    public GameObject vfx_EarthImpact01;
    public GameObject vfx_AirImpact01;

    [Header("Level 2 Impact Effects")]
    public GameObject vfx_FireImpact02;
    public GameObject vfx_WaterImpact02;
    public GameObject vfx_EarthImpact02;
    public GameObject vfx_AirImpact02;

    [Header("Level 3 Impact Effects")]
    public GameObject vfx_FireImpact03;
    public GameObject vfx_WaterImpact03;
    public GameObject vfx_EarthImpact03;
    public GameObject vfx_AirImpact03;




    // LAG FIX
    public void SetupEnemies(List<EnemyData> enemiesToSpawn)
    {
        ClearEnemies();
        currentBoss = null;
        StartCoroutine(SpawnEnemiesRoutine(enemiesToSpawn));
    }

    IEnumerator SpawnEnemiesRoutine(List<EnemyData> enemiesToSpawn)
    {
        foreach (EnemyData data in enemiesToSpawn)
        {
            GameObject newEnemyObj = Instantiate(enemyPrefab, enemySpawnPoint);
            EnemyDisplay display = newEnemyObj.GetComponent<EnemyDisplay>();

            display.SetupEnemy(data, data.maxHealth);
            activeEnemies.Add(display);

            bool isBoss = data.enemyName.Contains("Protector") || data.enemyName.Contains("Elementalist");
            float enemyScale = 1f;
            if (GlobalScaleManager.instance != null)
            {
                enemyScale = isBoss ? GlobalScaleManager.instance.bossEnemyScale : GlobalScaleManager.instance.normalEnemyScale;
            }
            newEnemyObj.transform.localScale = Vector3.one * enemyScale;

            if (data.enemyName.Contains("Protector"))
            {
                currentBoss = newEnemyObj.AddComponent<Boss_Protector>();
                ((Boss_Protector)currentBoss).minionData = defaultMinion;
                currentBoss.Setup(this, display);
            }
            else if (data.enemyName.Contains("Elementalist"))
            {
                currentBoss = newEnemyObj.AddComponent<Boss_Elementalist>();
                currentBoss.Setup(this, display);
            }
        }

        activeEnemyCount = activeEnemies.Count;
        Debug.Log($"Savaş Başladı! {activeEnemies.Count} düşman anında belirdi.");
        yield return null;
    }

    void ClearEnemies()
    {
        foreach (EnemyDisplay enemy in activeEnemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }
        activeEnemies.Clear();
    }

    public void UpdateSlot(int index, CardData data)
    {
        if (index >= 0 && index < playerSlots.Length)
        {
            playerSlots[index] = data;
            RecalculateTotalEnergyCost();
        }
    }

    public void ClearSlot(int index)
    {
        if (index >= 0 && index < playerSlots.Length)
        {
            playerSlots[index] = null;
            RecalculateTotalEnergyCost();
        }
    }

    // --- YENİ: EFEKTLERİ EN ÖNE ÇEKME ASİSTANI ---
    void BringEffectToFront(GameObject effectObj)
    {
        if (effectObj == null) return;

        // Hiyerarşide en alta (öne) al
        effectObj.transform.SetAsLastSibling();

        // İçindeki tüm partikül sistemlerini bul ve çizim sırasını UI'ın üstüne (200) çıkar
        ParticleSystemRenderer[] renderers = effectObj.GetComponentsInChildren<ParticleSystemRenderer>(true);
        foreach (ParticleSystemRenderer r in renderers)
        {
            r.sortingOrder = 200;
        }
    }

    // Element ve Güç seviyesine göre doğru prefabı seçer
    GameObject GetAttackVFXPrefab(ElementType element, int power)
    {
        switch (power)
        {
            case 1:
                if (element == ElementType.Fire) return vfx_FireAttack01;
                if (element == ElementType.Water) return vfx_WaterAttack01;
                if (element == ElementType.Earth) return vfx_EarthAttack01;
                if (element == ElementType.Air) return vfx_AirAttack01;
                break;
            case 2:
                if (element == ElementType.Fire) return vfx_FireAttack02;
                if (element == ElementType.Water) return vfx_WaterAttack02;
                if (element == ElementType.Earth) return vfx_EarthAttack02;
                if (element == ElementType.Air) return vfx_AirAttack02;
                break;
            case 3:
            default: // 3 veya beklenmedik durumlarda en güçlüsü
                if (element == ElementType.Fire) return vfx_FireAttack03;
                if (element == ElementType.Water) return vfx_WaterAttack03;
                if (element == ElementType.Earth) return vfx_EarthAttack03;
                if (element == ElementType.Air) return vfx_AirAttack03;
                break;
        }
        return null;
    }

    // Element ve Güç seviyesine göre doğru Impact (Çarpma) prefabını seçer
    GameObject GetImpactVFXPrefab(ElementType element, int power)
    {
        switch (power)
        {
            case 1:
                if (element == ElementType.Fire) return vfx_FireImpact01;
                if (element == ElementType.Water) return vfx_WaterImpact01;
                if (element == ElementType.Earth) return vfx_EarthImpact01;
                if (element == ElementType.Air) return vfx_AirImpact01;
                break;
            case 2:
                if (element == ElementType.Fire) return vfx_FireImpact02;
                if (element == ElementType.Water) return vfx_WaterImpact02;
                if (element == ElementType.Earth) return vfx_EarthImpact02;
                if (element == ElementType.Air) return vfx_AirImpact02;
                break;
            case 3:
            default:
                if (element == ElementType.Fire) return vfx_FireImpact03;
                if (element == ElementType.Water) return vfx_WaterImpact03;
                if (element == ElementType.Earth) return vfx_EarthImpact03;
                if (element == ElementType.Air) return vfx_AirImpact03;
                break;
        }
        return null;
    }

    // Elementin türüne göre uygun rengi döndürür
    Color GetElementColor(ElementType type)
    {
        switch (type)
        {
            case ElementType.Fire: return new Color(1f, 0.25f, 0.25f);
            case ElementType.Water: return new Color(0.25f, 0.6f, 1f);
            case ElementType.Earth: return new Color(0.6f, 0.4f, 0.2f);
            case ElementType.Air: return new Color(0.3f, 0.85f, 0.7f);
            default: return Color.white;
        }
    }

    // Yaratılan partikülün (ve içindeki tüm alt partiküllerin) rengini değiştirir
    void SetParticleColor(GameObject effectObj, Color color)
    {
        if (effectObj == null) return;

        ParticleSystem[] particles = effectObj.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem ps in particles)
        {
            var main = ps.main;
            main.startColor = color;
        }
    }


   // --- GÜNCELLENEN FONKSİYON: CANVAS İÇİNDE UÇAN MERMİLER ---
    IEnumerator PlaySpellEffects(Transform startTransform, List<EnemyDisplay> targets, CardData cardData)
    {
        if (targets == null || targets.Count == 0 || startTransform == null || cardData == null) yield break;

        // --- KRİTİK ÇÖZÜM: Game Manager'ı değil, Canvas'ı bul ve parent yap! ---
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        Transform vfxParent = mainCanvas != null ? mainCanvas.transform : transform;

        Color elementColor = GetElementColor(cardData.element);

        // 1. KARTIN PARLAMASI 
        CardDisplay display = startTransform.GetComponent<CardDisplay>();
        if (display != null && display.artImage != null)
        {
            Material mat = display.artImage.material;
            if (mat.HasProperty("_FlashColor") && mat.HasProperty("_FlashAmount"))
            {
                mat.SetColor("_FlashColor", elementColor);
                float flashDuration = 0.15f;
                float t = 0;

                while (t < 1) { t += Time.deltaTime / flashDuration; mat.SetFloat("_FlashAmount", Mathf.Lerp(0f, 1f, t)); yield return null; }
                t = 0;
                while (t < 1) { t += Time.deltaTime / flashDuration; mat.SetFloat("_FlashAmount", Mathf.Lerp(1f, 0f, t)); yield return null; }
                mat.SetFloat("_FlashAmount", 0f);
            }
        }

        // 1.5 CHARGE (Enerji Toplama) 
        GameObject charge = null;
        if (spellChargePrefab != null)
        {
            charge = Instantiate(spellChargePrefab, vfxParent); // Artık Canvas'ta oluşuyor
            charge.transform.position = startTransform.position;
            charge.transform.localScale = Vector3.one;

            Vector3 chargePos = charge.transform.localPosition;
            chargePos.z = -50f; // Öne itele
            charge.transform.localPosition = chargePos;

            SetParticleColor(charge, elementColor);
            BringEffectToFront(charge);

            yield return new WaitForSeconds(0.5f);
        }
        if (charge != null) Destroy(charge);

        // 2. MERMİ MANTIĞI
        GameObject vfxPrefabToUse = GetAttackVFXPrefab(cardData.element, cardData.cardPower);
        List<GameObject> spawnedAttacks = new List<GameObject>();
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> endPositions = new List<Vector3>();

        if (vfxPrefabToUse != null)
        {
            foreach (var target in targets)
            {
                if (target != null && target.gameObject != null)
                {
                    Transform targetCenter = target.visualRoot != null ? target.visualRoot : target.transform;

                    GameObject attackVFX = Instantiate(vfxPrefabToUse, vfxParent); // Artık Canvas'ta oluşuyor
                    attackVFX.transform.position = startTransform.position;
                    attackVFX.transform.localScale = Vector3.one;

                    Vector3 atkPos = attackVFX.transform.localPosition;
                    atkPos.z = -50f; // Öne itele
                    attackVFX.transform.localPosition = atkPos;

                    BringEffectToFront(attackVFX);

                    spawnedAttacks.Add(attackVFX);
                    startPositions.Add(attackVFX.transform.position);
                    endPositions.Add(targetCenter.position);
                }
            }

          

            // --- YENİ: UÇUŞ SESİNİ OYNAT ---
            if (AudioManager.instance != null) AudioManager.instance.PlayTrailSound(cardData.element, cardData.cardPower);

            float moveT = 0;
            while (moveT < 1)
            {
                moveT += Time.deltaTime * 6f;
                for (int i = 0; i < spawnedAttacks.Count; i++)
                {
                    if (spawnedAttacks[i] != null)
                    {
                        spawnedAttacks[i].transform.position = Vector3.Lerp(startPositions[i], endPositions[i], moveT);
                    }
                }
                yield return null;
            }

            foreach (var attack in spawnedAttacks)
            {
                if (attack != null) Destroy(attack, 0.5f);
            }
        }

        // 3. IMPACT (Patlama) 
        GameObject impactPrefabToUse = GetImpactVFXPrefab(cardData.element, cardData.cardPower);
        if (impactPrefabToUse != null)
        {
            foreach (var target in targets)
            {
                if (target != null && target.gameObject != null)
                {
                    Transform targetCenter = target.visualRoot != null ? target.visualRoot : target.transform;

                    GameObject impact = Instantiate(impactPrefabToUse, vfxParent); // Artık Canvas'ta oluşuyor
                    // --- YENİ: ÇARPMA SESİNİ OYNAT ---
                    if (AudioManager.instance != null) AudioManager.instance.PlayImpactSound(cardData.element, cardData.cardPower);
                    impact.transform.position = targetCenter.position;
                    impact.transform.localScale = Vector3.one;

                    Vector3 impPos = impact.transform.localPosition;
                    impPos.z = -50f; // Öne itele
                    impact.transform.localPosition = impPos;

                    BringEffectToFront(impact);
                    Destroy(impact, 1f);
                }
            }
        }
    }

    public IEnumerator OnAttackPhase()
    {
        bool isFullCombo = CheckElementalCombo();
        bool isSplitCombo = CheckTwoPairCombo();

        EnemyDisplay frontEnemy = null;
        if (activeEnemies.Count > 0) frontEnemy = activeEnemies[0];

        Debug.Log("--- TUR BAŞLIYOR ---");

        {
           
            if (isFullCombo) Debug.Log("?? TAM ELEMENTAL KOMBO (AOE) AKTİF! ??");
            else if (isSplitCombo) Debug.Log("?? AYRIK İKİLİ KOMBO (SPLIT) AKTİF! ??");

            bool[] processed = new bool[4];

            for (int i = 0; i < playerSlots.Length; i++)
            {
                if (processed[i]) continue;

                if (playerSlots[i] != null && slotTransforms[i].childCount > 0)
                {
                    // YENİ: GetChild(0) yerine GERÇEK kartı bul!
                    CardDisplay display = slotTransforms[i].GetComponentInChildren<CardDisplay>();
                    if (display == null) continue; // Kart yoksa (sadece efekt kalmışsa) atla

                    Transform cardTransform = display.transform;
                    CardData card = playerSlots[i];

                    if (!EnergyManager.instance.TrySpendEnergy(card.energyCost))
                    {
                        yield return StartCoroutine(FizzleCardRoutine(i, cardTransform, card));
                        yield return new WaitForSeconds(0.4f);
                        continue;
                    }

                    Transform supportTransform = null;
                    CardData supportCard = null;
                    float currentSupportMultiplier = 1.0f;

                    // --- SAĞDAKİ GÜÇLENDİRİCİYİ KONTROL ET ---
                    // Güçlendirici sadece AYNI elementteki kartı +%50 güçlendirir.
                    // Farklı elementteyse eşleşmez ve kendi başına normal saldırı yapar (ceza yok).
                    if (i < 3 && playerSlots[i + 1] != null && playerSlots[i + 1].isElementalModifier
                        && playerSlots[i + 1].element == card.element)
                    {
                        supportCard = playerSlots[i + 1];
                        if (slotTransforms[i + 1].childCount > 0)
                        {
                            // YENİ: Sağdaki slotun da GERÇEK kartını bul!
                            CardDisplay supDisplay = slotTransforms[i + 1].GetComponentInChildren<CardDisplay>();
                            if (supDisplay != null) supportTransform = supDisplay.transform;
                        }

                        if (EnergyManager.instance.TrySpendEnergy(supportCard.energyCost))
                        {
                            processed[i + 1] = true;

                            currentSupportMultiplier = 1.5f;
                            Debug.Log($"MÜKEMMEL UYUM! {supportCard.cardName}, hasarı artırdı!");
                        }
                    }

                    // --- ANİMASYON (İki Kart Varsa Aynı Anda Kalkar) ---
                    Vector3 originalPos = cardTransform.localPosition;
                    Vector3 punchPos = originalPos + new Vector3(0, 50f, 0);

                    Vector3 supOriginalPos = Vector3.zero;
                    Vector3 supPunchPos = Vector3.zero;

                    if (supportTransform != null)
                    {
                        supOriginalPos = supportTransform.localPosition;
                        supPunchPos = supOriginalPos + new Vector3(0, 50f, 0);
                    }

                    float animT = 0;
                    while (animT < 1)
                    {
                        animT += Time.deltaTime * 10;
                        cardTransform.localPosition = Vector3.Lerp(originalPos, punchPos, animT);
                        if (supportTransform != null) supportTransform.localPosition = Vector3.Lerp(supOriginalPos, supPunchPos, animT);
                        yield return null;
                    }

                    List<EnemyDisplay> currentTargets = new List<EnemyDisplay>();

                    if (card.cardType == CardType.Companion)
                    {
                        if (card.specialEffect == SpecialEffect.HalveEnemyHealth && frontEnemy != null) currentTargets.Add(frontEnemy);
                        else if (card.specialEffect == SpecialEffect.AOE_Damage) currentTargets.AddRange(activeEnemies);
                    }
                    else if (card.cardType == CardType.Attack)
                    {
                        if (activeEnemies.Count > 0)
                        {
                            if (isFullCombo) currentTargets.AddRange(activeEnemies);
                            else if (isSplitCombo)
                            {
                                EnemyDisplay target = (i < 2) ? activeEnemies[0] : activeEnemies[activeEnemies.Count - 1];
                                if (target != null) currentTargets.Add(target);
                            }
                            else currentTargets.Add(activeEnemies[0]);
                        }
                    }

                    // Güçlendirici tek başına oynanırsa kendi hasarıyla normal saldırı yapar
                    {
                        if (currentTargets.Count > 0)
                        {
                            yield return StartCoroutine(PlaySpellEffects(cardTransform, currentTargets, card));
                        }

                        if (card.cardType == CardType.Companion)
                        {
                            switch (card.specialEffect)
                            {
                                case SpecialEffect.HalveEnemyHealth:
                                    if (frontEnemy != null)
                                    {
                                        int halfHp = frontEnemy.currentHealth / 2;
                                        frontEnemy.currentHealth = halfHp;
                                        frontEnemy.UpdateHealthUI(frontEnemy.currentHealth);
                                        StartCoroutine(frontEnemy.TakeDamageEffect());
                                    }
                                    break;
                                case SpecialEffect.FullPlayerHeal:
                                    if (PlayerManager.instance != null) PlayerManager.instance.Heal(PlayerManager.instance.maxHealth - PlayerManager.instance.currentHealth);
                                    break;
                                case SpecialEffect.AOE_Damage:
                                    foreach (EnemyDisplay enemy in currentTargets)
                                    {
                                        if (enemy != null) { ApplyDamageToEnemy(card, enemy, currentSupportMultiplier); StartCoroutine(enemy.TakeDamageEffect()); }
                                    }
                                    break;
                            }
                            DeckManager dm = DeckManager.instance != null ? DeckManager.instance : FindFirstObjectByType<DeckManager>();
                            if (dm != null) dm.RemoveCardFromDeck(card);
                        }
                        else if (card.cardType == CardType.Heal)
                        {
                            if (PlayerManager.instance != null) PlayerManager.instance.Heal(card.healAmount);
                        }
                        else if (card.cardType == CardType.Support) // --- YENİ EKLENEN KORUMA ---
                        {
                            Debug.Log($"{card.cardName} bir Destek (Support) kartıdır ve normal slottan etki etmez! Lütfen Exchange (Feda) yuvasını kullanın.");
                            // Kart çöpe gidecek ama hasar falan vurmayacak.
                        }
                        else
                        {
                            foreach (EnemyDisplay enemy in currentTargets)
                            {
                                if (enemy != null) { ApplyDamageToEnemy(card, enemy, currentSupportMultiplier); StartCoroutine(enemy.TakeDamageEffect()); }
                            }
                        }
                    }

                    animT = 0;
                    while (animT < 1)
                    {
                        animT += Time.deltaTime * 10;
                        cardTransform.localPosition = Vector3.Lerp(punchPos, originalPos, animT);
                        if (supportTransform != null) supportTransform.localPosition = Vector3.Lerp(supPunchPos, supOriginalPos, animT);
                        yield return null;
                    }

                    yield return new WaitForSeconds(0.2f);
                    HandleDeaths();
                    yield return new WaitForSeconds(0.6f);
                }
            }
        }
    }

    bool CheckTwoPairCombo()
    {
        for (int i = 0; i < playerSlots.Length; i++) { if (playerSlots[i] == null) return false; }
        bool firstPairMatch = playerSlots[0].element == playerSlots[1].element;
        bool secondPairMatch = playerSlots[2].element == playerSlots[3].element;
        bool pairsAreDifferent = playerSlots[0].element != playerSlots[2].element;
        return firstPairMatch && secondPairMatch && pairsAreDifferent;
    }

    bool CheckElementalCombo()
    {
        for (int i = 0; i < playerSlots.Length; i++) if (playerSlots[i] == null) return false;
        ElementType firstElement = playerSlots[0].element;
        for (int i = 1; i < playerSlots.Length; i++) if (playerSlots[i].element != firstElement) return false;
        return true;
    }

    public void SpawnExtraEnemy(EnemyData data)
    {
        GameObject newEnemyObj = Instantiate(enemyPrefab, enemySpawnPoint);
        EnemyDisplay display = newEnemyObj.GetComponent<EnemyDisplay>();
        display.SetupEnemy(data, data.maxHealth);
        activeEnemies.Insert(0, display);
        newEnemyObj.transform.SetAsFirstSibling();
        newEnemyObj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        activeEnemyCount = activeEnemies.Count;
    }

    void ApplyDamageToEnemy(CardData card, EnemyDisplay target, float supportMultiplier = 1.0f)
    {
        if (target == null) return;
        BossBehavior boss = target.GetComponent<BossBehavior>();
        if (boss != null && boss.IsInvulnerable()) return;

        int finalDamage = 0;
        {
            float multiplier = ElementSystem.GetDamageMultiplier(card.element, target.currentElement);

            int baseDmg = card.baseDamage;
            if (PlayerManager.instance != null)
            {
                baseDmg += PlayerManager.instance.bonusDamage;
            }
            // YENİ: Bonus hasar ve Support çarpanı ile asıl hasarı çarpıyoruz
            finalDamage = Mathf.RoundToInt(baseDmg * multiplier * supportMultiplier);
        }

        target.currentHealth -= finalDamage;
        if (target.currentHealth < 0) target.currentHealth = 0;
        target.UpdateHealthUI(target.currentHealth);
    }

    void HandleDeaths()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i].currentHealth <= 0) EnemyDie(activeEnemies[i]);
        }
    }

    void EnemyDie(EnemyDisplay enemy)
    {
        if (PlayerManager.instance != null)
        {
            PlayerManager.instance.AddGold(enemy.enemyData.goldReward);
            // --- YENİ: Düşman öldürme skorunu artır ---
            PlayerManager.instance.enemiesDefeated++;
        }

        activeEnemies.Remove(enemy);

        // --- KRİTİK ÇÖZÜM: Game Manager'ı değil, Canvas'ı bul ve parent yap! ---
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        Transform vfxParent = mainCanvas != null ? mainCanvas.transform : transform;

        // 1. Orijinal Yanma/Erime Efekti
        if (enemyDeathEffectPrefab != null)
        {
            GameObject effect = Instantiate(enemyDeathEffectPrefab, vfxParent);
            effect.transform.position = enemy.transform.position;
            effect.transform.localScale = Vector3.one;

            // Öne itele
            Vector3 pos = effect.transform.localPosition;
            pos.z = -50f;
            effect.transform.localPosition = pos;

            BringEffectToFront(effect);
            Destroy(effect, 2f);
        }

        // 2. Ekstra Duman Efekti
        if (enemySmokeDeathPrefab != null)
        {
            GameObject smokeEffect = Instantiate(enemySmokeDeathPrefab, vfxParent);
            smokeEffect.transform.position = enemy.transform.position;
            smokeEffect.transform.localScale = Vector3.one;

            // Öne itele
            Vector3 pos = smokeEffect.transform.localPosition;
            pos.z = -50f;
            smokeEffect.transform.localPosition = pos;

            BringEffectToFront(smokeEffect);
            Destroy(smokeEffect, 2.5f);
        }

        StartCoroutine(enemy.DieAnimationAndDestroy());
        activeEnemyCount = activeEnemies.Count;
    }

    void RecalculateTotalEnergyCost()
    {
        int totalCost = 0;
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] != null) totalCost += playerSlots[i].energyCost;
        }
        if (EnergyManager.instance != null) EnergyManager.instance.UpdatePendingCost(totalCost);
    }

    public IEnumerator DestroyPlayedCards()
    {
        DeckManager deckManager = DeckManager.instance != null ? DeckManager.instance : FindFirstObjectByType<DeckManager>();
        for (int i = 0; i < slotTransforms.Length; i++)
        {
            if (slotTransforms[i].childCount > 0)
            {
                // Slotun içindeki tüm objeleri (Kart + Efektler) bir listeye alalım
                List<Transform> children = new List<Transform>();
                foreach (Transform child in slotTransforms[i]) children.Add(child);

                foreach (Transform child in children)
                {
                    GameObject childObj = child.gameObject;

                    // YENİ: Bu obje bir kart mı, yoksa efekt mi?
                    CardDisplay isCard = childObj.GetComponent<CardDisplay>();

                    if (isCard != null)
                    {
                        // EĞER KART İSE: İskartaya yolla
                        if (playerSlots[i] != null && deckManager != null)
                        {
                            if (playerSlots[i].cardType != CardType.Companion)
                                deckManager.AddToDiscard(playerSlots[i]);
                        }

                        if (deckManager != null)
                            deckManager.StartCoroutine(deckManager.AnimateCardToDiscard(childObj));
                        else
                            Destroy(childObj);
                    }
                    else
                    {
                        // EĞER SADECE EFEKT İSE (VFX_Element_Effect): Olduğu yerde direkt yok et
                        Destroy(childObj);
                    }
                }
            }
            playerSlots[i] = null;
        }
        yield return new WaitForSeconds(0.4f);
        RecalculateTotalEnergyCost();
    }

    // --- YENİ: DISSOLVE EFEKTİ VE KART YANMASI ---
    // --- YENİ: DISSOLVE EFEKTİ VE KART YANMASI ---
    IEnumerator FizzleCardRoutine(int slotIndex, Transform cardTransform, CardData card)
    {
        Debug.Log($"Yetersiz Enerji! {card.cardName} yanarak kül oluyor...");

        CardDisplay display = cardTransform.GetComponent<CardDisplay>();
        Material dissolveMat = null;

        if (display != null && display.artImage != null)
        {
            dissolveMat = new Material(display.artImage.material);
            display.artImage.material = dissolveMat;

            if (display.banderimage != null) display.banderimage.material = dissolveMat;
        }

        // 2. Burning (Yanma) Partikülünü Yarat
        GameObject burningEffect = null;
        if (dissolveBurningPrefab != null)
        {
            // Sadece bir kere, ölen kartın içine (çocuk olarak) yaratıyoruz!
            burningEffect = Instantiate(dissolveBurningPrefab, cardTransform);

            // Merkeze al ve arkada kalmasın diye Z eksenini -50'ye itele
            burningEffect.transform.localPosition = new Vector3(0, 0, -50f);
            burningEffect.transform.localScale = Vector3.one;

            BringEffectToFront(burningEffect);
        }

        // 3. Titreme ve Erime Döngüsü
        Vector3 basePos = cardTransform.localPosition;
        float duration = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            cardTransform.localPosition = basePos + new Vector3(Mathf.Sin(elapsed * 50f) * 3f, 0, 0);

            if (dissolveMat != null)
            {
                dissolveMat.SetFloat("_DissolveAmount", elapsed / duration);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (AudioManager.instance != null) AudioManager.instance.PlayCardDissolve();

        if (burningEffect != null) Destroy(burningEffect);

        DeckManager dm = DeckManager.instance != null ? DeckManager.instance : FindFirstObjectByType<DeckManager>();
        if (dm != null) dm.AddToDiscard(card);
        Destroy(cardTransform.gameObject);

        playerSlots[slotIndex] = null;
    }

    // Hangi elementlerin birbirine zıt (Anti) olduğunu belirler
}