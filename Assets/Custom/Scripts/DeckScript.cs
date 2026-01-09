using System.Collections.Generic;
using UnityEngine;

public class DeckScript : MonoBehaviour
{
    [Tooltip("Optional: collect child card GameObjects automatically on Awake.")]
    public bool collectChildrenOnAwake = true;

    [Tooltip("Horizontal offset (local X) between subsequently drawn cards when attached to the hand.")]
    public float drawOffset = 4f;

    // Internal ordered list of card GameObjects
    private List<GameObject> cards = new List<GameObject>();

    // Next index to draw from `cards`
    private int drawIndex = 0;

    // Last card that was drawn (can be used by editor scripts)
    public GameObject lastDrawnCard { get; private set; }

    void Awake()
    {
        if (collectChildrenOnAwake)
        {
            CollectChildren();
        }
        // Ensure all cards start invisible and at default local position
        ResetDeck();
        Shuffle();
    }

    // Collect child GameObjects under this deck (keeps current sibling order)
    public void CollectChildren()
    {
        cards.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
                cards.Add(child.gameObject);
        }
    }

    // Fisher-Yates shuffle
    [ContextMenu("Shuffle Deck")]
    public void Shuffle()
    {
        if (cards == null || cards.Count <= 1)
            return;

        var rnd = new System.Random();
        int n = cards.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            // swap
            var temp = cards[i];
            cards[i] = cards[j];
            cards[j] = temp;
        }

        // After shuffling, drawing should start from the top of the new order
        drawIndex = 0;
    }

    // Draw the next card in the order: activate it and move along local Z by drawZOffset
    [ContextMenu("Draw Card")]
    public void Draw()
    {
        if (cards == null || drawIndex >= cards.Count)
        {
            Debug.Log("DeckScript: No cards left to draw.");
            return;
        }

        GameObject card = cards[drawIndex];
        if (card == null)
        {
            drawIndex++;
            Draw();
            return;
        }

        card.SetActive(true);
        lastDrawnCard = card;
        Debug.Log("DeckScript: Drew card '" + card.name + "'.");
        
        // If a PlayerHand exists (tagged 'CardHand'), parent the card to it and position locally above the hand.
        var hand = GameObject.FindWithTag("CardHand");
        if (hand != null)
        {
            // Compute the desired parent position and rotation for the card based on the hand's transform
            
            // Parent while preserving the child's world transform, then place it exactly where we want.
            card.transform.SetParent(hand.transform, false);
            card.transform.localPosition = new Vector3(0f, drawOffset, 0f);
            drawOffset -= 2f;
        }
        else
        {
            //Vector3 desiredWorldPos = card.transform.position + new Vector3(0f, drawYOffset * 2f, 0f);
            //card.transform.SetParent(null, true);
            //card.transform.position = desiredWorldPos;
        }

        // Disable physics colliders and make Rigidbody kinematic while the card is held/attached.
        var colliders = card.GetComponentsInChildren<Collider>();
        foreach (var c in colliders)
            c.enabled = false;

        var rb = card.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        drawIndex++;
    }

    // Reset all cards to local position (0,0,0) and set them inactive
    [ContextMenu("Reset Deck")]
    public void ResetDeck()
    {
        if (cards == null)
            CollectChildren();

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card == null)
                continue;

            card.transform.localPosition = Vector3.zero;
            card.transform.localEulerAngles = Vector3.zero;
            card.SetActive(false);
        }

        drawIndex = 0;
        drawOffset = 4f;

    }

    // Optional: expose a simple inspector method to shuffle then draw (helpful for quick testing)
    [ContextMenu("Shuffle And Draw One")]
    public void ShuffleAndDrawOne()
    {
        Shuffle();
        Draw();
    }
}
