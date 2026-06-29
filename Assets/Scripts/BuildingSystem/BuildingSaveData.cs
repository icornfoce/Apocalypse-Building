using System;
using System.Collections.Generic;
using UnityEngine;

namespace Simulation.Building
{
    [Serializable]
    public class BuildingSavePieceData
    {
        public string structureName;
        public string materialName;
        public bool isGadget;
        public Vector3 position;
        public float rotationY;
    }

    [Serializable]
    public class BuildingSaveData
    {
        public string id;
        public string displayName;
        public string thumbnailFileName;
        public long savedAtTicks;
        public List<BuildingSavePieceData> pieces = new List<BuildingSavePieceData>();
    }

    [Serializable]
    public class BuildingSaveSummary
    {
        public string id;
        public string displayName;
        public string thumbnailPath;
        public int pieceCount;
        public long savedAtTicks;
    }
}
