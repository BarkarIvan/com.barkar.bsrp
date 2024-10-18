using UnityEngine;

namespace Barkar.BSRP.Settings
{
    [System.Serializable]
    public class TonemappingSettings 
    {
        public enum Tonemapping
        {
            ACES = 1,
            GrandTurismo = 2,
            PBRNeutral = 3,
        }

        public Tonemapping Tonemap;
    }
    
    
}