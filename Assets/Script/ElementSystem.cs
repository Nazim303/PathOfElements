using UnityEngine;

// Element türlerini tanımlıyoruz
public enum ElementType
{
    None,
    // --- ANA ELEMENTLER ---
    Fire,
    Water,
    Earth,  // (Doğa)
    Air
}

public static class ElementSystem
{
    // Hasar çarpanını hesaplayan fonksiyon
    public static float GetDamageMultiplier(ElementType attacker, ElementType defender)
    {
        if (attacker == ElementType.None || defender == ElementType.None) return 1.0f;
        if (attacker == defender) return 1.0f;

        // --- 1. ANA ELEMENTLERİN DÖNGÜSÜ (Ateş > Toprak > Hava > Su > Ateş) ---
        // Kritik Vuruşlar (2.0x)
        if (attacker == ElementType.Fire && defender == ElementType.Earth) return 2.0f;
        if (attacker == ElementType.Earth && defender == ElementType.Air) return 2.0f;
        if (attacker == ElementType.Air && defender == ElementType.Water) return 2.0f;
        if (attacker == ElementType.Water && defender == ElementType.Fire) return 2.0f;

        // Zayıf Vuruşlar (0.5x)
        if (attacker == ElementType.Fire && defender == ElementType.Water) return 0.5f;
        if (attacker == ElementType.Water && defender == ElementType.Air) return 0.5f;
        if (attacker == ElementType.Air && defender == ElementType.Earth) return 0.5f;
        if (attacker == ElementType.Earth && defender == ElementType.Fire) return 0.5f;

        return 1.0f; // Diğer durumlar nötr
    }
}
