using System;

namespace Unlimited_NetworkingServer_MiningGame.Game
{
    public static class NameGenerator
    {
        private static Random _rand = new Random();
        public static string RandName()
        {
            string[] mineNames = 
            {
                " Rann",
                " Thanagar",
                " Oa",
                " Korugar",
                " Tamaran",
                " Krypton",
                " Warworld",
                " Mogo",
                " Qward",
                " Apokolips",
                " New Genesis",
                " Biot",
                " Corona Seven",
                " Xanshi",
                " Ryut",
                " Ungara",
                " Betrassus",
                " Xanador",
                " Daxam",
                " Karna",
                " Hny'xx",
                " Okaara",
                " Voorl",
                " Euphorix",
                " Slagg",
                " Changralyn",
                " Bizarro World",
                " Durla",
                " Colu",
                " Ancar",
                " Bellatrix",
                " Ysmault",
                " Seekwom",
                " Kautnom",
                " Odym",
                " Maltus",
                " Havania",
                " Fourscore",
                " Vora",
                " Grenda",
                " Xanthu",
                " Trom",
                " Braal",
                " Rimbor",
                " Slumburg",
                " Sorca",
                " Gala",
                " Proxima Centauri",
                " Raeth",
                " Terrana",
                " Calados",
                " Vorrin-Tog",
                " Babylon",
                " Kadar Zee",
                " Krod",
                " Thebor",
                " Slann",
                " Khera",
                " Hagar-Way",
                " Ghan IX",
                " Nerro",
                " Gllyn",
                " Cetus",
                " Antares",
                " Xtar",
                " Krill",
                " Medusa",
                " Geminius",
                " Wengaren",
                " Khip Vool",
                " Vartu",
                " Panoptes",
                " Naltor",
                " Winath",
                " Orando",
                " Imsk",
                " Lallor",
                " Talok VIII",
                " Takron-Galtos",
                " Starhaven",
                " Bolovax Vik",
                " Almerac",
                " Kalanor",
                " Weber's World",
                " Cairn",
                " Zamaron",
                " Cargg",
                " Zur-En-Arrh",
                " Xudar",
                " J586",
                " Hykraius",
                " Graxos IV",
                " Bgztl",
                " Bismoll",
                " Bunyon's World",
                " Cyrem",
                " Gyrich",
                " Kar Zagas",
                " Klorra",
                " Murgador"
            };
            
            return mineNames[_rand.Next(0, mineNames.Length - 1)];
        }
    }
}