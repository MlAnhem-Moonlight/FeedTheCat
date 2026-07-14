using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class ChangeLanguageButton : MonoBehaviour
{
    [SerializeField]
    private Locale locale;

    public void ChangeLanguage()
    {
        StartCoroutine(ChangeLocale());
    }

    private IEnumerator ChangeLocale()
    {
        yield return LocalizationSettings.InitializationOperation;

        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;
        }
    }
}