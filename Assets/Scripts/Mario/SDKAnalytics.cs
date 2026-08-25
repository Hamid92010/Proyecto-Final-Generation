using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
//Hola Mundo
/// <summary>
/// Inicializa Unity Gaming Services y arranca la recolección de Analytics.
/// Colócalo en un GameObject que exista desde el arranque del juego
/// (por ejemplo tu _SceneController o un objeto con DontDestroyOnLoad),
/// para que se ejecute una sola vez y antes de cualquier tracker.
/// </summary>
public class SDKAnalytics : MonoBehaviour
{
    public static SDKAnalytics Instance { get; private set; }
    public bool IsReady { get; private set; }

    private async void Awake()
    {
        // Evita duplicados si cambias de escena
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        await InitializeAnalytics();
    }

    private async Task InitializeAnalytics()
    {
        try
        {
            await UnityServices.InitializeAsync();

            // Arranca la recolección de datos (necesario, aunque no uses login de jugador)
            AnalyticsService.Instance.StartDataCollection();

            IsReady = true;
            Debug.Log("[Analytics] Inicializado correctamente. SDK listo para enviar eventos.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Analytics] Error al inicializar: {e}");
        }
    }

    /// <summary>
    /// Ejemplo de cómo mandar un evento custom desde cualquier otro script:
    /// AnalyticsInitializer.Instance.TrackEvent("button_click", new Dictionary<string, object> { { "button_name", "start" } });
    /// Nota: el nombre del evento (ej. "button_click") debe existir como schema
    /// en el Event Manager del Dashboard, si no, no aparecerá como válido.
    /// </summary>
    public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
    {
        if (!IsReady)
        {
            Debug.LogWarning("[Analytics] Intentaste mandar un evento antes de inicializar el SDK.");
            return;
        }

        try
        {
            var customEvent = new CustomEvent(eventName);
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    customEvent[kvp.Key] = kvp.Value;
                }
            }

            AnalyticsService.Instance.RecordEvent(customEvent);
        }
        catch (System.Exception e)
        {
            // Puede pasar al cerrar la app / detener el modo Play: Unity ya
            // apagó los servicios aunque IsReady siga en true desde que se
            // inicializaron, así que el acceso a AnalyticsService.Instance
            // lanza en vez de simplemente no estar listo.
            Debug.LogWarning($"[Analytics] No se pudo mandar el evento '{eventName}': {e.Message}");
        }
    }
}
