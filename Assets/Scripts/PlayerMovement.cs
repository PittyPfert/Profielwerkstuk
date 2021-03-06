﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace Profielwerkstuk
{
    public class PlayerMovement : MonoBehaviour
    {
        public bool participating = true;
        public NavMeshAgent agent;
        public TaskManager taskManager;
        public NavMeshObstacle obstacle;
        public CapsuleCollider triggerCollider;
        public Vector3 target;
        public string status;
        public List<GameObject> waitingFor;
        public GameObject coughCloudPrefab;
        public Material infectedMaterial;
        public Material asymptomaticMaterial;
        public Transform coughCloudParent;
        public bool infected = false;
        public bool asymptomatic = false;
        public bool waiting;
        private float timeSinceLastCough = 0.0f;
        private float timeUntilCough = 0.0f;
        // public Camera cam;
        public string id;
        public DataHoarder dataHoarder;
        public RegisterManager registerManager;
        public Vector3 register;
        public bool unableToReachTarget = false;
        float timeSinceLastCheck = 0.0f;
        private int timesChecked = 0;
        void Start()
        {
            taskManager = new TaskManager(agent);
            timeUntilCough = Random.Range(Config.minCough, Config.maxCough);
            obstacle.enabled = false;
            agent.autoBraking = false;
            status = "ASSIGNING";
            waiting = false;
            register = new Vector3();
            waitingFor = new List<GameObject>();
        }

        // Update is called once per frame
        void Update()
        {
            /*if (Input.GetMouseButtonDown(0))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                Physics.Raycast(ray, out hit);
                target = hit.point;
                agent.SetDestination(target);
            }*/
            if (status == "ASSIGNING") return;

            if(timesChecked > 0 && !unableToReachTarget)
            {
                timesChecked = 0;
            }
            if(timesChecked >= 10)
            {
                transform.parent.GetComponent<FlowManager>().OnPlayerLeave();
                registerManager.OnLeaveRegister(register);
                dataHoarder.OnLeave(id, infected);
                print(name + " couldn't reach any targets, so he left. :'(");
                Destroy(gameObject);
            }
            if (infected && !asymptomatic)
            {
                // print(timeSinceLastCough);
                timeSinceLastCough += Time.deltaTime;
                if(timeUntilCough < timeSinceLastCough)
                {
                    timeSinceLastCough = 0.0f;
                    timeUntilCough = Random.Range(Config.minCough, Config.maxCough);
                    GameObject p = Instantiate(coughCloudPrefab, transform.position, transform.rotation, coughCloudParent);
                    p.transform.GetComponent<Rigidbody>().velocity = transform.forward * 0.7f;
                }
            }
            if (unableToReachTarget && timeSinceLastCheck > 10)
            {
                timeSinceLastCheck = 0.0f;
                StartCoroutine(CheckForTarget());
                timesChecked++;
            }
            if (unableToReachTarget)
            {
                timeSinceLastCheck += Time.deltaTime;
                return;
            }


            // check if agent has reached goal
            if (agent.enabled && !waiting && !unableToReachTarget)
            {
                var pos = transform.position;
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        // Utility.PrintVector(pos);
                        // Utility.PrintVector(target);
                        if (status == "ACTIVE") taskManager.RemoveTask(target);
                        if (taskManager.Tasks.Count == 0)
                        {
                            if (status == "ACTIVE")
                            {
                                if (registerManager.WaitForRegister(this, out target))
                                    status = "CHECKING OUT";
                                else
                                    status = "WAITING FOR REGISTER";

                                // print(name + " " + status);
                                StartCoroutine(WaitForNextTask(Random.Range(5, 15)));
                            }
                            else if (status == "WAITING FOR REGISTER")
                            {
                                waiting = true;
                                if (participating)
                                {
                                    StartCoroutine(ActivateObstacle());
                                }
                            }
                            else if (status == "CHECKING OUT")
                            {
                                register = target;
                                target = taskManager.leavingPos;
                                status = "LEAVING";
                                StartCoroutine(WaitForNextTask(Random.Range(20, 40)));
                            }
                            else if (status == "LEAVING")
                            {
                                transform.parent.GetComponent<FlowManager>().OnPlayerLeave();
                                dataHoarder.OnLeave(id, infected);
                                // print(name + " HAS LEFT");
                                Destroy(gameObject);
                                return;
                            }
                        }
                        else
                        {
                            if (taskManager.GetTask(out target))
                            {
                                // yay
                            }
                            else
                            {
                                // fuk
                                unableToReachTarget = true;
                                if (participating)
                                {
                                    StartCoroutine(ActivateObstacle());
                                }
                            }
                            StartCoroutine(WaitForNextTask(Random.Range(5, 15)));

                        }
                    }
                }
            }
            if (!waiting)
            {
                if(waitingFor.Count == 0 && !agent.enabled && !unableToReachTarget)
                {
                    StartCoroutine(DeactivateObstacle());
                }
                for (int i = waitingFor.Count - 1; i >= 0; i--)
                {
                    GameObject player = waitingFor[i];
                    if (player == null)
                    {
                        waitingFor.Remove(player);
                        continue;
                    }
                    PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
                    if (!playerMovement.participating) continue;
                    if (playerMovement.waiting || playerMovement.unableToReachTarget)
                    {
                        waitingFor.Remove(player);
                        playerMovement.waitingFor.Add(gameObject);
                    }
                }
            }
        }

        private bool CanReach(Vector3 destination)
        {
            NavMeshPath path = new NavMeshPath();
            return agent.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!participating) return;
            if (other.transform.parent.name != "Players") return;
            if (other.GetComponent<NavMeshObstacle>().enabled) return;
            if (transform.GetSiblingIndex() < other.transform.GetSiblingIndex() || !other.transform.GetComponent<PlayerMovement>().participating)
            {
                if (waitingFor.Contains(other.gameObject)) return;
                waitingFor.Add(other.gameObject);
                StartCoroutine(ActivateObstacle());
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!participating) return;
            if (other.transform.parent.name != "Players") return;
            waitingFor.Remove(other.gameObject);
        }

        IEnumerator WaitForNextTask(int s)
        {
            waiting = true;
            if (!participating)
            {
                agent.isStopped = true;
            }
            if (participating)
            {
                StartCoroutine(ActivateObstacle());
            }
            yield return new WaitForSeconds(s);
            waiting = false;
            if (!participating)
            {
                agent.isStopped = false;
            }
            if(status == "LEAVING")
            {
                transform.parent.GetComponent<RegisterManager>().OnLeaveRegister(register);
            }
            if (!participating)
            {
                agent.SetDestination(target);
            }
        }
        IEnumerator ActivateObstacle()
        {
            agent.enabled = false;
            yield return new WaitForEndOfFrame();
            while (agent.enabled)
            {
                agent.enabled = false;
                yield return null;
            }
            obstacle.enabled = true;
            yield return null;
        }

        IEnumerator DeactivateObstacle()
        {
            obstacle.enabled = false;
            yield return null;
            if(!obstacle.enabled) agent.enabled = true;
            yield return new WaitForEndOfFrame();
            if (agent.enabled) agent.SetDestination(target);
        }

        IEnumerator CheckForTarget()
        {
            StartCoroutine(DeactivateObstacle());
            yield return new WaitForChangedResult();
            if (!agent.enabled) yield break;
            if (taskManager.GetTask(out target))
            {
                unableToReachTarget = false;
                agent.SetDestination(target);
            }
            else if (participating)
            {
                StartCoroutine(ActivateObstacle());
            }
        }

        public void Infect()
        {
           // Debug.Log(name + " is infected");
            infected = true;
            gameObject.GetComponent<MeshRenderer>().material = infectedMaterial;
        }
        public void Asymptomatic()
        {
            asymptomatic = true;
            infected = true;
            gameObject.GetComponent<MeshRenderer>().material = asymptomaticMaterial;
        }
    }
}

