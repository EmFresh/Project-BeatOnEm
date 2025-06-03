using UnityEngine;
using UnityEngine.Events;

public class HayYouSlappedSomthing : MonoBehaviour
{

    public UnityEvent onObjectHit = new UnityEvent();

    private void OnCollisionEnter(Collision collision)
    {
        //if(collision.gameObject.GetComponent<>() != null)
        {
            onObjectHit.Invoke();
        }
    }
}
