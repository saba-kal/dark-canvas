using DarkCanvas.Misc;
using System;
using UnityEngine;

namespace DarkCanvas.Data
{
    public class UpdatableData : ScriptableObject
    {
        public event Action OnValuesUpdated;

        [SerializeField] private bool _autoUpdate;

        protected virtual void OnValidate()
        {
            ValidationUtility.SafeOnValidate(() =>
            {
                if (_autoUpdate)
                {
                    NotifyOfUpdatedValues();
                }
            });
        }

        public void NotifyOfUpdatedValues()
        {
            OnValuesUpdated?.Invoke();
        }
    }
}