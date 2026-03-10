using UnityEngine;

// Element türlerini tanımlıyoruz
public enum ElementType
{
    None,
    // --- ANA ELEMENTLER ---
    Fire,
    Water,
    Earth,  // (Doğa)
    Air,

    // --- YAN ELEMENTLER (FİZYON) ---
    Magma,  // Ateş + Doğa
    Steam,  // Su + Ateş
    Ice,    // Su + Hava
    Dust    // Doğa + Hava
}

// ElementSystem.cs sınıfının kalan kısmı (GetDamageMultiplier) aynı kalacak.
public static class ElementSystem
{
    // Hasar çarpanını hesaplayan fonksiyon
    public static float GetDamageMultiplier(ElementType attacker, ElementType defender)
    {
        // Örnek Mantık: Ateş > Toprak > Hava > Su > Ateş (Bu döngüyü tasarımına göre değiştirebilirsin)

        if (attacker == ElementType.Fire && defender == ElementType.Earth) return 2.0f; // Kritik
        if (attacker == ElementType.Earth && defender == ElementType.Air) return 2.0f;
        if (attacker == ElementType.Air && defender == ElementType.Water) return 2.0f;
        if (attacker == ElementType.Water && defender == ElementType.Fire) return 2.0f;

        // Zayıf vuruşlar (Resistance)
        if (attacker == ElementType.Fire && defender == ElementType.Water) return 0.5f;

        // Su -> Havaya vurursa (Zayıf)
        if (attacker == ElementType.Water && defender == ElementType.Air) return 0.5f;

        // Hava -> Toprağa vurursa (Zayıf)
        if (attacker == ElementType.Air && defender == ElementType.Earth) return 0.5f;

        // Toprak -> Ateşe vurursa (Zayıf)
        if (attacker == ElementType.Earth && defender == ElementType.Fire) return 0.5f;

        return 1.0f; // Nötr
        if (attacker == ElementType.Water && defender == ElementType.Air) return 0.5f;
        // ... diğer zayıflıklar buraya

       
    }
}