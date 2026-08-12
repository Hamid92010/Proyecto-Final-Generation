using UnityEngine;

public class CameraFollow_02 : MonoBehaviour
{
  public Transform target;
   public Vector3 offset = new Vector3(0f, 5f, -8f);
   public float smoothSpeed = 5f;
   public float fixedZ = 47f; // El valor de Z donde quieres que la cámara se quede fija

   void LateUpdate(){
        if (target == null){
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.z = fixedZ; // Forzamos Z antes de interpolar

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target);
   }
}
