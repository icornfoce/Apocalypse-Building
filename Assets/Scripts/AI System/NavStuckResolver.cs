using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Simulation.AI
{
    /// <summary>
    /// Pathfinding helper that prevents NavMeshAgents (zombies & NPCs) from getting stuck.
    ///
    /// It is fully automatic and non-invasive — a self-spawning service attaches a
    /// <see cref="NavStuckResolver"/> to every NavMeshAgent in the scene, so none of the
    /// existing AI scripts need to change.
    ///
    /// What it fixes:
    ///   1) Avoidance deadlocks — every agent ships with the same avoidancePriority (50),
    ///      so two agents pushing into each other can freeze forever. We give each agent a
    ///      unique-ish priority.
    ///   2) Stuck / wedged agents — if an agent wants to move but its velocity stays ~0,
    ///      we recover with pathfinding: force a re-path, then (if still stuck) sample a
    ///      reachable point on the NavMesh toward the goal and Warp the agent there to
    ///      break the wedge — without overriding the AI's own destination.
    ///   3) Partial / off-mesh destinations — the recovery steers toward the nearest
    ///      reachable point instead of stalling on an unreachable target.
    /// </summary>
    [DisallowMultipleComponent]
    public class NavStuckResolver : MonoBehaviour
    {
        // ── Tunables ──
        private const float StuckSeconds   = 0.7f;   // how long "trying but not moving" before we act
        private const float MovingSqr      = 0.0025f; // velocity^2 above this = actually moving (~0.05 m/s)
        private const float NudgeForward   = 0.6f;   // how far toward the goal to warp
        private const float NudgeSide      = 0.9f;   // random sideways spread of the warp
        private const float SampleRadius   = 1.5f;   // navmesh sample radius for the warp point

        private NavMeshAgent _agent;
        private float _stuckTimer;
        private int _escalation;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            // Unique-ish priority breaks RVO deadlocks (lower = higher priority).
            if (_agent != null) _agent.avoidancePriority = Random.Range(20, 80);
        }

        private void Update()
        {
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
            {
                _stuckTimer = 0f; _escalation = 0;
                return;
            }

            // While a path is still being computed, don't judge "stuck" yet.
            if (_agent.pathPending) { _stuckTimer = 0f; return; }

            bool tryingToMove = !_agent.isStopped
                                && _agent.hasPath
                                && _agent.remainingDistance > _agent.stoppingDistance + 0.1f;
            bool actuallyMoving = _agent.velocity.sqrMagnitude > MovingSqr;

            if (tryingToMove && !actuallyMoving)
            {
                _stuckTimer += Time.deltaTime;
                if (_stuckTimer >= StuckSeconds)
                {
                    Recover();
                    _stuckTimer = 0f;   // give the recovery a moment to take effect
                }
            }
            else
            {
                _stuckTimer = 0f;
                _escalation = 0;
            }
        }

        /// <summary>Escalating recovery: re-path first, then physically nudge onto a reachable point.</summary>
        private void Recover()
        {
            _escalation++;
            Vector3 dest = _agent.destination;

            if (_escalation == 1)
            {
                // Cheap fix first: make sure we're not paused, and force a fresh path.
                _agent.isStopped = false;
                _agent.SetDestination(dest);
                return;
            }

            // Still stuck → break the wedge with a small Warp to a reachable NavMesh point,
            // biased toward the goal with a little random sideways offset.
            Vector3 toDest = dest - transform.position; toDest.y = 0f;
            Vector3 dir = toDest.sqrMagnitude > 0.0001f ? toDest.normalized
                                                        : new Vector3(Random.value - 0.5f, 0f, Random.value - 0.5f).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            Vector3 candidate = transform.position + dir * NudgeForward + side * Random.Range(-NudgeSide, NudgeSide);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, SampleRadius, _agent.areaMask))
            {
                _agent.Warp(hit.position);
            }

            // Shuffle priority again (in case several agents share the same value) and re-path.
            _agent.avoidancePriority = Random.Range(20, 80);
            _agent.isStopped = false;
            _agent.SetDestination(dest);

            if (_escalation > 4) _escalation = 0;   // loop the escalation rather than give up
        }
    }

    /// <summary>
    /// Lightweight singleton service that auto-attaches a <see cref="NavStuckResolver"/> to
    /// every NavMeshAgent as it appears (zombies/NPCs are spawned at runtime). Spawns itself
    /// on game start and persists across scene loads.
    /// </summary>
    [DisallowMultipleComponent]
    public class NavStuckService : MonoBehaviour
    {
        private static NavStuckService _instance;
        private float _scanTimer;
        private const float ScanInterval = 0.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("[NavStuckService]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<NavStuckService>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
        }

        private void Update()
        {
            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f) return;
            _scanTimer = ScanInterval;

            var agents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);
            for (int i = 0; i < agents.Length; i++)
            {
                if (agents[i] != null && agents[i].GetComponent<NavStuckResolver>() == null)
                    agents[i].gameObject.AddComponent<NavStuckResolver>();
            }
        }
    }
}
