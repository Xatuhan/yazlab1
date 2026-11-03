using UnityEngine;

// Oyuncu modelinin yürüme animasyonu ve ayak sesi
[RequireComponent(typeof(Animator))]
public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private AudioClip[] footsteps = new AudioClip[0];
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.5f;

    private static readonly int SpeedId = Animator.StringToHash("Speed");

    private Animator animator;
    private AudioSource audioSource;
    private Transform body;
    private Vector3 lastPosition;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        body = transform.parent != null ? transform.parent : transform;
        lastPosition = body.position;
    }

    private void Update()
    {
        // hız konumdan ölçülür: sohbette hareket kapanınca robot yerinde yürümesin
        if (Time.deltaTime <= 0f) return;
        Vector3 delta = body.position - lastPosition;
        delta.y = 0f;
        lastPosition = body.position;
        animator.SetFloat(SpeedId, delta.magnitude / Time.deltaTime, 0.1f, Time.deltaTime);
    }

    // Walk ve Run kliplerindeki animasyon olayı çağırır
    private void OnFootstep(AnimationEvent animationEvent)
    {
        // karışımda iki klip birden adım atmasın
        if (animationEvent.animatorClipInfo.weight < 0.5f) return;
        if (audioSource == null || footsteps.Length == 0) return;
        audioSource.PlayOneShot(footsteps[Random.Range(0, footsteps.Length)], footstepVolume);
    }
}
