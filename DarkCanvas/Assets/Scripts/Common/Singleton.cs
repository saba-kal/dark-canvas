using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkCanvas.Common
{
    /// <summary>
    /// Singleton pattern implementation for Unity MonoBehaviour classes.
    /// </summary>
    public class Singleton<T> : MonoBehaviour
    {
        private static Dictionary<Type, object> _singletons
            = new Dictionary<Type, object>();

        protected virtual void Awake()
        {
            // If there is an instance, and it's not me, delete myself.
            if (_singletons.ContainsKey(GetType()))
            {
                Destroy(this);
            }
            else
            {
                _singletons.Add(GetType(), this);
            }
        }

        /// <summary>
        /// Gets current singleton instance.
        /// </summary>
        public static T Instance
        {
            get
            {
                return (T)_singletons[typeof(T)];
            }
        }
    }
}