using AI;
using CombatSystem;
using UnityEngine;
using Utils;

namespace GhostNirvana {
public class GhostyStateMachine : StateMachine<Ghosty, Ghosty.States> {
    public Ghosty Ghosty {get; private set;}

    void Awake() {
        Ghosty = GetComponent<Ghosty>();
        ConstructMachine(agent: Ghosty, defaultState: Ghosty.States.Seek);
    }

    void Start() => Init();

    public override void Construct() {
        AssignState<Ghosty.GhostySeek>(Ghosty.States.Seek);
        AssignState<Ghosty.GhostyPossessing>(Ghosty.States.Possessing);
        AssignState<Ghosty.GhostyPossessed>(Ghosty.States.Possessed);
        AssignState<Ghosty.GhostyDeath>(Ghosty.States.Death);
    }
}

public partial class Ghosty {

public class GhostySeek : State<Ghosty, Ghosty.States> {
    public override void Begin(StateMachine<Ghosty, States> stateMachine, Ghosty agent) {
        agent.OnPossession.AddListener(OnPossession);
    }

    public override void End(StateMachine<Ghosty, States> stateMachine, Ghosty agent) {
        agent.OnPossession.RemoveListener(OnPossession);
    }

    public override States? Update(StateMachine<Ghosty, States> stateMachine, Ghosty agent) {
        Vector3 desiredVelocity = agent.input.desiredMovement * agent.Status.BaseStats.MovementSpeed;

        agent.Velocity = Mathx.Damp(Vector3.Lerp, agent.Velocity, desiredVelocity,
                              (agent.Velocity.sqrMagnitude > desiredVelocity.sqrMagnitude)
                              ? agent.Status.BaseStats.DeccelerationAlpha : agent.Status.BaseStats.AccelerationAlpha,
                              Time.deltaTime);

        if (!Miyu.Instance || !Miyu.Instance.gameObject) return null;

        // TODO: rid of magic number
        float hitboxCheckingDistance = 2;
        bool closeToPlayer = (Miyu.Instance.transform.position - agent.transform.position).sqrMagnitude < hitboxCheckingDistance;
        if (closeToPlayer) agent.CheckForHits();

        bool isNotStationary = agent.Velocity != Vector3.zero;
        if (isNotStationary) agent.TurnToFace(agent.Velocity, agent.turnSpeed);

        return null;
    }

    void OnPossession(Ghosty ghosty, Appliance appliance) {
        GhostyStateMachine stateMachine = ghosty.StateMachine;
        stateMachine.Blackboard["possessingAppliance"] = appliance;
        stateMachine.SetState(States.Possessing);
    }
}

public class GhostyPossessing : State<Ghosty, Ghosty.States> {
    public override void Begin(StateMachine<Ghosty, States> stateMachine, Ghosty agent) {
        agent.OnDamage.AddListener(OnDamageTaken);
        agent.OnPossessionFinish.AddListener(OnPossessionFinish);
        agent.Velocity = Vector3.zero;
        agent.possessionCooldownActive = agent.possessionCooldownSeconds;
    }

    public override void End(StateMachine<Ghosty, States> stateMachine, Ghosty agent) {
        agent.OnDamage.RemoveListener(OnDamageTaken);
        agent.OnPossessionFinish.RemoveListener(OnPossessionFinish);
    }

    public override States? Update(StateMachine<Ghosty, States> stateMachine, Ghosty agent) {
        // try to be static, and face the appliance
        Appliance appliance = stateMachine.Blackboard["possessingAppliance"] as Appliance;

        agent.transform.position = Mathx.Damp(Vector3.Lerp,
            agent.transform.position, appliance.transform.position,
            agent.Status.BaseStats.AccelerationAlpha, Time.deltaTime);

        Vector3 directionToAppliance = appliance.transform.position - agent.transform.position;
        agent.TurnToFace(directionToAppliance, agent.turnSpeed);

        return null;
    }

    void OnDamageTaken(IHurtable hurtable, float damage, DamageType damageType) {
        if (!(hurtable is Ghosty)) return;

        Ghosty ghosty = hurtable as Ghosty;
        GhostyStateMachine stateMachine = ghosty.StateMachine;

        Appliance appliance = stateMachine.Blackboard["possessingAppliance"] as Appliance;

        appliance.OnPossessionInterupt?.Invoke(appliance);
        stateMachine.SetState(States.Seek);
    }

    void OnPossessionFinish(Ghosty ghosty) {
        GhostyStateMachine stateMachine = ghosty.StateMachine;
        Appliance appliance = stateMachine.Blackboard["possessingAppliance"] as Appliance;

        appliance.OnPossessionComplete?.Invoke(appliance);
        ghosty.StateMachine.SetState(States.Possessed);
    }
}

public class GhostyPossessed : State<Ghosty, Ghosty.States> {
    public override void Begin(StateMachine<Ghosty, States> stateMachine, Ghosty agent)
        => agent.Dispose();
}

public class GhostyDeath : State<Ghosty, Ghosty.States> {
    public override void Begin(StateMachine<Ghosty, States> stateMachine, Ghosty agent)
        => agent.Dispose();
}

}
}