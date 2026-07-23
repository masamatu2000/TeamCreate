using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ‘S‚Ä‚Ì‚¨‹q‚³‚ñ‚ğŠÇ—‚·‚é
/// </summary>
public class CustomerManager : MonoBehaviour
{
    [Header("‚¨‹q‚³‚ñˆê——")]
    [SerializeField] private Customer[] customers;

    [Header("“D–_‚Ìl”")]
    [SerializeField] private int thiefCount = 3;

    private void Start()
    {
        DecideThieves();
    }

    /// <summary>
    /// “D–_‚ğƒ‰ƒ“ƒ_ƒ€‚ÉŒˆ’è‚·‚é
    /// </summary>
    private void DecideThieves()
    {
        if (customers == null || customers.Length == 0)
        {
            Debug.LogWarning("‚¨‹q‚³‚ñ‚ª“o˜^‚³‚ê‚Ä‚¢‚Ü‚¹‚ñ");
            return;
        }

        if (thiefCount > customers.Length)
        {
            thiefCount = customers.Length;
        }

        // ˆê“x‘Sˆõ‚ğˆê”Ê‹q‚É‚·‚é
        foreach (Customer customer in customers)
        {
            customer.SetThief(false);
        }

        // ‚¨‹q‚³‚ñ‚Ìˆê——‚ğƒRƒs[
        List<Customer> shuffledCustomers =
            new List<Customer>(customers);

        // ƒVƒƒƒbƒtƒ‹
        for (int i = 0; i < shuffledCustomers.Count; i++)
        {
            int randomIndex =
                Random.Range(i, shuffledCustomers.Count);

            Customer temp = shuffledCustomers[i];
            shuffledCustomers[i] = shuffledCustomers[randomIndex];
            shuffledCustomers[randomIndex] = temp;
        }

        // æ“ª‚©‚ç“D–_‚É‚·‚é
        for (int i = 0; i < thiefCount; i++)
        {
            shuffledCustomers[i].SetThief(true);
        }
    }
}