using System.Collections.Generic;
using UnityEngine;

// ---------------------------------------------------------------------------
// Receptor de los Animation Events de VFX.
//
// Las animaciones de Bravard y de la palanca llevan eventos incrustados que
// llaman a los metodos de abajo por su nombre. Este script no decide CUANDO se
// ve un efecto (eso lo marca el frame exacto del clip, que es justo la gracia):
// solo traduce cada llamada al ParticleSystem que le corresponde.
//
// IMPORTANTE: un Animation Event solo llega a los componentes del MISMO
// GameObject que tiene el Animator. En este proyecto eso significa:
//
//   - el hijo "Bravard" del Player  -> los seis efectos del personaje
//   - el hijo "Lever" del LeverPlatform -> solo el de la palanca
//
// Cada copia deja vacias las casillas que no le tocan; solo se avisa de una
// casilla vacia si su evento llega a dispararse de verdad.
// ---------------------------------------------------------------------------
public class AnimationVFXEvents : MonoBehaviour
{
    [Header("Bravard")]
    [Tooltip("VFX_DoubleJump. Salta en la anticipacion del salto aereo.")]
    [SerializeField] private ParticleSystem doubleJumpVFX;

    [Tooltip("VFX_BeingHit. Salta en la reaccion al golpe.")]
    [SerializeField] private ParticleSystem hitVFX;

    [Tooltip("VFX_GroundHit. Salta al aterrizar.")]
    [SerializeField] private ParticleSystem groundHitVFX;

    [Tooltip("VFX_Flying. Se repite mientras sube y mientras cae: los dos clips son en bucle.")]
    [SerializeField] private ParticleSystem flyingVFX;

    [Tooltip("VFX_Steps. Dos veces por ciclo de carrera, una por pisada.")]
    [SerializeField] private ParticleSystem stepsVFX;

    [Tooltip("VFX_WaterDefeat. Se repite mientras dura la animacion de ahogarse, que es en bucle.")]
    [SerializeField] private ParticleSystem waterVFX;

    [Header("Palanca")]
    [Tooltip("VFX_LeverActivated. Es el unico que necesita la palanca; los de arriba se dejan vacios en ella.")]
    [SerializeField] private ParticleSystem leverVFX;

    // Eventos de los que ya se ha avisado. Los pasos y el vuelo se disparan en bucle,
    // asi que sin esto una sola casilla sin asignar llenaria la consola hasta dejarla
    // inservible y taparia cualquier otro error de la partida.
    private readonly HashSet<string> warnedEvents = new HashSet<string>();

    private void Awake()
    {
        // El fallo mas facil de cometer con esto es colgarlo del objeto padre. Los
        // eventos irian al hijo con el Animator, no llegarian aqui, y no habria ningun
        // sintoma que lo explicase: los efectos simplemente no saldrian. Mejor decirlo.
        if (GetComponent<Animator>() == null)
        {
            Debug.LogError($"{nameof(AnimationVFXEvents)} esta en '{name}', que no tiene Animator. Los Animation Events solo llegan al GameObject del Animator, asi que ningun VFX se reproducira. Muevelo al hijo que lo tenga.", this);
        }
    }

    // --- Metodos que llaman los Animation Events ---------------------------
    // Los nombres los fija el FBX. Renombrar uno aqui no rompe la compilacion, pero
    // el evento deja de encontrarlo y Unity se queja en CADA reproduccion del clip.

    public void PlayDoubleJumpVFX()
    {
        PlayEffect(doubleJumpVFX, nameof(PlayDoubleJumpVFX));
    }

    public void PlayHitVFX()
    {
        PlayEffect(hitVFX, nameof(PlayHitVFX));
    }

    public void PlayGroundHitVFX()
    {
        PlayEffect(groundHitVFX, nameof(PlayGroundHitVFX));
    }

    public void PlayFlyingVFX()
    {
        PlayEffect(flyingVFX, nameof(PlayFlyingVFX));
    }

    public void PlayStepsVFX()
    {
        PlayEffect(stepsVFX, nameof(PlayStepsVFX));
    }

    public void PlayWaterVFX()
    {
        PlayEffect(waterVFX, nameof(PlayWaterVFX));
    }

    public void PlayLeverVFX()
    {
        PlayEffect(leverVFX, nameof(PlayLeverVFX));
    }

    // Lanza una rafaga del sistema indicado
    private void PlayEffect(ParticleSystem effect, string eventName)
    {
        if (effect == null)
        {
            WarnOnce(eventName);
            return;
        }

        // Play() sobre un sistema que YA esta en marcha no hace nada, y entonces la
        // segunda pisada de la carrera no se veria. Parar primero lo rebobina para que
        // cada evento suelte una rafaga limpia.
        //
        // StopEmitting (y no StopAndClear) es lo que hace que la rafaga anterior siga
        // viva en pantalla: se corta la emision nueva, pero las particulas ya soltadas
        // terminan su vida. Sin eso, cada pisada borraria de golpe la polvareda de la
        // anterior en lugar de acumularse con ella.
        effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        effect.Play(true);
    }

    // Avisa una sola vez por evento
    private void WarnOnce(string eventName)
    {
        if (!warnedEvents.Add(eventName))
        {
            return;
        }

        Debug.LogWarning($"El evento {eventName} se disparo en '{name}' pero su ParticleSystem no esta asignado en el Inspector. No se volvera a avisar de este.", this);
    }
}
