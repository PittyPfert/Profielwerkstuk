using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace Profielwerkstuk
{
    class Utility {
        public static float NextGaussian()
        {
            float v1, v2, s;
            do
            {
                v1 = 2.0f * Random.Range(0f, 1f) - 1.0f;
                v2 = 2.0f * Random.Range(0f, 1f) - 1.0f;
                s = v1 * v1 + v2 * v2;
            } while (s >= 1.0f || s == 0f);

            s = Mathf.Sqrt((-2.0f * Mathf.Log(s)) / s);

            return v1 * s;
        }

        public static float NextGaussian(float mean, float standard_deviation)
        {
            return mean + NextGaussian() * standard_deviation;
        }

        public static float NextGaussian(float mean, float standard_deviation, float min, float max)
        {
            float x;
            do
            {
                x = NextGaussian(mean, standard_deviation);
            } while (x < min || x > max);
            return x;
        }

        public static float Round(float n, int d)
        {
            float powered = Mathf.Pow(10, d);

            return Mathf.Round(n * powered) / powered;
        }

        public static Vector3 Round(Vector3 v, int d)
        {
            return new Vector3(Round(v.x, d), Round(v.y, d), Round(v.z, d));
        }

        public static Vector3 Rotate(Vector3 v)
        {
            return new Vector3(-v.z, v.y, v.x);
        }

        public static float Abs(float f)
        {
            if (f >= 0) return f;
            return -f;
        }

        public static Vector3 Abs(Vector3 v)
        {
            return new Vector3(Abs(v.x), Abs(v.y), Abs(v.z));
        }

        public static void PrintVector(Vector3 v)
        {
            Debug.Log("x: " + v.x + ", y: " + v.y + ", z: " + v.z);
        }

    }

    public class FlowManager : MonoBehaviour
    {
        private int hours = 0;
        private int minutes = 0;
        private float seconds = 0.0f;
        private float timeInHours;

        private int index = 0;

        /* When spawning a new player, the following variables need to be assigned:
         * Position
         * Ground
         * Number of tasks
         * Parent of coughclouds
        */

        public Transform spawningGround;
        public Transform taskGround;
        public Transform registerGround;
        public Transform leavingGround;
    
        public Transform ground;
        public Transform coughClouds;

        public GameObject playerPrefab;

        public DataHoarder dataHoarder;
        public Camera cam;
        private int playersPerDay;
        private List<float> spawningTimes;
        public List<Vector3> taskPositions;
        // Start is called before the first frame update
        void Start()
        {
            Time.timeScale = Config.speed;
            playersPerDay = (int)Config.playersPerDay;
            spawningTimes = new List<float>();
            for(int i = 0; i < playersPerDay; i++)
            {
                spawningTimes.Add(Utility.NextGaussian(Config.playerDistributionMean, 
                                                              Config.playerDistributionStandardDeviation, 
                                                              Config.openingTime, 
                                                              Config.closingTime));
            }
            spawningTimes.Sort();
            /*foreach(float time in spawningTimes)
            {
                print(time);
            }*/
            //print(spawningTimes[0]);    
            hours = (int)Config.openingTime;
            minutes = (int)((Config.openingTime % 1) * 60);

        }

        IEnumerator spawnPlayer(string name)
        {
            //print("spawning...");
            var minX = spawningGround.position.x - spawningGround.localScale.x / 2;
            var maxX = spawningGround.position.x + spawningGround.localScale.x / 2;
            var minZ = spawningGround.position.z - spawningGround.localScale.z / 2;
            var maxZ = spawningGround.position.z + spawningGround.localScale.z / 2;
            var y = spawningGround.position.y + spawningGround.localScale.y / 2;

            // Generates starting position
            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);
            var spawnPosition = new Vector3(x, y, z);
            //print("generated position");
                
            // Spawns player    
            GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity, transform);
            yield return new WaitForEndOfFrame();
            player.name = name;
            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            playerMovement.coughCloudParent = coughClouds;
            playerMovement.dataHoarder = dataHoarder;
            playerMovement.id = name;
            //print("spawned player");
            // Infects player   
            if (Random.Range(0.0f, 1.0f) >= Config.chanceInfected)
            {
                playerMovement.infected = true;
                player.GetComponent<MeshRenderer>().material = playerMovement.infectedMaterial;
            }

            dataHoarder.onSpawn(player.name, playerMovement.infected);

            // Assigns Tasks
            //print("assigning tasks");

            TaskManager taskManager = playerMovement.taskManager;
            NavMeshAgent agent = playerMovement.agent;
            int numTasks = Random.Range(15, 25);
            List<int> tasks = new List<int>();
            for (int t = 0; t < numTasks; t++)
            {
                int randomIndex;
                do
                {
                    randomIndex = Random.Range(0, taskPositions.Count);
                } while (tasks.Contains(randomIndex));
                tasks.Add(randomIndex);
            }

            foreach(int index in tasks)
            {
                taskManager.addTask(taskPositions[index]);
            }

            // taskManager.addPos("register", registerGround);
            taskManager.addPos("leaving", leavingGround);

            taskManager.waitingForRegisterPos = new Vector3(11, 1, -23);

            playerMovement.target = taskManager.getTask();
            agent.SetDestination(playerMovement.target);

            playerMovement.status = "ACTIVE";
            //print("done spawning");
        }

        /*IEnumerator spawnPlayers(int numPlayers)
        {
            yield return null;


            for (int i = 0; i < numPlayers; i++)
            {
                spawnPlayer(""+(i+1));
                yield return new WaitForSeconds(35);

            }
        }*/

        void Update()
        {

            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                Physics.Raycast(ray, out hit);
                Utility.PrintVector(hit.point); 
            }


            seconds += Time.deltaTime;
            if (seconds >= 60)
            {
                minutes++;
                // print(timeInHours);
                // print(hours + ":" + minutes + ":" + (int)seconds);
                // print(spawningTimes[index]);
            }
            if (minutes >= 60) hours++;
            seconds %= 60;
            minutes %= 60;

            timeInHours = hours + (minutes + seconds / 60) / 60;
            if(index < spawningTimes.Count)
            {
                if(timeInHours > spawningTimes[index])
                {
                    // print("Spawning player");
                    index++;
                    StartCoroutine(spawnPlayer("" + index));
                    // print("Spawned player");
                }
            }
        }
    }
}