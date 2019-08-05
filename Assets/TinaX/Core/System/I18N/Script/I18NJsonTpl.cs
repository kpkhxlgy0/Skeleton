﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TinaX.I18N
{
    public class I18NJsonTpl
    {
        public S_KV[] data;

        [System.Serializable]
        public struct S_KV
        {
            public string key;
            public string value;
        }
    }
}

