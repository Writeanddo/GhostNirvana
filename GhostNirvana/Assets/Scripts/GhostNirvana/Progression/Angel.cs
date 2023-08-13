using UnityEngine;
using UnityEngine.Events;
using NaughtyAttributes;
using ScriptableBehaviour;
using System.Collections;
using DG.Tweening;
using Base;

namespace GhostNirvana {

public class Angel : MonoBehaviour {
    [SerializeField, Required] Miyu miyu;

    [SerializeField] ScriptableFloat time;
    [SerializeField] float gameEndTime;
    [SerializeField] MovableAgentRuntimeSet allEnemies;
    [SerializeField] MovableAgentRuntimeSet allAppliances;
    [SerializeField] float timeToEndSeconds;

    [Header("Animation")]
    [SerializeField] float timeSlowDownSeconds = 1;
    [SerializeField] Ease slowDownEase;
    [SerializeField] float respawnTime;

    [Header("Interaction")]
    [SerializeField] Canvas interactionCanvas;

    [Header("StatsScreen")]
    [SerializeField] Canvas statsCanvas;
    [SerializeField] UnityEvent onStatsStart;

    float impendingEnd;
    bool reincarnating;

    void Update() {
        if (impendingEnd > timeToEndSeconds || Time.timeScale < 1) return;

        bool timeUp =
            time.Value > gameEndTime 
            && !miyu.IsDead;
        bool noEnemies = allEnemies.Count == 0;
        bool canEnd = timeUp && noEnemies;
        if (canEnd) impendingEnd += Time.deltaTime;

        if (impendingEnd > timeToEndSeconds) StartCoroutine(IOnMiyuWin());
    }

    IEnumerator IOnMiyuWin() {
        Tween timeSlowDown = DOTween.To(
            setter: (value) => Time.timeScale = value,
            startValue: 1, endValue: 0, duration: timeSlowDownSeconds)
            .SetUpdate(isIndependentUpdate: true).SetEase(slowDownEase);
        timeSlowDown.Play();
        yield return timeSlowDown.WaitForCompletion();
        Time.timeScale = 0;
        onStatsStart?.Invoke();
        statsCanvas.gameObject.SetActive(true);
    }

    public void TriggerReincarnation() {
        if (reincarnating) return;
        reincarnating = true;
        // reload current level
        StartCoroutine(IReincarnation());
    }

    IEnumerator IReincarnation() {
        yield return new WaitForSecondsRealtime(respawnTime);
        App.instance.RequestLoad(gameObject.scene.name);
        Time.timeScale = 1;
    }
}

}
