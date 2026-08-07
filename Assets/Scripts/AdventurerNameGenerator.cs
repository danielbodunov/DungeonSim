using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AdventurerNameGenerator
{
    static readonly string[] FirstNames =
    {
        "Alden", "Bryn", "Cassia", "Dorian", "Elara", "Finn", "Greta", "Hale",
        "Iris", "Joren", "Kael", "Lina", "Mira", "Nolan", "Orin", "Petra",
        "Quinn", "Rowan", "Sable", "Tamsin", "Ulric", "Vera", "Wren", "Yara"
    };

    static readonly string[] Surnames =
    {
        "Ashford", "Blackbriar", "Brightshield", "Crowley", "Deepdelver", "Emberfall",
        "Fairwind", "Grimward", "Hawthorne", "Ironwood", "Keeneye", "Lightfoot",
        "Moonbrook", "Nightwell", "Oakheart", "Ravencrest", "Stonehand", "Swift",
        "Thornfield", "Vale", "Wintermere", "Wyrmwood"
    };

    public static NPCCharacterRecord Create(IReadOnlyCollection<string> existingNames)
    {
        string generated = null;
        for (int attempt = 0; attempt < 50; attempt++)
        {
            string candidate = FirstNames[UnityEngine.Random.Range(0, FirstNames.Length)] + " " +
                Surnames[UnityEngine.Random.Range(0, Surnames.Length)];
            if (existingNames == null || !existingNames.Contains(candidate))
            {
                generated = candidate;
                break;
            }
        }

        generated ??= FirstNames[UnityEngine.Random.Range(0, FirstNames.Length)] + " " +
            Surnames[UnityEngine.Random.Range(0, Surnames.Length)] +
            " " + UnityEngine.Random.Range(2, 100);

        return new NPCCharacterRecord
        {
            id = Guid.NewGuid().ToString("N"),
            characterName = generated
        };
    }
}
