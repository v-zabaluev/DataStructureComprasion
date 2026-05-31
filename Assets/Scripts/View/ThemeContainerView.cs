using System;
using UnityEngine;
using UnityEngine.UI;

namespace View
{
    public class ThemeContainerView : MonoBehaviour
    {
        [SerializeField] private Button _groupActivationButton;
        [SerializeField] private Transform _parameterGroup;

        private void Awake()
        {
            _parameterGroup.gameObject.SetActive(false);

            _groupActivationButton.onClick.AddListener(() =>
            {
                _parameterGroup.gameObject.SetActive(!_parameterGroup.gameObject.activeSelf);
            });
        }
    }
}