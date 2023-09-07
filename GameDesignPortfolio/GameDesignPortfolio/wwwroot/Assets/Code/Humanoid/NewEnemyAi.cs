using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


enum EnemyStates
{
    moveToEndGoal,  // Standard state for moving to the endGoal
    attackTurret,   // State for attacking the turrit it detected
    atEndGoal,      // State for when the enemy is at the EndGoal
    attackOnce,
    attackForcefield,
    walkToforcefield,
    wait
};

public class NewEnemyAi : MonoBehaviour
{
    [SerializeField]
    private AudioClip sweepAttack;

    public int cost = 10;

    private NavMeshAgent agent;

    private Vector3 Endgoal;

    private TurretHandler turretHandler;

    private GameObject currentWall;

    bool attackforcefield;


    [SerializeField] private EnemyStates currentState;

    public float iTimer;
    public float iEnd;

    bool draw = false;

    [Header("AttackStyle")]
    public bool once;
    public bool Multiple;

    public float fireRate = 1.5F;
    public float nextFire = 0.0F;

    public List<Collider> Targets = new List<Collider>();
    public List<GameObject> AlreadyAttacked = new List<GameObject>();
    GameObject turretToAttack;

    GameObject forcefieldToAttack;

    [SerializeField]
    protected Transform enemyBulletPrefab;


    public LayerMask layerMask;

    Animator Anim;

    private bool canshoot = true;

    public EndGoal endgoal;

    private void Start()
    {
        Anim = gameObject.GetComponent<Animator>();
        Endgoal = endgoal.transform.position;
        turretHandler = GameObject.Find("TurretManager").GetComponent<TurretHandler>();
        agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(Endgoal);
    }

    private void Update()
    {
        StateHandler();
    }

    private void Detection()
    {
        iTimer += Time.deltaTime;

        if (iTimer >= iEnd)
        {
            iTimer = 0;
            iEnd = Random.Range(5, 7);
            Targets.AddRange(Physics.OverlapSphere(this.transform.position, 10, layerMask, QueryTriggerInteraction.UseGlobal));
            turretToAttack = GetClosestTurret();
            if (Targets.Count != 0)
            {
                if (Vector3.Distance(this.transform.position, turretToAttack.transform.position) <= 7)
                {
                    if (once)
                    {
                        if (AlreadyAttacked.Count == 0)
                        {
                            agent.ResetPath();
                            agent.SetDestination(turretToAttack.transform.position);
                            currentState = EnemyStates.attackOnce;
                        }
                        else
                        {
                            foreach (var item in AlreadyAttacked)
                            {
                                if (turretToAttack.transform.position != item.transform.position)
                                {
                                    agent.ResetPath();
                                    agent.SetDestination(turretToAttack.transform.position);
                                    currentState = EnemyStates.attackOnce;
                                }
                            }
                        }
                    }
                    if (Multiple)
                    {
                        agent.ResetPath();
                        agent.SetDestination(turretToAttack.transform.position);
                        currentState = EnemyStates.attackTurret;
                    }
                }
                else
                {
                    currentState = EnemyStates.moveToEndGoal;
                }
            }
        }
    }

    private GameObject GetClosestTurret()
    {
        GameObject tMin = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = transform.position;
        foreach (var target in Targets)
        {
            if (target != null)
            {
                float dist = Vector3.Distance(target.transform.position, currentPos);
                if (dist < minDist)
                {
                    tMin = target.gameObject;
                    minDist = dist;
                }
            }
        }
        currentWall = tMin;
        return tMin;
    }

    private void StateHandler()
    {
        switch (currentState)
        {
            case EnemyStates.moveToEndGoal:
                agent.Resume();

                Anim.Play("Walking");

                Detection();

                Targets.Clear();

                agent.SetDestination(Endgoal);
                if (agent.pathStatus == NavMeshPathStatus.PathPartial)
                {
                    currentState = EnemyStates.walkToforcefield;
                }
                else
                {

                    if (Vector3.Distance(this.transform.position, endgoal.transform.position) <= 5)
                    {
                        currentState = EnemyStates.atEndGoal;
                    }
                }

                break;
            case EnemyStates.attackTurret:
                if (turretToAttack != null)
                {
                    agent.transform.LookAt(turretToAttack.transform.position);
                }
                if (canshoot)
                {
                    StartCoroutine(AttackTurret());
                }
                break;
            case EnemyStates.atEndGoal:

                endgoal.setEndGoalLivesMinusOne();
                Destroy(this.gameObject);

                break;
            case EnemyStates.attackOnce:
                agent.transform.LookAt(turretToAttack.transform.position);
                if (canshoot)
                {
                    StartCoroutine(AttackOnce());
                }
                break;
            case EnemyStates.attackForcefield:

                if (forcefieldToAttack == null)
                {
                    currentState = EnemyStates.moveToEndGoal;
                }

                if (canshoot)
                {
                    StartCoroutine(AttackForceField());
                }
                break;
            case EnemyStates.walkToforcefield:

                WalkToForceField();

                break;
            case EnemyStates.wait:
                Wait();
                break;
            default:
                break;
        }
    }

    private void Wait()
    {
        agent.Stop();
        Anim.Play("Idle");
    }
    private void WalkToForceField()
    {
        Anim.Play("Walking");

        forcefieldToAttack = searchForceField(turretHandler.Walls);

/*        agent.SetDestination(forcefieldToAttack.transform.position);*/

        NavMeshHit hit;
        if (NavMesh.SamplePosition(forcefieldToAttack.transform.position, out hit, 5, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        if (Vector3.Distance(agent.transform.position, forcefieldToAttack.transform.position) <= 4)
        {
            if (once || Multiple)
            {
                currentState = EnemyStates.attackForcefield;
            }
            else
            {
                currentState = EnemyStates.wait;
            }
        }

    }



    private IEnumerator AttackOnce()
    {
        canshoot = false;

        agent.Stop();

        agent.transform.LookAt(turretToAttack.transform.position);

        Anim.SetTrigger("Shoot");

        yield return new WaitForSeconds(Anim.GetCurrentAnimatorStateInfo(0).length * Anim.GetCurrentAnimatorStateInfo(0).speed);

        Shoot(turretToAttack.transform.position);

        yield return new WaitForSeconds(0.5f);
        canshoot = true;
        currentState = EnemyStates.moveToEndGoal;
    }


    private IEnumerator AttackTurret()
    {
        canshoot = false;

        agent.Stop();

        if (turretToAttack != null)
        {
            agent.transform.LookAt(turretToAttack.transform.position);
        }

        Anim.SetTrigger("Shoot");

        yield return new WaitForSeconds(Anim.GetCurrentAnimatorStateInfo(0).length * Anim.GetCurrentAnimatorStateInfo(0).speed);

        if (turretToAttack != null)
        {
            Shoot(turretToAttack.transform.position);
        }
        else
        {
            canshoot = true;
            currentState = EnemyStates.moveToEndGoal;
        }

        canshoot = true;

    }

    private void Shoot(Vector3 Position)
    {
        SoundManager.instance.playEnemy(sweepAttack);
        var bullet = Instantiate(enemyBulletPrefab);
        bullet.transform.position = this.transform.position;
        bullet.transform.position += new Vector3(0, 3, 0);
        bullet.transform.LookAt(Position);
    }

    private IEnumerator AttackForceField()
    {
        if (Multiple || once)
        {
            canshoot = false;

            agent.Stop();

            forcefieldToAttack = searchForceField(turretHandler.Walls);

            agent.transform.LookAt(forcefieldToAttack.transform.position);

            Anim.SetTrigger("Shoot");

            yield return new WaitForSeconds(Anim.GetCurrentAnimatorStateInfo(0).length * Anim.GetCurrentAnimatorStateInfo(0).speed);

            if (forcefieldToAttack != null)
            {
                Shoot(forcefieldToAttack.transform.position);
            }
            else
            {
                canshoot = true;
                currentState = EnemyStates.moveToEndGoal;
            }
            canshoot = true;
        }

        else
        {
            agent.Stop();
            Anim.Play("Idle");
        }
    }

    GameObject searchForceField(List<GameObject> walls)
    {
        GameObject tMin = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = transform.position;
        foreach (GameObject wall in walls)
        {
            if (wall != null)
            {
                float dist = Vector3.Distance(wall.transform.position, currentPos);
                if (dist < minDist)
                {
                    tMin = wall;
                    minDist = dist;
                }
            }
        }
        currentWall = tMin;
        return tMin;
    }
}
